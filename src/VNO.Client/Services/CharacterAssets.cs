using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace VNO.Client.Services;

/// <summary>
/// One selectable emote of a character
/// </summary>
/// <remarks>
/// Mirrors an entry of the legacy Default.ini [Emotions] list. The sprite is the
/// full viewport pose drawn over the background, the thumbnail is the small
/// buttons image shown in the emote picker
/// </remarks>
public sealed class CharacterEmote
{
    /// <summary>Emote id from the ini, its png base name</summary>
    public required string Name { get; init; }

    /// <summary>Full pose sprite, drawn over the scene background</summary>
    public Bitmap? Sprite { get; init; }

    /// <summary>Small picker thumbnail from the buttons folder</summary>
    public Bitmap? Thumbnail { get; init; }
}

/// <summary>
/// A character loaded from its data folder, ready to show on the stage
/// </summary>
public sealed class LoadedCharacter
{
    /// <summary>Folder name of the character</summary>
    public required string Name { get; init; }

    /// <summary>Display name from char.ini showname, falls back to the folder name</summary>
    public required string ShowName { get; init; }

    /// <summary>The emotes in ini order</summary>
    public required IReadOnlyList<CharacterEmote> Emotes { get; init; }
}
