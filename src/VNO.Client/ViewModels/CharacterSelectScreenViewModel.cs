using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VNO.Client.Services;

namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the character select screen
/// </summary>
/// <remarks>
/// Ports groupbox_charselect from Form15. The roster is a paged five by five grid
/// (twenty five cells per page like the skin), each cell drawn from the theme
/// roster image. Selecting a slot shows its big art and name, taken slots are
/// blocked, and Random rolls an available one. The roster loads from the local
/// data folder until the game server streams a per server list
/// </remarks>
public sealed partial class CharacterSelectScreenViewModel : ViewModelBase
{
    // the charselect skin draws a five by five grid, so a page holds 25 slots
    private const int PageColumns = 5;
    private const int PageRows = 5;
    private const int PageSize = PageColumns * PageRows;

    private readonly IClientNavigator _navigator;
    private readonly IClientSession _session;
    private readonly IServerConnection _server;
    private readonly List<CharacterSlotViewModel> _roster = new();
    private readonly Dictionary<string, RosterCharacter> _localByName = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedName))]
    [NotifyPropertyChangedFor(nameof(SelectedBigArt))]
    private CharacterSlotViewModel? _selectedCharacter;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPage1))]
    [NotifyPropertyChangedFor(nameof(IsPage2))]
    [NotifyPropertyChangedFor(nameof(IsPage3))]
    [NotifyPropertyChangedFor(nameof(IsPage4))]
    private int _page = 1;

    [ObservableProperty]
    private string _typedName = string.Empty;

    [ObservableProperty]
    private string _characterPassword = string.Empty;

    [ObservableProperty]
    private bool _showTakenNotice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPage2))]
    [NotifyPropertyChangedFor(nameof(HasPage3))]
    [NotifyPropertyChangedFor(nameof(HasPage4))]
    private int _pageCount = 1;

    /// <summary>
    /// Creates the screen over the navigator, the theme, and the roster loader
    /// </summary>
    public CharacterSelectScreenViewModel(
        IClientNavigator navigator,
        IThemeService theme,
        ICharacterRosterService roster,
        IClientSession session,
        IServerConnection server)
    {
        _navigator = navigator;
        _session = session;
        _server = server;
        Theme = theme;

        // index the local roster by name so the server roster can pick from it
        foreach (var character in roster.Load())
        {
            _localByName[character.Name] = character;
        }

        BuildRoster();

        // if the server sends its roster after this screen is built, rebuild.
        // the session mutations are already marshaled to the UI thread by the
        // server list view model, so these run on the UI thread
        _session.ServerRosterChanged += (_, _) => BuildRoster();
        // taken flags arrive as players claim characters, grey those slots
        _session.TakenCharactersChanged += (_, _) => ApplyTakenFlags();
    }

    private void ApplyTakenFlags()
    {
        var taken = _session.TakenCharacters;
        foreach (var slot in _roster)
        {
            slot.IsTaken = taken.Contains(slot.Name);
        }
    }

    private void BuildRoster()
    {
        _roster.Clear();

        // prefer the server roster, falling back to the full local roster. A
        // server named character with no local image still gets a slot
        var names = _session.ServerRoster.Count > 0
            ? _session.ServerRoster
            : _localByName.Keys.ToList();

        var page = 1;
        var indexOnPage = 0;
        foreach (var name in names)
        {
            _localByName.TryGetValue(name, out var character);
            _roster.Add(new CharacterSlotViewModel
            {
                Name = name,
                Page = page,
                OffImage = character?.OffImage,
                OnImage = character?.OnImage,
                BigArt = character?.BigArt,
            });
            if (++indexOnPage == PageSize)
            {
                indexOnPage = 0;
                page++;
            }
        }

        PageCount = Math.Max(1, (_roster.Count + PageSize - 1) / PageSize);
        if (Page > PageCount)
        {
            Page = 1;
        }
        ApplyTakenFlags();
        UpdatePageSlots();
    }

    /// <summary>
    /// The player theme, views resolve their skin images through it
    /// </summary>
    public IThemeService Theme { get; }

    /// <summary>
    /// The slots shown on the current page, always padded to a full grid
    /// </summary>
    public ObservableCollection<CharacterSlotViewModel?> PageSlots { get; } = new();

    /// <summary>True when the roster has a second page</summary>
    public bool HasPage2 => PageCount >= 2;

    /// <summary>True when the roster has a third page</summary>
    public bool HasPage3 => PageCount >= 3;

    /// <summary>True when the roster has a fourth page</summary>
    public bool HasPage4 => PageCount >= 4;

    /// <summary>Name of the selected character, blank when none</summary>
    public string SelectedName => SelectedCharacter?.Name ?? string.Empty;

    /// <summary>Big art of the selected character</summary>
    public Bitmap? SelectedBigArt => SelectedCharacter?.BigArt;

    /// <summary>True when the matching radio page is active</summary>
    public bool IsPage1 => Page == 1;

    /// <summary>True when the matching radio page is active</summary>
    public bool IsPage2 => Page == 2;

    /// <summary>True when the matching radio page is active</summary>
    public bool IsPage3 => Page == 3;

    /// <summary>True when the matching radio page is active</summary>
    public bool IsPage4 => Page == 4;

    partial void OnSelectedCharacterChanged(CharacterSlotViewModel? value) =>
        ShowTakenNotice = value?.IsTaken == true;

    partial void OnPageChanged(int value) => UpdatePageSlots();

    private void UpdatePageSlots()
    {
        PageSlots.Clear();
        var start = (Page - 1) * PageSize;
        for (var i = 0; i < PageSize; i++)
        {
            var index = start + i;
            PageSlots.Add(index < _roster.Count ? _roster[index] : null);
        }
    }

    [RelayCommand]
    private void ShowPage(string page)
    {
        // the page buttons pass their number as a string command parameter
        if (int.TryParse(page, out var number) && number >= 1 && number <= PageCount)
        {
            Page = number;
        }
    }

    [RelayCommand]
    private void Random()
    {
        // roll the first available slot, jumping to its page like the original
        var slot = _roster.FirstOrDefault(s => !s.IsTaken);
        if (slot is not null)
        {
            Page = slot.Page;
            SelectedCharacter = slot;
        }
    }

    [RelayCommand]
    private void SelectSlot(CharacterSlotViewModel? slot)
    {
        // clicking an empty grid cell does nothing, like a blank roster tile
        if (slot is not null)
        {
            SelectedCharacter = slot;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private void Select()
    {
        var name = SelectedCharacter?.Name;
        // claim the character on the server so other players see it taken
        if (!string.IsNullOrEmpty(name))
        {
            _ = _server.SendAsync(new VNO.Core.Protocol.NetworkMessage(
                VNO.Core.Protocol.MessageType.PickCharacter, name));
        }
        // hand the chosen character to the stage through the shared session
        _session.SelectedCharacter = name;
        _navigator.ShowGameStage();
    }

    private bool CanSelect() => SelectedCharacter is { IsTaken: false };
}
