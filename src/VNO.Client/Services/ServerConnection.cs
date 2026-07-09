using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VNO.Core.Models;
using VNO.Core.Networking;
using VNO.Core.Protocol;

namespace VNO.Client.Services;

/// <summary>
/// Default game server link built on the core message client
/// </summary>
public sealed class ServerConnection : IServerConnection, IAsyncDisposable
{
    private static readonly TimeSpan AuthenticationTimeout = TimeSpan.FromSeconds(10);

    private readonly IMessageClient _client;
    private readonly ClientSettings _settings;

    private Timer? _heartbeatTimer;
    private TaskCompletionSource<bool>? _pendingVersion;
    private TaskCompletionSource<bool>? _pendingAuthentication;

    /// <summary>
    /// Creates the connection with its dependencies
    /// </summary>
    public ServerConnection(IMessageClient client, IOptions<ClientSettings> settings)
    {
        _client = client;
        _settings = settings.Value;
        _client.MessageReceived += (_, e) => ForwardMessage(e.Message);
        _client.StateChanged += (_, e) =>
        {
            if (e.State == ConnectionState.Disconnected)
            {
                _pendingVersion?.TrySetResult(false);
                _pendingAuthentication?.TrySetResult(false);
            }
            StateChanged?.Invoke(this, e.State);
        };
    }

    /// <inheritdoc />
    public ConnectionState State => _client.State;

    /// <inheritdoc />
    public event EventHandler<NetworkMessage>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<ConnectionState>? StateChanged;

    /// <inheritdoc />
    public async Task ConnectAsync(
        string handoffToken,
        string? host = null,
        int? port = null,
        CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(
            host ?? _settings.GameServerHost, port ?? _settings.GameServerPort, cancellationToken)
            .ConfigureAwait(false);

        var version = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingVersion = version;
        await _client.SendAsync(
            new NetworkMessage(MessageType.VersionCheck, "client", ProtocolConstants.ClientVersion),
            cancellationToken).ConfigureAwait(false);
        if (!await AwaitGateAsync(version, cancellationToken).ConfigureAwait(false))
        {
            _pendingVersion = null;
            await _client.DisconnectAsync().ConfigureAwait(false);
            throw new UnauthorizedAccessException("The game server rejected this client version");
        }
        _pendingVersion = null;

        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingAuthentication = pending;
        await _client.SendAsync(new NetworkMessage(MessageType.Login, handoffToken), cancellationToken)
            .ConfigureAwait(false);

        var authenticated = await AwaitGateAsync(pending, cancellationToken).ConfigureAwait(false);
        _pendingAuthentication = null;
        if (!authenticated)
        {
            await _client.DisconnectAsync().ConfigureAwait(false);
            throw new UnauthorizedAccessException("The game server rejected the Master session");
        }

        StartHeartbeat();
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
    }

    /// <inheritdoc />
    public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default) =>
        _client.SendAsync(message, cancellationToken);

    /// <inheritdoc />
    public Task SendInCharacterAsync(string characterName, string text, CancellationToken cancellationToken = default) =>
        // the legacy IC message carried the character name and the spoken line
        _client.SendAsync(new NetworkMessage(MessageType.InCharacter, characterName, text), cancellationToken);

    /// <inheritdoc />
    public Task SendChatAsync(string text, CancellationToken cancellationToken = default) =>
        _client.SendAsync(new NetworkMessage(MessageType.OutOfCharacter, text), cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
        await _client.DisposeAsync().ConfigureAwait(false);
    }

    private void StartHeartbeat()
    {
        var period = TimeSpan.FromSeconds(Math.Max(1, _settings.HeartbeatSeconds));
        _heartbeatTimer = new Timer(_ => SendHeartbeat(), null, period, period);
    }

    private void ForwardMessage(NetworkMessage message)
    {
        if (message.Type == MessageType.VersionAccepted)
        {
            _pendingVersion?.TrySetResult(true);
            return;
        }
        if (message.Type is MessageType.VersionRejected or MessageType.AddressBanned)
        {
            _pendingVersion?.TrySetResult(false);
            return;
        }

        if (message.Type == MessageType.LoginRejected)
        {
            _pendingAuthentication?.TrySetResult(false);
            return;
        }

        if (message.Type != MessageType.JoinSnapshot)
        {
            MessageReceived?.Invoke(this, message);
            return;
        }

        var offset = 0;
        if (!TryReadList(message.Arguments, ref offset, out var areas) ||
            !TryReadList(message.Arguments, ref offset, out var music) ||
            !TryReadList(message.Arguments, ref offset, out var characters))
        {
            _pendingAuthentication?.TrySetResult(false);
            return;
        }

        List<string> items = [];
        if (offset < message.Arguments.Count &&
            !TryReadList(message.Arguments, ref offset, out items) ||
            offset != message.Arguments.Count)
        {
            _pendingAuthentication?.TrySetResult(false);
            return;
        }

        _pendingAuthentication?.TrySetResult(true);
        MessageReceived?.Invoke(this, new NetworkMessage(MessageType.AreaList, areas.ToArray()));
        MessageReceived?.Invoke(this, new NetworkMessage(MessageType.MusicList, music.ToArray()));
        MessageReceived?.Invoke(this, new NetworkMessage(MessageType.CharacterList, characters.ToArray()));
        MessageReceived?.Invoke(this, new NetworkMessage(MessageType.ItemList, items.ToArray()));
    }

    private static bool TryReadList(
        IReadOnlyList<string> fields,
        ref int offset,
        out List<string> values)
    {
        values = [];
        if (offset >= fields.Count ||
            !int.TryParse(fields[offset++], NumberStyles.None, CultureInfo.InvariantCulture, out var count) ||
            count < 0 ||
            count > fields.Count - offset)
        {
            return false;
        }

        values = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            values.Add(fields[offset++]);
        }

        return true;
    }

    private static async Task<bool> AwaitGateAsync(
        TaskCompletionSource<bool> pending,
        CancellationToken cancellationToken)
    {
        var completed = await Task.WhenAny(
            pending.Task,
            Task.Delay(AuthenticationTimeout, cancellationToken)).ConfigureAwait(false);
        return completed == pending.Task && await pending.Task.ConfigureAwait(false);
    }

    private void SendHeartbeat()
    {
        if (_client.State == ConnectionState.Connected)
        {
            _ = _client.SendAsync(NetworkMessage.Create(MessageType.Heartbeat));
        }
    }
}
