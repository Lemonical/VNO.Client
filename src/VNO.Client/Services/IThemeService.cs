using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace VNO.Client.Services;

/// <summary>
/// Access to the player supplied UI theme and the legacy data folder
/// </summary>
/// <remarks>
/// The legacy client drew its whole interface from a user provided theme folder,
/// data\UI\&lt;design&gt;\, chosen by the design key in data\settings.ini. The theme
/// folder holds the skin images (login.png, masterlist.png, ...) and a design.ini
/// with per control colors and fonts. Themes are player content, never packaged
/// with the client, so every read tolerates a missing file and falls back to the
/// legacy defaults
/// </remarks>
public interface IThemeService
{
    /// <summary>
    /// Name of the active theme folder from settings.ini
    /// </summary>
    string DesignName { get; }

    /// <summary>
    /// Loads a skin image from the active theme folder, null when the file is missing
    /// </summary>
    Bitmap? GetImage(string relativePath);

    /// <summary>
    /// Reads a design.ini ObjectColor entry, white when absent like the original
    /// </summary>
    Color GetColor(string key);

    /// <summary>
    /// Reads a design.ini ObjectColor entry as a solid brush
    /// </summary>
    IBrush GetBrush(string key);

    /// <summary>
    /// Reads a design.ini Font entry, Tahoma when absent like the original
    /// </summary>
    FontFamily GetFontFamily(string key);

    /// <summary>
    /// Reads a design.ini Font size entry in points and returns display pixels
    /// </summary>
    double GetFontSize(string key);

    /// <summary>
    /// Reads a plain integer entry from the given design.ini section, used for the
    /// Placement offsets like badge_leftdiv and badge_updiv, 0 when absent
    /// </summary>
    int GetDesignInteger(string section, string key, int fallback);

    /// <summary>
    /// Reads a value from data\settings.ini, the legacy user settings file
    /// </summary>
    string ReadSetting(string section, string key, string fallback);

    /// <summary>
    /// Reads an integer value from data\settings.ini
    /// </summary>
    int ReadSettingInteger(string section, string key, int fallback);

    /// <summary>
    /// Writes a value to data\settings.ini, preserving the file's comment lines
    /// </summary>
    void WriteSetting(string section, string key, string value);

    /// <summary>
    /// Reads a plain line file from the data folder, empty when missing,
    /// like the legacy favorites.txt and pinglist.txt
    /// </summary>
    IReadOnlyList<string> ReadDataLines(string fileName);

    /// <summary>
    /// Writes a plain line file in the data folder
    /// </summary>
    void WriteDataLines(string fileName, IReadOnlyList<string> lines);
}
