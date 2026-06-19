using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace VNO.Client.Services;

/// <summary>
/// One roster entry, a character the player can pick
/// </summary>
/// <remarks>
/// Mirrors what the legacy client drew per grid cell. The off and on images are
/// the two roster states, taken characters show the on image. Big art is the
/// large preview shown at the right when the slot is selected
/// </remarks>
public sealed class RosterCharacter
{
    /// <summary>Folder name of the character</summary>
    public required string Name { get; init; }

    /// <summary>Grid cell image for an available character</summary>
    public Bitmap? OffImage { get; init; }

    /// <summary>Grid cell image for a taken character</summary>
    public Bitmap? OnImage { get; init; }

    /// <summary>Large preview art, null when the character has none</summary>
    public Bitmap? BigArt { get; init; }
}

/// <summary>
/// Loads the character roster from the legacy data folder layout
/// </summary>
/// <remarks>
/// The legacy client drew each grid cell from data\misc\RosterImage\&lt;name&gt;_off.png
/// or _on.png and the preview from data\misc\BigArt\&lt;name&gt;_BigArt.png, with the
/// roster list itself coming from the server. Until the game server streams that
/// list, the roster is read from the local RosterImage folder so the grid is
/// populated from real external content, never hard coded
/// </remarks>
public interface ICharacterRosterService
{
    /// <summary>
    /// Loads the roster, empty when the data folder has no roster images
    /// </summary>
    IReadOnlyList<RosterCharacter> Load();
}
