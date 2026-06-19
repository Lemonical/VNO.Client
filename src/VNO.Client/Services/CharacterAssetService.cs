using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VNO.Client.Services;

/// <summary>
/// Default loader for character poses and scene backgrounds
/// </summary>
/// <remarks>
/// Reads data\characters\&lt;name&gt;\char.ini and Default.ini the way the legacy
/// client did, then loads each pose png and its buttons thumbnail. Every image
/// load is guarded like the legacy TPicture.LoadFromFile calls
/// </remarks>
public sealed class CharacterAssetService : ICharacterAssetService
{
    private const string CharactersFolder = "characters";
    private const string BackgroundFolder = "background";
    private const string ButtonsFolder = "buttons";

    private readonly string _dataDirectory;
    private readonly ILogger<CharacterAssetService> _logger;

    /// <summary>
    /// Creates the service over the configured data folder
    /// </summary>
    public CharacterAssetService(IOptions<ClientSettings> options, ILogger<CharacterAssetService> logger)
    {
        _dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, options.Value.DataDirectory));
        _logger = logger;
    }

    /// <inheritdoc />
    public LoadedCharacter? LoadCharacter(string folderName)
    {
        var charFolder = Path.Combine(_dataDirectory, CharactersFolder, folderName);
        if (!Directory.Exists(charFolder))
        {
            _logger.LogWarning("Character folder {Path} not found", charFolder);
            return null;
        }

        var charIni = DelphiIniFile.Load(Path.Combine(charFolder, "char.ini"));
        var showName = charIni.ReadString("Options", "showname", folderName);

        // the emote list lives in Default.ini [Emotions], numbered 1..N
        var emoteIni = DelphiIniFile.Load(Path.Combine(charFolder, "Default.ini"));
        var count = emoteIni.ReadInteger("Emotions", "number", 0);

        var emotes = new List<CharacterEmote>();
        for (var i = 1; i <= count; i++)
        {
            var name = emoteIni.ReadString(
                "Emotions", i.ToString(System.Globalization.CultureInfo.InvariantCulture), string.Empty);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }
            emotes.Add(new CharacterEmote
            {
                Name = name,
                Sprite = TryLoad(Path.Combine(charFolder, name + ".png")),
                Thumbnail = TryLoad(Path.Combine(charFolder, ButtonsFolder, name + ".png")),
            });
        }

        return new LoadedCharacter { Name = folderName, ShowName = showName, Emotes = emotes };
    }

    /// <inheritdoc />
    public Bitmap? LoadBackground(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        var path = Path.Combine(_dataDirectory, BackgroundFolder, name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? name : name + ".png");
        return TryLoad(path);
    }

    /// <inheritdoc />
    public Bitmap? LoadDefaultBackground()
    {
        var folder = Path.Combine(_dataDirectory, BackgroundFolder);
        if (!Directory.Exists(folder))
        {
            return null;
        }
        foreach (var file in Directory.EnumerateFiles(folder, "*.png"))
        {
            return TryLoad(file);
        }
        return null;
    }

    private Bitmap? TryLoad(string path)
    {
        try
        {
            return File.Exists(path) ? new Bitmap(path) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load character image {Path}", path);
            return null;
        }
    }
}
