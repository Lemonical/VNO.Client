using System;
using System.Threading;
using System.Threading.Tasks;
using VNO.Core.Models;

namespace VNO.Client.Services;

/// <summary>
/// The client link to the central auth and listing service, the AS
/// </summary>
/// <remarks>
/// The legacy client owned clientsocket_master and used it to reach the AS for the
/// login screen status, the news text, the server list, and account operations.
/// Here that outbound link is a service so the login and server list screens can
/// share one connection and react to its traffic. This is separate from
/// <see cref="IServerConnection"/>, which links to a chosen game server
/// </remarks>
public interface IAuthServerLink
{
    /// <summary>
    /// Current state of the link to the auth server
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Raised when the link state changes
    /// </summary>
    event EventHandler<ConnectionState>? StateChanged;

    /// <summary>
    /// Raised when the AS accepts the version check and delivers the news text,
    /// the legacy VEROK reply
    /// </summary>
    event EventHandler<string>? NewsReceived;

    /// <summary>
    /// Raised when the AS rejects this client's version, the legacy VERPB reply
    /// </summary>
    event EventHandler? VersionRejected;

    /// <summary>
    /// Raised once per server list entry the AS sends, the legacy SDA packets
    /// </summary>
    event EventHandler<ServerListing>? ServerDiscovered;

    /// <summary>
    /// Raised when the connection attempt to the AS fails, the legacy path that
    /// dropped the player into the server list as a guest
    /// </summary>
    event EventHandler? ConnectFailed;

    /// <summary>
    /// Connects to the configured auth server, performs the version handshake,
    /// and begins the heartbeat
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts an account login, the legacy CO command
    /// </summary>
    Task<MasterLoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to create an account, the legacy CA command
    /// </summary>
    Task<AccountCreateResult> CreateAccountAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the AS to resend the public server list
    /// </summary>
    Task RequestServersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the link to the auth server
    /// </summary>
    Task DisconnectAsync();
}
