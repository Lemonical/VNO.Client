using VNO.Core.Networking;

namespace VNO.Client.Services;

/// <summary>
/// Settings that control how the client connects
/// </summary>
/// <remarks>
/// Loaded from the legacy data folder ini files by <see cref="ClientSettingsLoader"/>,
/// data\settings.ini for the saved identity and data\AS.ini for the auth server.
/// These replace the hard coded addresses found in the legacy Form15
/// </remarks>
public sealed class ClientSettings
{
    /// <summary>
    /// Name the player shows to others by default
    /// </summary>
    public string DisplayName { get; set; } = "Player";

    /// <summary>
    /// Host name or address of the game server
    /// </summary>
    public string GameServerHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// TCP port of the game server, also the HTTP/WebSocket port
    /// </summary>
    public int GameServerPort { get; set; } = 16789;

    /// <summary>
    /// Which transport the client reaches a game server over
    /// </summary>
    public Transport GameServerTransport { get; set; } = Transport.Tcp;

    /// <summary>
    /// Dial game servers over TLS, wss instead of ws
    /// </summary>
    public bool GameServerUseTls { get; set; }

    /// <summary>
    /// Host name or address of the auth server
    /// </summary>
    public string AuthServerHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// TCP port of the auth server
    /// </summary>
    public int AuthServerPort { get; set; } = 6543;

    /// <summary>
    /// Which transport the client reaches the auth server over
    /// </summary>
    /// <remarks>
    /// The auth server list entry may be a bare address (legacy TCP) or a ws/wss URL. A wss
    /// URL selects WebSocket with TLS, which is how the App Platform hosted AS is reached
    /// </remarks>
    public Transport AuthTransport { get; set; } = Transport.Tcp;

    /// <summary>
    /// Dial the auth server over TLS, wss instead of ws
    /// </summary>
    public bool AuthUseTls { get; set; }

    /// <summary>
    /// How often to send a heartbeat to the game server, in seconds
    /// </summary>
    public int HeartbeatSeconds { get; set; } = 10;

    /// <summary>
    /// Folder holding the legacy player data layout, settings.ini and UI themes.
    /// Relative paths resolve against the executable directory like the original
    /// </summary>
    public string DataDirectory { get; set; } = "data";
}
