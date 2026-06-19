using System;
using System.Globalization;
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
    private readonly ILogger<AuthServerLink> _logger;

    private Timer? _heartbeatTimer;
    private TaskCompletionSource<MasterLoginResult>? _pendingLogin;
    private TaskCompletionSource<AccountCreateResult>? _pendingCreate;

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
        ILogger<AuthServerLink> logger)
    {
        _client = new TcpMessageClient(loggerFactory.CreateLogger<TcpMessageClient>());
        _settings = settings.Value;
        _logger = logger;
        _client.StateChanged += (_, e) => StateChanged?.Invoke(this, e.State);
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
            await _client.ConnectAsync(_settings.AuthServerHost, _settings.AuthServerPort, cancellationToken)
                .ConfigureAwait(false);

            // identify as a player client and state our version, the legacy VER#C#
            // reply to the master's CV prompt so the AS can gate outdated clients
            await _client.SendAsync(
                new NetworkMessage(MessageType.VersionCheck, "client", ProtocolConstants.ClientVersion),
                cancellationToken).ConfigureAwait(false);

            StartHeartbeat();
        }
        catch (Exception ex)
        {
            // a missing AS is not fatal, the legacy client dropped to guest mode
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
        await _client.SendAsync(new NetworkMessage(MessageType.MasterLogin, username, password), cancellationToken)
            .ConfigureAwait(false);
        return await AwaitReplyAsync(pending, MasterLoginResult.TimedOut, cancellationToken).ConfigureAwait(false);
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
        await _client.SendAsync(new NetworkMessage(MessageType.CreateAccount, username, password), cancellationToken)
            .ConfigureAwait(false);
        return await AwaitReplyAsync(pending, AccountCreateResult.TimedOut, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RequestServersAsync(CancellationToken cancellationToken = default) =>
        _client.State == ConnectionState.Connected
            ? _client.SendAsync(NetworkMessage.Create(MessageType.RequestServers), cancellationToken)
            : Task.CompletedTask;

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        if (_heartbeatTimer is not null)
        {
            await _heartbeatTimer.DisposeAsync().ConfigureAwait(false);
            _heartbeatTimer = null;
        }

        await _client.DisconnectAsync().ConfigureAwait(false);
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
                // the legacy VEROK payload is the news text with | as line breaks,
                // and the client immediately asked for the server list
                NewsReceived?.Invoke(this, message.GetArgument(0).Replace('|', '\n'));
                _ = RequestServersAsync();
                break;

            case MessageType.VersionRejected:
            case MessageType.AddressBanned:
                VersionRejected?.Invoke(this, EventArgs.Empty);
                break;

            case MessageType.ServerEntry:
                if (TryParseListing(message, out var listing))
                {
                    ServerDiscovered?.Invoke(this, listing);
                }
                break;

            case MessageType.LoginGranted:
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

    private static async Task<T> AwaitReplyAsync<T>(
        TaskCompletionSource<T> pending, T timeoutResult, CancellationToken cancellationToken)
    {
        var completed = await Task.WhenAny(pending.Task, Task.Delay(ReplyTimeout, cancellationToken))
            .ConfigureAwait(false);
        return completed == pending.Task ? await pending.Task.ConfigureAwait(false) : timeoutResult;
    }

    private void StartHeartbeat()
    {
        var period = TimeSpan.FromSeconds(Math.Max(1, _settings.HeartbeatSeconds));
        _heartbeatTimer = new Timer(_ => SendHeartbeat(), null, period, period);
    }

    private void SendHeartbeat()
    {
        if (_client.State == ConnectionState.Connected)
        {
            // the master's dispatcher recognizes MasterHeartbeat as keepalive traffic
            _ = _client.SendAsync(NetworkMessage.Create(MessageType.MasterHeartbeat));
        }
    }
}
