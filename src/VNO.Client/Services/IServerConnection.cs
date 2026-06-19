using System;
using System.Threading;
using System.Threading.Tasks;
using VNO.Core.Models;
using VNO.Core.Protocol;

namespace VNO.Client.Services;

/// <summary>
/// The client link to a game server
/// </summary>
/// <remarks>
/// Wraps the core message client and adds the few high level sends the client
/// needs. The legacy Form15 owned this socket directly, here it is a service so
/// the main, moderator, and animator views can share one link
/// </remarks>
public interface IServerConnection
{
    /// <summary>
    /// Current state of the link
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Raised when any message arrives from the server
    /// </summary>
    event EventHandler<NetworkMessage>? MessageReceived;

    /// <summary>
    /// Raised when the link state changes
    /// </summary>
    event EventHandler<ConnectionState>? StateChanged;

    /// <summary>
    /// Connects to a game server and sends the hello. Host and port default to
    /// the configured game server when not given, matching the legacy default
    /// </summary>
    Task ConnectAsync(
        string displayName,
        string? host = null,
        int? port = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the link
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a raw message, used by the moderator and animator views
    /// </summary>
    Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an in character chat line, the legacy IC message
    /// </summary>
    Task SendInCharacterAsync(string characterName, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an out of character chat line
    /// </summary>
    Task SendChatAsync(string text, CancellationToken cancellationToken = default);
}
