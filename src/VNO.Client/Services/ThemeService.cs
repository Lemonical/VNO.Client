using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VNO.Client.Services;

/// <summary>
/// Loads the player supplied UI theme from the legacy data folder layout
/// </summary>
/// <remarks>
/// Mirrors the legacy FormCreate flow. The data folder sits next to the
/// executable unless overridden in configuration, settings.ini names the design,
/// and data\UI\&lt;design&gt;\ provides design.ini plus the skin images. The
/// original wrapped every image load in an exception handler and read every ini
/// key with a default, so a partial or missing theme degrades instead of failing
/// </remarks>
public sealed class ThemeService : IThemeService
{
    // legacy defaults from the decompiled FormCreate and design reader
    private const string DefaultDesign = "twewy";
    private const string DefaultColorText = "$FFFFFF";
    private const string DefaultFontName = "Tahoma";
    private const double DefaultFontPoints = 10;

    private readonly ILogger<ThemeService> _logger;
    private readonly string _dataDirectory;
    private readonly DelphiIniFile _settings;
    private readonly DelphiIniFile _design;
    private readonly Dictionary<string, Bitmap?> _images = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string DesignName { get; }

    /// <summary>
    /// Creates the service and loads settings.ini and the active design.ini once,
    /// like the original did at form creation
    /// </summary>
    public ThemeService(IOptions<ClientSettings> options, ILogger<ThemeService> logger)
    {
        _logger = logger;
        _dataDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, options.Value.DataDirectory));

        _settings = DelphiIniFile.Load(Path.Combine(_dataDirectory, "settings.ini"));
        DesignName = _settings.ReadString("DesignStyle", "design", DefaultDesign);
        _design = DelphiIniFile.Load(Path.Combine(ThemeDirectory, "design.ini"));

        if (!Directory.Exists(ThemeDirectory))
        {
            _logger.LogWarning(
                "Theme folder {ThemeDirectory} not found, the UI will use fallback colors and no skin images",
                ThemeDirectory);
        }
    }

    private string ThemeDirectory => Path.Combine(_dataDirectory, "UI", DesignName);

    /// <inheritdoc />
    public Bitmap? GetImage(string relativePath)
    {
        if (_images.TryGetValue(relativePath, out var cached))
        {
            return cached;
        }

        Bitmap? bitmap = null;
        var path = Path.Combine(ThemeDirectory, relativePath);
        try
        {
            if (File.Exists(path))
            {
                bitmap = new Bitmap(path);
            }
        }
        catch (Exception ex)
        {
            // the original swallowed load failures per image, keep that tolerance
            _logger.LogWarning(ex, "Failed to load theme image {Path}", path);
        }

        _images[relativePath] = bitmap;
        return bitmap;
    }

    /// <inheritdoc />
    public Color GetColor(string key)
    {
        var text = _design.ReadString("ObjectColor", key, DefaultColorText);
        return DelphiColor.Parse(text, Colors.White);
    }

    /// <inheritdoc />
    public IBrush GetBrush(string key) => new SolidColorBrush(GetColor(key));

    /// <inheritdoc />
    public FontFamily GetFontFamily(string key)
    {
        var name = _design.ReadString("Font", key, DefaultFontName);
        // unknown families fall through to the platform default at render time
        return new FontFamily($"{name}, {DefaultFontName}, sans-serif");
    }

    /// <inheritdoc />
    public double GetFontSize(string key)
    {
        var points = _design.ReadDouble("Font", key + "_size", DefaultFontPoints);
        if (points <= 0)
        {
            points = DefaultFontPoints;
        }
        // Delphi font sizes are points at 96 dpi, Avalonia font sizes are pixels
        return points * 4.0 / 3.0;
    }

    /// <inheritdoc />
    public int GetDesignInteger(string section, string key, int fallback) =>
        _design.ReadInteger(section, key, fallback);

    /// <inheritdoc />
    public string ReadSetting(string section, string key, string fallback) =>
        _settings.ReadString(section, key, fallback);

    /// <inheritdoc />
    public int ReadSettingInteger(string section, string key, int fallback) =>
        _settings.ReadInteger(section, key, fallback);

    /// <inheritdoc />
    public IReadOnlyList<string> ReadDataLines(string fileName)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        try
        {
            return File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read data file {Path}", path);
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public void WriteDataLines(string fileName, IReadOnlyList<string> lines)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        try
        {
            Directory.CreateDirectory(_dataDirectory);
            File.WriteAllLines(path, lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write data file {Path}", path);
        }
    }

    /// <inheritdoc />
    public void WriteSetting(string section, string key, string value)
    {
        _settings.SetValue(section, key, value);
        try
        {
            DelphiIniFile.WriteValue(Path.Combine(_dataDirectory, "settings.ini"), section, key, value);
        }
        catch (Exception ex)
        {
            // losing a settings write is not fatal, match the legacy tolerance
            _logger.LogWarning(ex, "Failed to write settings.ini");
        }
    }
}
