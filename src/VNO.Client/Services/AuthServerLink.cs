using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VNO.Core.Models;
using VNO.Core.Networking;
using VNO.Core.Protocol;

namespace VNO.Client.Services;

/// <summary>
/// Default auth server link for the client, built on the core message client
/// </summary>
/// <remarks>
/// Ports the clientsocket_master traffic from Form15. After the TCP connect the
/// link answers the master's handshake with a version check, receives the news
/// text and the public server list, and performs account login and creation on
/// behalf of the login screen. It owns its own socket so it never competes with
/// the game server link for the shared <see cref="IMessageClient"/>
/// </remarks>
public sealed class AuthServerLink : IAuthServerLink, IAsyncDisposable
{
    private static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(10);

    private readonly IMessageClient _client;
    private readonly ClientSettings _settings;
    private readonly IClientSession _session;
    private readonly ILogger<AuthServerLink> _logger;
    private readonly SemaphoreSlim _maintenanceGate = new(1, 1);

    private Timer? _heartbeatTimer;
    private TaskCompletionSource<MasterLoginResult>? _pendingLogin;
    private TaskCompletionSource<AccountCreateResult>? _pendingCreate;
    private TaskCompletionSource<string?>? _pendingGameToken;
    private TaskCompletionSource<bool>? _pendingVersion;
    private string? _accountName;
    private string? _accountPassword;

    /// <summary>
    /// Creates the link with its dependencies
    /// </summary>
    /// <remarks>
    /// Builds a dedicated message client so the AS socket is independent of the game
    /// server socket that <see cref="ServerConnection"/> drives
    /// </remarks>
    public AuthServerLink(
        ILoggerFactory loggerFactory,
        IOptions<ClientSettings> settings,
        IClientSession session,
        ILogger<AuthServerLink> logger)
    {
        _settings = settings.Value;

        // the AS link picks its transport from settings, wss to the App Platform hosted AS or
        // TCP to a legacy one. Small frames, so the tight auth inbound cap applies
        var transportOptions = new WebSocketTransportOptions
        {
            UseTls = _settings.AuthUseTls,
            MaxInboundBytes = ProtocolConstants.MaxAuthMessageBytes,
        };
        _client = MessageTransportFactory.CreateClient(_settings.AuthTransport, loggerFactory, transportOptions);
        _session = session;
        _logger = logger;
        _client.StateChanged += (_, e) =>
        {
            if (e.State == ConnectionState.Disconnected)
            {
                _pendingVersion?.TrySetResult(false);
                _pendingLogin?.TrySetResult(MasterLoginResult.NotConnected);
                _pendingGameToken?.TrySetResult(null);
            }
            StateChanged?.Invoke(this, e.State);
        };
        _client.MessageReceived += (_, e) => HandleMessage(e.Message);
    }

    /// <inheritdoc />
    public ConnectionState State => _client.State;

    /// <inheritdoc />
    public event EventHandler<ConnectionState>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<string>? NewsReceived;

    /// <inheritdoc />
    public event EventHandler? VersionRejected;

    /// <inheritdoc />
    public event EventHandler<ServerListing>? ServerDiscovered;

    /// <inheritdoc />
    public event EventHandler? ConnectFailed;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ConnectAndVerifyAsync(cancellationToken).ConfigureAwait(false))
            {
                StartMaintenance();
            }
        }
        catch (Exception ex)
        {
            // Master is authoritative; surface the failure and remain on the login screen.
            _logger.LogWarning(ex, "Could not reach the auth server");
            ConnectFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public async Task<MasterLoginResult> LoginAsync(
        string username, string password, CancellationToken cancellationToken = default)
    {
        if (_client.State != ConnectionState.Connected)
        {
            return MasterLoginResult.NotConnected;
        }

        var pending = new TaskCompletionSource<MasterLoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingLogin = pending;
        var credential = LegacyHash.ToWireCredential(password);
        await _client.SendAsync(new NetworkMessage(MessageType.MasterLogin, username, credential), cancellationToken)
            .ConfigureAwait(false);
        var result = await AwaitReplyAsync(pending, MasterLoginResult.TimedOut, cancellationToken)
            .ConfigureAwait(false);
        if (result == MasterLoginResult.Granted)
        {
            _accountName = username;
            _accountPassword = credential;
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<AccountCreateResult> CreateAccountAsync(
        string username, string password, CancellationToken cancellationToken = default)
    {
        if (_client.State != ConnectionState.Connected)
        {
            return AccountCreateResult.NotConnected;
        }

        var pending = new TaskCompletionSource<AccountCreateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCreate = pending;
        await _client.SendAsync(
            new NetworkMessage(MessageType.CreateAccount, username, LegacyHash.ToWireCredential(password)),
            cancellationToken)
            .ConfigureAwait(false);
        return await AwaitReplyAsync(pending, AccountCreateResult.TimedOut, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RequestServersAsync(CancellationToken cancellationToken = default) =>
        _client.State == ConnectionState.Connected
            ? _client.SendAsync(NetworkMessage.Create(MessageType.RequestServers), cancellationToken)
            : Task.CompletedTask;

    /// <inheritdoc />
    public async Task<string?> RequestGameTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_client.State != ConnectionState.Connected)
        {
            return null;
        }

        var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _pendingGameToken, pending, null) is not null)
        {
            return null;
        }

        try
        {
            await _client.SendAsync(NetworkMessage.Create(MessageType.GameTokenRequest), cancellationToken)
                .ConfigureAwait(false);
            return await AwaitReplyAsync(pending, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.CompareExchange(ref _pendingGameToken, null, pending);
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        if (_heartbeatTimer is not null)
        {
            await _heartbeatTimer.DisposeAsync().ConfigureAwait(false);
            _heartbeatTimer = null;
        }

        await _client.DisconnectAsync().ConfigureAwait(false);
        _accountName = null;
        _accountPassword = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        await _client.DisposeAsync().ConfigureAwait(false);
    }

    private void HandleMessage(NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.VersionAccepted:
                _pendingVersion?.TrySetResult(true);
                break;

            case MessageType.VersionRejected:
            case MessageType.AddressBanned:
                _pendingVersion?.TrySetResult(false);
                VersionRejected?.Invoke(this, EventArgs.Empty);
                break;

            case MessageType.ServerEntry:
                if (TryParseListing(message, out var listing))
                {
                    ServerDiscovered?.Invoke(this, listing);
                }
                break;

            case MessageType.BadgeGrant:
                // the master pushes one of these per badge holder right after login, the
                // stage later draws the badge next to anyone speaking under the name
                _session.SetBadge(message.GetArgument(0), message.GetArgument(1));
                break;

            case MessageType.LoginGranted:
                NewsReceived?.Invoke(this, message.GetArgument(1).Replace('|', '\n'));
                ExpandLoginSnapshot(message);
                _pendingLogin?.TrySetResult(MasterLoginResult.Granted);
                break;

            case MessageType.LoginDenied:
                _pendingLogin?.TrySetResult(MasterLoginResult.Denied);
                break;

            case MessageType.AccountBanned:
                _pendingLogin?.TrySetResult(MasterLoginResult.Banned);
                break;

            case MessageType.AccountCreated:
                _pendingCreate?.TrySetResult(AccountCreateResult.Created);
                break;

            case MessageType.AccountTaken:
                _pendingCreate?.TrySetResult(AccountCreateResult.Taken);
                break;

            case MessageType.AccountInvalid:
                _pendingCreate?.TrySetResult(AccountCreateResult.Invalid);
                break;

            case MessageType.GameTokenIssued:
                var token = message.GetArgument(0);
                _pendingGameToken?.TrySetResult(string.IsNullOrWhiteSpace(token) ? null : token);
                break;
        }
    }

    private static bool TryParseListing(NetworkMessage message, out ServerListing listing)
    {
        listing = new ServerListing
        {
            Index = int.TryParse(message.GetArgument(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                ? index
                : 0,
            Name = message.GetArgument(1),
            Host = message.GetArgument(2),
            Port = int.TryParse(message.GetArgument(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
                ? port
                : ProtocolConstants.DefaultGameServerPort,
            Description = message.GetArgument(4),
            ContentUrl = message.GetArgument(5),
        };
        return listing.Name.Length > 0 && listing.Host.Length > 0;
    }

    private void ExpandLoginSnapshot(NetworkMessage message)
    {
        if (message.Arguments.Count <= 2)
        {
            return;
        }

        var offset = 2;
        if (!TryReadCount(message.Arguments, ref offset, 7, out var serverCount))
        {
            return;
        }

        var servers = new List<ServerListing>(serverCount);
        for (var index = 0; index < serverCount; index++)
        {
            var entry = new NetworkMessage(
                MessageType.ServerEntry,
                message.Arguments[offset],
                message.Arguments[offset + 1],
                message.Arguments[offset + 2],
                message.Arguments[offset + 3],
                message.Arguments[offset + 4],
                message.Arguments[offset + 5],
                message.Arguments[offset + 6]);
            if (!TryParseListing(entry, out var listing))
            {
                return;
            }
            servers.Add(listing);
            offset += 7;
        }

        if (!TryReadCount(message.Arguments, ref offset, 2, out var badgeCount) ||
            offset + (badgeCount * 2) != message.Arguments.Count)
        {
            return;
        }

        var badges = new List<(string Name, string Badge)>(badgeCount);
        for (var index = 0; index < badgeCount; index++)
        {
            badges.Add((message.Arguments[offset++], message.Arguments[offset++]));
        }

        foreach (var server in servers)
        {
            ServerDiscovered?.Invoke(this, server);
        }
        foreach (var badge in badges)
        {
            _session.SetBadge(badge.Name, badge.Badge);
        }
    }

    private static bool TryReadCount(
        IReadOnlyList<string> fields,
        ref int offset,
        int fieldsPerItem,
        out int count)
    {
        count = 0;
        return offset < fields.Count &&
            int.TryParse(fields[offset++], NumberStyles.None, CultureInfo.InvariantCulture, out count) &&
            count >= 0 &&
            count <= (fields.Count - offset) / fieldsPerItem;
    }

    private static async Task<T> AwaitReplyAsync<T>(
        TaskCompletionSource<T> pending, T timeoutResult, CancellationToken cancellationToken)
    {
        var completed = await Task.WhenAny(pending.Task, Task.Delay(ReplyTimeout, cancellationToken))
            .ConfigureAwait(false);
        return completed == pending.Task ? await pending.Task.ConfigureAwait(false) : timeoutResult;
    }

    private void StartMaintenance()
    {
        var period = TimeSpan.FromSeconds(Math.Max(1, _settings.HeartbeatSeconds));
        _heartbeatTimer = new Timer(_ => _ = MaintainAsync(), null, period, period);
    }

    private async Task MaintainAsync()
    {
        if (!await _maintenanceGate.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            if (_client.State == ConnectionState.Connected)
            {
                await _client.SendAsync(NetworkMessage.Create(MessageType.MasterHeartbeat)).ConfigureAwait(false);
                return;
            }

            if (_accountName is null || _accountPassword is null ||
                !await ConnectAndVerifyAsync(CancellationToken.None).ConfigureAwait(false))
            {
                return;
            }

            var pending = new TaskCompletionSource<MasterLoginResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingLogin = pending;
            await _client.SendAsync(
                new NetworkMessage(MessageType.MasterLogin, _accountName, _accountPassword))
                .ConfigureAwait(false);
            var result = await AwaitReplyAsync(pending, MasterLoginResult.TimedOut, CancellationToken.None)
                .ConfigureAwait(false);
            if (result != MasterLoginResult.Granted)
            {
                _accountName = null;
                _accountPassword = null;
                await _client.DisconnectAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reconnecting to the auth server failed");
        }
        finally
        {
            _maintenanceGate.Release();
        }
    }

    private async Task<bool> ConnectAndVerifyAsync(CancellationToken cancellationToken)
    {
        await _client.ConnectAsync(_settings.AuthServerHost, _settings.AuthServerPort, cancellationToken)
            .ConfigureAwait(false);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingVersion = pending;
        try
        {
            await _client.SendAsync(
                new NetworkMessage(MessageType.VersionCheck, "client", ProtocolConstants.ClientVersion),
                cancellationToken).ConfigureAwait(false);
            var accepted = await AwaitReplyAsync(pending, false, cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                await _client.DisconnectAsync().ConfigureAwait(false);
            }
            return accepted;
        }
        finally
        {
            _pendingVersion = null;
        }
    }
}
