using VNO.Core.Networking;

namespace VNO.Client.Services;

/// <summary>
/// Settings that control how the client connects
/// </summary>
/// <remarks>
/// Loaded from the legacy data folder ini files by <see cref="ClientSettingsLoader"/>,
/// data\settings.ini for player-owned preferences. The public Master endpoint is
/// defined centrally by VNO.Core so it cannot drift between Client and Server.
/// </remarks>
public sealed class ClientSettings
{
    /// <summary>
    /// Privacy level for optional Discord Rich Presence. Defaults to off.
    /// </summary>
    public DiscordPresenceMode DiscordPresence { get; set; } = DiscordPresenceMode.Off;

    /// <summary>
    /// Public Discord application id used by local desktop IPC, never a client secret.
    /// </summary>
    public string DiscordApplicationId { get; set; } = string.Empty;

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
    /// How often to send a heartbeat to the game server, in seconds
    /// </summary>
    public int HeartbeatSeconds { get; set; } = 10;

    /// <summary>
    /// Folder holding the legacy player data layout, settings.ini and UI themes.
    /// Relative paths resolve against the executable directory like the original
    /// </summary>
    public string DataDirectory { get; set; } = "data";
}
