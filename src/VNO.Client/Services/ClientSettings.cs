namespace VNO.Client.Services;

/// <summary>
/// Settings that control how the client connects
/// </summary>
/// <remarks>
/// Bound from the Client section of appsettings.json. These replace the hard
/// coded addresses found in the legacy Form15
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
    /// TCP port of the game server
    /// </summary>
    public int GameServerPort { get; set; } = 16789;

    /// <summary>
    /// Host name or address of the auth server
    /// </summary>
    public string AuthServerHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// TCP port of the auth server
    /// </summary>
    public int AuthServerPort { get; set; } = 6543;

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
