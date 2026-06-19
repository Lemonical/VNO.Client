using System;
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
    private readonly IMessageClient _client;
    private readonly ClientSettings _settings;

    private Timer? _heartbeatTimer;

    /// <summary>
    /// Creates the connection with its dependencies
    /// </summary>
    public ServerConnection(IMessageClient client, IOptions<ClientSettings> settings)
    {
        _client = client;
        _settings = settings.Value;
        _client.MessageReceived += (_, e) => MessageReceived?.Invoke(this, e.Message);
        _client.StateChanged += (_, e) => StateChanged?.Invoke(this, e.State);
    }

    /// <inheritdoc />
    public ConnectionState State => _client.State;

    /// <inheritdoc />
    public event EventHandler<NetworkMessage>? MessageReceived;

    /// <inheritdoc />
    public event EventHandler<ConnectionState>? StateChanged;

    /// <inheritdoc />
    public async Task ConnectAsync(
        string displayName,
        string? host = null,
        int? port = null,
        CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(
            host ?? _settings.GameServerHost, port ?? _settings.GameServerPort, cancellationToken)
            .ConfigureAwait(false);

        await _client.SendAsync(new NetworkMessage(MessageType.Hello, displayName), cancellationToken)
            .ConfigureAwait(false);

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

    private void SendHeartbeat()
    {
        if (_client.State == ConnectionState.Connected)
        {
            _ = _client.SendAsync(NetworkMessage.Create(MessageType.Heartbeat));
        }
    }
}
