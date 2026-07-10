using System;
using System.IO;
using VNO.Core.Networking;

namespace VNO.Client.Services;

/// <summary>
/// Builds <see cref="ClientSettings"/> from the legacy data folder ini files
/// </summary>
/// <remarks>
/// The original client never had a json config, it read its identity from
/// data\settings.ini next to the executable. The Master endpoint is intentionally
/// owned by VNO.Core rather than player-editable data. Every key is
/// read with a default so a missing or partial file degrades to the built in
/// values instead of failing, matching the legacy tolerance
/// </remarks>
public static class ClientSettingsLoader
{
    // the legacy data folder sits next to the executable and anchors every read
    private const string DataDirectoryName = "data";

    /// <summary>
    /// Reads settings.ini from the data folder and returns the settings
    /// </summary>
    public static ClientSettings Load() => Load(AppContext.BaseDirectory);

    /// <summary>
    /// Reads the settings from the data folder under the given base directory
    /// </summary>
    public static ClientSettings Load(string baseDirectory)
    {
        var settings = new ClientSettings { DataDirectory = DataDirectoryName };
        var dataDirectory = Path.Combine(baseDirectory, DataDirectoryName);

        // settings.ini carries the saved player identity and the game server transport
        var settingsIni = DelphiIniFile.Load(Path.Combine(dataDirectory, "settings.ini"));
        settings.DisplayName = settingsIni.ReadString("User", "user", settings.DisplayName);
        settings.GameServerTransport = ReadTransport(
            settingsIni.ReadString("Network", "transport", string.Empty), settings.GameServerTransport);
        settings.GameServerUseTls = ReadBool(
            settingsIni.ReadString("Network", "tls", string.Empty), settings.GameServerUseTls);
        settings.DiscordPresence = ReadDiscordPresence(
            settingsIni.ReadString("Discord", "presence", string.Empty));
        settings.DiscordApplicationId = settingsIni.ReadString("Discord", "application_id", string.Empty).Trim();

        return settings;
    }

    private static Transport ReadTransport(string value, Transport fallback) =>
        value.Trim().ToLowerInvariant() switch
        {
            "websocket" or "ws" or "wss" => Transport.WebSocket,
            "tcp" => Transport.Tcp,
            _ => fallback,
        };

    // the legacy ini stored booleans as 1/0, true/false, or yes/no
    private static bool ReadBool(string value, bool fallback) =>
        value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => fallback,
        };

    private static DiscordPresenceMode ReadDiscordPresence(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "running" => DiscordPresenceMode.Running,
            "server" or "publicserver" => DiscordPresenceMode.PublicServer,
            "players" or "publicserverandplayercount" => DiscordPresenceMode.PublicServerAndPlayerCount,
            _ => DiscordPresenceMode.Off,
        };

}
