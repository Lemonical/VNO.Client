using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace VNO.Client.Services;

/// <summary>
/// Maps a badge id to its bundled icon and caches the loaded bitmap
/// </summary>
/// <remarks>
/// Badges are app content, staff, vip, and special markers, not player theme content,
/// so they ship embedded under Assets/images/badges. A staff member assigns a badge to
/// a player and the server injects the badge id into that player's in character line,
/// which the stage badge layer renders
/// </remarks>
public static class BadgeCatalog
{
    private const string Root = "avares://VNO.Client/Assets/images/badges/";

    // known badge ids and their file, the id is the file base name so it is stable
    private static readonly IReadOnlyDictionary<string, string> Paths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mod"] = "common/mod.png",
            ["animator"] = "common/animator.png",
            ["champ"] = "common/champ.png",
            ["communityhero"] = "vip/communityhero.png",
            ["vip_1"] = "vip/vip_1.png",
            ["vip_2"] = "vip/vip_2.png",
            ["vip_3"] = "vip/vip_3.png",
            ["vip_4"] = "vip/vip_4.png",
            ["vip_angel"] = "vip/vip_angel.png",
            ["vip_burning"] = "vip/vip_burning.png",
            ["vip_cold"] = "vip/vip_cold.png",
            ["vip_daemon"] = "vip/vip_daemon.png",
            ["vip_demon"] = "vip/vip_demon.png",
            ["vip_grandmaster"] = "vip/vip_grandmaster.png",
            ["vip_passion"] = "vip/vip_passion.png",
            ["fireball"] = "specials/fireball.png",
            ["hadoken"] = "specials/hadoken.png",
            ["ut"] = "specials/ut.png",
        };

    private static readonly Dictionary<string, Bitmap> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The known badge ids, for the animator badge picker
    /// </summary>
    public static IReadOnlyCollection<string> Ids => (IReadOnlyCollection<string>)Paths.Keys;

    /// <summary>
    /// True when the id names a known badge
    /// </summary>
    public static bool IsKnown(string? badgeId) =>
        !string.IsNullOrWhiteSpace(badgeId) && Paths.ContainsKey(badgeId);

    /// <summary>
    /// Loads the icon for a badge id, null when the id is empty or unknown
    /// </summary>
    public static Bitmap? Load(string? badgeId)
    {
        if (string.IsNullOrWhiteSpace(badgeId) || !Paths.TryGetValue(badgeId, out var relative))
        {
            return null;
        }

        if (Cache.TryGetValue(badgeId, out var cached))
        {
            return cached;
        }

        try
        {
            var bitmap = new Bitmap(AssetLoader.Open(new Uri(Root + relative)));
            Cache[badgeId] = bitmap;
            return bitmap;
        }
        catch (Exception)
        {
            // a missing or unreadable asset just means no badge is shown
            return null;
        }
    }
}
