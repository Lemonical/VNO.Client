using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VNO.Client.Services;

/// <summary>
/// Default roster loader over the legacy data folder
/// </summary>
/// <remarks>
/// Reads the roster from data\misc\RosterImage. Every character with an _off.png
/// contributes a slot, its _on.png and BigArt are loaded when present. Missing
/// files are tolerated the way the legacy loader guarded each TPicture.LoadFromFile
/// </remarks>
public sealed class CharacterRosterService : ICharacterRosterService
{
    private const string RosterFolder = "misc/RosterImage";
    private const string BigArtFolder = "misc/BigArt";
    private const string OffSuffix = "_off.png";
    private const string OnSuffix = "_on.png";

    private readonly string _dataDirectory;
    private readonly ILogger<CharacterRosterService> _logger;

    /// <summary>
    /// Creates the service over the configured data folder
    /// </summary>
    public CharacterRosterService(IOptions<ClientSettings> options, ILogger<CharacterRosterService> logger)
    {
        _dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, options.Value.DataDirectory));
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<RosterCharacter> Load()
    {
        var rosterPath = Path.Combine(_dataDirectory, RosterFolder);
        if (!Directory.Exists(rosterPath))
        {
            _logger.LogWarning("Roster folder {Path} not found, the character grid will be empty", rosterPath);
            return Array.Empty<RosterCharacter>();
        }

        var result = new List<RosterCharacter>();
        foreach (var offFile in Directory.EnumerateFiles(rosterPath, "*" + OffSuffix)
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(offFile);
            name = name[..^OffSuffix.Length];

            result.Add(new RosterCharacter
            {
                Name = name,
                OffImage = TryLoad(offFile),
                OnImage = TryLoad(Path.Combine(rosterPath, name + OnSuffix)),
                BigArt = LoadBigArt(name),
            });
        }

        return result;
    }

    private Bitmap? LoadBigArt(string name)
    {
        // BigArt files are inconsistently cased in the shipped data, try both
        var folder = Path.Combine(_dataDirectory, BigArtFolder);
        foreach (var suffix in new[] { "_BigArt.png", "_bigart.png" })
        {
            var candidate = Path.Combine(folder, name + suffix);
            if (File.Exists(candidate))
            {
                return TryLoad(candidate);
            }
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
            _logger.LogWarning(ex, "Failed to load roster image {Path}", path);
            return null;
        }
    }
}
