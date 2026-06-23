using System;
using System.IO;

namespace VNO.Client.Services;

/// <summary>
/// Builds <see cref="ClientSettings"/> from the legacy data folder ini files
/// </summary>
/// <remarks>
/// The original client never had a json config, it read its identity from
/// data\settings.ini and its auth server list from data\AS.ini next to the
/// executable. This loader reproduces that so the port is configured by the same
/// external files a player already edits, not an appsettings.json. Every key is
/// read with a default so a missing or partial file degrades to the built in
/// values instead of failing, matching the legacy tolerance
/// </remarks>
public static class ClientSettingsLoader
{
    // the legacy data folder sits next to the executable and anchors every read
    private const string DataDirectoryName = "data";

    /// <summary>
    /// Reads settings.ini and AS.ini from the data folder and returns the settings
    /// </summary>
    public static ClientSettings Load() => Load(AppContext.BaseDirectory);

    /// <summary>
    /// Reads the settings from the data folder under the given base directory
    /// </summary>
    public static ClientSettings Load(string baseDirectory)
    {
        var settings = new ClientSettings { DataDirectory = DataDirectoryName };
        var dataDirectory = Path.Combine(baseDirectory, DataDirectoryName);

        // settings.ini carries the saved player identity
        var settingsIni = DelphiIniFile.Load(Path.Combine(dataDirectory, "settings.ini"));
        settings.DisplayName = settingsIni.ReadString("User", "user", settings.DisplayName);

        // AS.ini is the auth server directory, the first numbered entry is primary
        var authIni = DelphiIniFile.Load(Path.Combine(dataDirectory, "AS.ini"));
        var primary = authIni.ReadString("AS", "1", string.Empty);
        if (primary.Length > 0)
        {
            var (host, port) = SplitHostPort(primary, settings.AuthServerPort);
            settings.AuthServerHost = host;
            settings.AuthServerPort = port;
        }

        return settings;
    }

    // legacy AS.ini stored bare addresses, allow an optional :port suffix so a
    // non default auth port can be set without a second key
    private static (string Host, int Port) SplitHostPort(string value, int defaultPort)
    {
        var colon = value.LastIndexOf(':');
        if (colon > 0 && int.TryParse(value[(colon + 1)..], out var port))
        {
            return (value[..colon].Trim(), port);
        }
        return (value.Trim(), defaultPort);
    }
}
