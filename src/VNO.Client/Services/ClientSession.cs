using System;
using System.Collections.Generic;

namespace VNO.Client.Services;

/// <summary>
/// Default in memory session shared across screens
/// </summary>
public sealed class ClientSession : IClientSession
{
    private string? _selectedCharacter;
    private IReadOnlyList<string> _serverRoster = Array.Empty<string>();
    private IReadOnlyCollection<string> _takenCharacters = Array.Empty<string>();
    private readonly Dictionary<string, string> _badges = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (_selectedCharacter == value)
            {
                return;
            }
            _selectedCharacter = value;
            SelectedCharacterChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ServerRoster
    {
        get => _serverRoster;
        set
        {
            _serverRoster = value ?? Array.Empty<string>();
            ServerRosterChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> TakenCharacters
    {
        get => _takenCharacters;
        set
        {
            _takenCharacters = value ?? Array.Empty<string>();
            TakenCharactersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Badges => _badges;

    /// <inheritdoc />
    public void SetBadge(string name, string badge)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        _badges[name] = badge;
    }

    /// <inheritdoc />
    public event EventHandler? SelectedCharacterChanged;

    /// <inheritdoc />
    public event EventHandler? ServerRosterChanged;

    /// <inheritdoc />
    public event EventHandler? TakenCharactersChanged;
}
