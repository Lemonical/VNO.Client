using System;
using System.Collections.Generic;

namespace VNO.Client.Services;

/// <summary>
/// Shared per play session state passed between screens
/// </summary>
/// <remarks>
/// The legacy client held the chosen character on the single Form15, so the stage
/// could read it directly. With one view model per screen the selection is shared
/// through this session so character select can hand the stage the picked
/// character folder name
/// </remarks>
public interface IClientSession
{
    /// <summary>
    /// Folder name of the character the player picked, null before selection
    /// </summary>
    string? SelectedCharacter { get; set; }

    /// <summary>
    /// Roster the game server sent for this session, empty until a CharacterList
    /// arrives. Character select prefers this over the local roster
    /// </summary>
    IReadOnlyList<string> ServerRoster { get; set; }

    /// <summary>
    /// Raised when the selected character changes
    /// </summary>
    event EventHandler? SelectedCharacterChanged;

    /// <summary>
    /// Names of characters currently claimed by players, from the server
    /// </summary>
    IReadOnlyCollection<string> TakenCharacters { get; set; }

    /// <summary>
    /// Raised when the server roster changes
    /// </summary>
    event EventHandler? ServerRosterChanged;

    /// <summary>
    /// Raised when the taken character set changes
    /// </summary>
    event EventHandler? TakenCharactersChanged;

    /// <summary>
    /// Badge roster the auth server pushed at login, a shown name maps to a badge id.
    /// The stage draws that badge next to anyone speaking under the name, lookups are
    /// case insensitive
    /// </summary>
    IReadOnlyDictionary<string, string> Badges { get; }

    /// <summary>
    /// Records a badge grant received from the auth server
    /// </summary>
    void SetBadge(string name, string badge);
}
