namespace VNO.Client.Services;

/// <summary>
/// Controls how much game activity the player publishes to Discord.
/// </summary>
public enum DiscordPresenceMode
{
    /// <summary>
    /// Do not publish Discord Rich Presence.
    /// </summary>
    Off,

    /// <summary>
    /// Show only that Visual Novel Online is running.
    /// </summary>
    Running,

    /// <summary>
    /// Also show the name of a server obtained from the public Master directory.
    /// </summary>
    PublicServer,

    /// <summary>
    /// Also show a validated player count when the public directory provides one.
    /// </summary>
    PublicServerAndPlayerCount,
}
