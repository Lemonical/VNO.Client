using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VNO.Client.Services;
using VNO.Core.Models;
using VNO.Core.Protocol;

namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the in game stage
/// </summary>
/// <remarks>
/// Ports the playable part of Form15. It owns the in character textbox, the OOC,
/// events, and errors feeds, the music and area lists, the player stats, and the
/// emote controls. It opens the staff windows on request
/// </remarks>
public sealed partial class GameStageViewModel : ViewModelBase
{
    private readonly IServerConnection _server;
    private readonly IWindowService _windows;
    private readonly IClientSession _session;
    private readonly ICharacterAssetService _assets;
    private readonly IImageFetcher _images;
    private readonly IAudioService _audio;

    private DispatcherTimer? _chatTimer;
    private string _fullChatLine = string.Empty;
    private int _revealIndex;

    private DispatcherTimer? _countdownTimer;
    private int _countdownRemaining;

    private DispatcherTimer? _effectTimer;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _inCharacterText = string.Empty;

    // the continue arrow shows once a line has fully revealed, the legacy img_arrow
    [ObservableProperty]
    private bool _isChatComplete;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEventsFeed))]
    [NotifyPropertyChangedFor(nameof(IsOocFeed))]
    [NotifyPropertyChangedFor(nameof(IsErrorsFeed))]
    private ChatFeed _activeFeed = ChatFeed.Events;

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string _oocInput = string.Empty;

    [ObservableProperty]
    private string _dice = "2d6";

    [ObservableProperty]
    private double _textColor;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _clockText = string.Empty;

    [ObservableProperty]
    private double _musicVolume = 50;

    [ObservableProperty]
    private double _sfxVolume = 50;

    [ObservableProperty]
    private double _blipVolume = 50;

    [ObservableProperty]
    private int _credits;

    [ObservableProperty]
    private Bitmap? _broadcastImage;

    [ObservableProperty]
    private Bitmap? _sceneBackground;

    [ObservableProperty]
    private Bitmap? _characterSprite;

    // the scene badge shown for the speaking character, null hides the layer
    [ObservableProperty]
    private Bitmap? _badgeImage;

    // the image_effect overlay, a staff triggered scene effect, null hides the layer
    [ObservableProperty]
    private Bitmap? _effectImage;

    /// <summary>
    /// Creates the game stage over its services
    /// </summary>
    public GameStageViewModel(
        IServerConnection server,
        IWindowService windows,
        IThemeService theme,
        IClientSession session,
        ICharacterAssetService assets,
        IImageFetcher images,
        IAudioService audio)
    {
        _server = server;
        _windows = windows;
        _session = session;
        _assets = assets;
        _images = images;
        _audio = audio;
        Theme = theme;

        // the badge layer sits at the legacy coordinates shifted by the theme offsets
        BadgeLeft = 35 + theme.GetDesignInteger("Placement", "badge_leftdiv", 0);
        BadgeTop = 187 + theme.GetDesignInteger("Placement", "badge_updiv", 0);

        _server.MessageReceived += OnMessageReceived;
        _server.StateChanged += OnStateChanged;

        // start on a default scene background until the server sets one
        SceneBackground = _assets.LoadDefaultBackground();

        // load the character the player picked, and reload if it changes
        _session.SelectedCharacterChanged += (_, _) => Dispatcher.UIThread.Post(LoadSelectedCharacter);
        LoadSelectedCharacter();
    }

    private void LoadSelectedCharacter()
    {
        Emotes.Clear();
        var folder = _session.SelectedCharacter;
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        var character = _assets.LoadCharacter(folder);
        if (character is null)
        {
            return;
        }

        CharacterName = character.ShowName;
        foreach (var emote in character.Emotes)
        {
            Emotes.Add(emote);
        }

        // show the first emote as the standing pose, like the legacy default
        CharacterSprite = character.Emotes.FirstOrDefault(e => e.Sprite is not null)?.Sprite;
    }

    /// <summary>
    /// The player theme, views resolve their skin images through it
    /// </summary>
    public IThemeService Theme { get; }

    /// <summary>
    /// Stage badge layer X, the legacy badge @35 plus the theme badge_leftdiv offset
    /// </summary>
    public double BadgeLeft { get; }

    /// <summary>
    /// Stage badge layer Y, the legacy badge @187 plus the theme badge_updiv offset
    /// </summary>
    public double BadgeTop { get; }

    /// <summary>True when the events feed is shown</summary>
    public bool IsEventsFeed => ActiveFeed == ChatFeed.Events;

    /// <summary>True when the OOC feed is shown</summary>
    public bool IsOocFeed => ActiveFeed == ChatFeed.Ooc;

    /// <summary>True when the errors feed is shown</summary>
    public bool IsErrorsFeed => ActiveFeed == ChatFeed.Errors;

    /// <summary>
    /// The events side channel feed
    /// </summary>
    public ObservableCollection<string> Events { get; } = new();

    /// <summary>
    /// The out of character feed
    /// </summary>
    public ObservableCollection<string> Ooc { get; } = new();

    /// <summary>
    /// The error and debug feed
    /// </summary>
    public ObservableCollection<string> Errors { get; } = new();

    /// <summary>
    /// The music track list
    /// </summary>
    public ObservableCollection<MusicTrack> Music { get; } = new();

    /// <summary>
    /// The area list
    /// </summary>
    public ObservableCollection<Area> Areas { get; } = new();

    /// <summary>
    /// The players present in the current area
    /// </summary>
    public ObservableCollection<string> Users { get; } = new();

    /// <summary>
    /// Items the player has been granted
    /// </summary>
    public ObservableCollection<InventoryItem> Inventory { get; } = new();

    /// <summary>
    /// The replay buffer shown below the stage
    /// </summary>
    public ObservableCollection<string> Replay { get; } = new();

    /// <summary>
    /// The character emotes available to the player
    /// </summary>
    public ObservableCollection<CharacterEmote> Emotes { get; } = new();

    /// <summary>
    /// The local player stats driving the HP and mana gauges
    /// </summary>
    public PlayerStats Stats { get; } = new();

    [RelayCommand]
    private void ShowEvents() => ActiveFeed = ChatFeed.Events;

    [RelayCommand]
    private void ShowOoc() => ActiveFeed = ChatFeed.Ooc;

    [RelayCommand]
    private void ShowErrors() => ActiveFeed = ChatFeed.Errors;

    [RelayCommand]
    private async Task SendInCharacterAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageInput))
        {
            return;
        }

        // in character lines carry the chosen character name, unlike OOC
        var name = string.IsNullOrEmpty(CharacterName) ? "Player" : CharacterName;
        await _server.SendInCharacterAsync(name, MessageInput).ConfigureAwait(false);
        MessageInput = string.Empty;
    }

    [RelayCommand]
    private async Task SendOutOfCharacterAsync()
    {
        if (string.IsNullOrWhiteSpace(OocInput))
        {
            return;
        }

        await _server.SendChatAsync(OocInput).ConfigureAwait(false);
        OocInput = string.Empty;
    }

    [RelayCommand]
    private void SelectEmote(CharacterEmote? emote)
    {
        // switching emote swaps the standing pose sprite, the legacy emote pick
        if (emote?.Sprite is not null)
        {
            CharacterSprite = emote.Sprite;
        }
    }

    [RelayCommand]
    private async Task RollDiceAsync()
    {
        // roll the dice box expression and announce the result to the room, the
        // legacy dice roll broadcast an out of character line
        var result = VNO.Core.DiceRoller.Roll(Dice);
        if (result is null)
        {
            Add(Errors, $"Bad dice expression: {Dice}");
            return;
        }

        var who = string.IsNullOrEmpty(CharacterName) ? "Someone" : CharacterName;
        var rolls = string.Join(", ", result.Rolls);
        var line = $"{who} rolled {result.Expression}: [{rolls}] = {result.Total}";
        Add(Ooc, line);
        await _server.SendChatAsync(line).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task JoinAreaAsync(Area? area)
    {
        // double tapping an area asks the server to move us there
        if (area is null)
        {
            return;
        }
        var index = Areas.IndexOf(area);
        if (index >= 0)
        {
            await _server.SendAsync(new NetworkMessage(
                MessageType.JoinArea, index.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                .ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task PlayMusicAsync(MusicTrack? track)
    {
        // selecting a track broadcasts it, the legacy music change
        if (track is not null)
        {
            await _server.SendAsync(new NetworkMessage(MessageType.Music, track.Name, CharacterName))
                .ConfigureAwait(false);
        }
    }

    private void RecordReplay(string line)
    {
        // capture the scene traffic so it can be saved and played back, the
        // legacy Memo_replay buffer, capped so it never grows without bound
        Replay.Add(line);
        while (Replay.Count > 5000)
        {
            Replay.RemoveAt(0);
        }
    }

    [RelayCommand]
    private void SaveReplay()
    {
        // write the recorded buffer to a timestamped file in the data folder,
        // the legacy Save Replay button
        if (Replay.Count == 0)
        {
            Add(Errors, "Nothing to save yet");
            return;
        }
        var name = $"replay-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        Theme.WriteDataLines(name, Replay.ToArray());
        Add(Events, $"* Replay saved to {name}");
    }

    [RelayCommand]
    private void OpenModerator() => _windows.ShowModerator();

    [RelayCommand]
    private void OpenAnimator() => _windows.ShowAnimator();

    /// <summary>
    /// True when the player hid themselves from the area roster
    /// </summary>
    [ObservableProperty]
    private bool _isSelfHidden;

    [RelayCommand]
    private async Task ToggleHideAsync()
    {
        // the server only honors this when staff enabled hiding, it enforces the policy
        IsSelfHidden = !IsSelfHidden;
        await _server.SendAsync(new NetworkMessage(MessageType.HideSelf, IsSelfHidden ? "on" : "off"))
            .ConfigureAwait(false);
    }

    private void OnMessageReceived(object? sender, NetworkMessage message) =>
        Dispatcher.UIThread.Post(() => HandleMessage(message));

    private void HandleMessage(NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.InCharacter:
                CharacterName = message.GetArgument(0);
                BeginTypewriter(message.GetArgument(1));
                // badges are owned by the master and delivered to us at login, look the
                // speaker's shown name up in that roster, an unknown name clears the layer
                BadgeImage = _session.Badges.TryGetValue(message.GetArgument(0), out var badgeId)
                    ? BadgeCatalog.Load(badgeId)
                    : null;
                Add(Events, $"{message.GetArgument(0)}: {message.GetArgument(1)}");
                RecordReplay($"IC|{message.GetArgument(0)}|{message.GetArgument(1)}");
                break;
            case MessageType.OutOfCharacter:
                Add(Ooc, message.GetArgument(0));
                RecordReplay($"OOC|{message.GetArgument(0)}");
                break;
            case MessageType.Music:
                HandleMusic(message);
                break;
            case MessageType.MusicList:
                PopulateMusic(message);
                break;
            case MessageType.AreaList:
                PopulateAreas(message);
                break;
            case MessageType.UserList:
                PopulateUsers(message);
                break;
            case MessageType.StatChange:
                ApplyStatChange(message);
                break;
            case MessageType.Timer:
                StartCountdown(message);
                break;
            case MessageType.StreamImage:
                ShowBroadcastImage(message.GetArgument(0));
                break;
            case MessageType.StreamMusic:
                // a staff forced music stream, announce it and play it
                Add(Events, $"♪ Streaming: {message.GetArgument(0)}");
                _audio.PlayMusic(message.GetArgument(0));
                break;
            case MessageType.SceneEffect:
                ShowSceneEffect(message.GetArgument(0));
                break;
            case MessageType.GiveItem:
                ReceiveItem(message);
                break;
            case MessageType.Notice:
                Add(Ooc, $"* {message.GetArgument(0)}");
                break;
            case MessageType.Kick:
                Add(Errors, $"You were kicked: {message.GetArgument(0)}");
                break;
            case MessageType.Mute:
                Add(Errors, "You were muted");
                break;
            case MessageType.Unmute:
                Add(Errors, "You were unmuted");
                break;
            default:
                // traffic the stage does not surface
                break;
        }
    }

    private void BeginTypewriter(string text)
    {
        // reveal the line one character at a time, the legacy timer_chat effect
        _chatTimer?.Stop();
        _fullChatLine = text ?? string.Empty;
        _revealIndex = 0;
        InCharacterText = string.Empty;
        // hide the continue arrow while the line is still typing out
        IsChatComplete = false;

        if (_fullChatLine.Length == 0)
        {
            IsChatComplete = true;
            return;
        }

        _chatTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _chatTimer.Tick -= OnChatTick;
        _chatTimer.Tick += OnChatTick;
        _chatTimer.Start();
    }

    // the volume sliders run 0 to 100, the audio service takes 0 to 1
    partial void OnMusicVolumeChanged(double value) => _audio.SetMusicVolume(value / 100.0);

    partial void OnSfxVolumeChanged(double value) => _audio.SetSfxVolume(value / 100.0);

    partial void OnBlipVolumeChanged(double value) => _audio.SetBlipVolume(value / 100.0);

    private void OnChatTick(object? sender, EventArgs e)
    {
        if (_revealIndex >= _fullChatLine.Length)
        {
            _chatTimer?.Stop();
            // the whole line is shown, reveal the continue arrow
            IsChatComplete = true;
            return;
        }
        _revealIndex++;
        InCharacterText = _fullChatLine[.._revealIndex];
        // the legacy timer_blips sounded a blip as each character appeared, skip
        // spaces so the effect matches the original cadence
        if (_fullChatLine[_revealIndex - 1] != ' ')
        {
            _audio.PlayBlip();
        }
    }

    private void ShowBroadcastImage(string url)
    {
        Add(Events, $"* Image streamed: {url}");
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        // fetch the image off the UI thread, local files load directly and remote
        // urls go through the guarded fetcher, then marshal back to set it
        _ = FetchBroadcastImageAsync(url);
    }

    private async Task FetchBroadcastImageAsync(string url)
    {
        var bitmap = await _images.FetchAsync(url).ConfigureAwait(false);
        if (bitmap is not null)
        {
            Dispatcher.UIThread.Post(() => BroadcastImage = bitmap);
        }
    }

    /// <summary>
    /// Clears the streamed image overlay, the legacy right click to dismiss
    /// </summary>
    [RelayCommand]
    private void ClearBroadcastImage() => BroadcastImage = null;

    private void ShowSceneEffect(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            EffectImage = null;
            return;
        }

        // effects are data\background\effect_<name>.png, the legacy image_effect layer.
        // a missing file just means nothing plays
        Add(Events, $"* Effect: {name}");
        EffectImage = _assets.LoadBackground("effect_" + name);
        if (EffectImage is null)
        {
            return;
        }

        // the overlay is transient, clear it after a short beat like the original
        _effectTimer?.Stop();
        _effectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _effectTimer.Tick += (_, _) =>
        {
            _effectTimer?.Stop();
            EffectImage = null;
        };
        _effectTimer.Start();
    }

    private void StartCountdown(NetworkMessage message)
    {
        // a staff started countdown replaces the clock with a ticking timer
        if (!int.TryParse(message.GetArgument(0), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds) || seconds <= 0)
        {
            return;
        }

        _countdownRemaining = seconds;
        ClockText = FormatClock(_countdownRemaining);

        _countdownTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick -= OnCountdownTick;
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        _countdownRemaining--;
        if (_countdownRemaining <= 0)
        {
            _countdownRemaining = 0;
            _countdownTimer?.Stop();
            Add(Events, "* Timer finished");
        }
        ClockText = FormatClock(_countdownRemaining);
    }

    private static string FormatClock(int totalSeconds)
    {
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private void ReceiveItem(NetworkMessage message)
    {
        // a give item carries the item name, or the literal "credits" with an
        // amount for a credit grant
        var what = message.GetArgument(1);
        if (string.IsNullOrWhiteSpace(what))
        {
            return;
        }

        if (string.Equals(what, "credits", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(message.GetArgument(2), System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                Credits += amount;
                Add(Events, $"* You received {amount} credits");
            }
            return;
        }

        Inventory.Add(new InventoryItem { Name = what });
        Add(Events, $"* You received {what}");
        // the legacy sfxbuy sound played when an item arrived
        _audio.PlaySfx("RE2-ItemGet.wav");
    }

    private void ApplyStatChange(NetworkMessage message)
    {
        // an animator stat edit carries the stat name and a value that is either
        // an absolute number or a signed delta like +10 or -5
        var stat = message.GetArgument(1).ToUpperInvariant();
        var raw = message.GetArgument(2).Trim();
        if (raw.Length == 0)
        {
            return;
        }

        // font colors carry a hex string, not a number, so handle them before parsing
        switch (stat)
        {
            case "HPCOLOR":
                Stats.HealthColor = raw;
                return;
            case "MPCOLOR":
                Stats.ManaColor = raw;
                return;
        }

        var isDelta = raw[0] is '+' or '-';
        if (!int.TryParse(raw, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return;
        }

        switch (stat)
        {
            case "HP":
                Stats.Health = Clamp(isDelta ? Stats.Health + value : value, Stats.MaxHealth);
                break;
            case "MAXHP":
                Stats.MaxHealth = Math.Max(0, value);
                Stats.Health = Clamp(Stats.Health, Stats.MaxHealth);
                break;
            case "HPRIGHT":
                Stats.HealthRight = Clamp(isDelta ? Stats.HealthRight + value : value, Stats.MaxHealthRight);
                break;
            case "MAXHPRIGHT":
                Stats.MaxHealthRight = Math.Max(0, value);
                Stats.HealthRight = Clamp(Stats.HealthRight, Stats.MaxHealthRight);
                break;
            case "MP":
                Stats.Mana = Clamp(isDelta ? Stats.Mana + value : value, Stats.MaxMana);
                break;
            case "MAXMP":
                Stats.MaxMana = Math.Max(0, value);
                Stats.Mana = Clamp(Stats.Mana, Stats.MaxMana);
                break;
            case "MPRIGHT":
                Stats.ManaRight = Clamp(isDelta ? Stats.ManaRight + value : value, Stats.MaxManaRight);
                break;
            case "MAXMPRIGHT":
                Stats.MaxManaRight = Math.Max(0, value);
                Stats.ManaRight = Clamp(Stats.ManaRight, Stats.MaxManaRight);
                break;
        }
    }

    private static int Clamp(int value, int max) => Math.Max(0, max > 0 ? Math.Min(value, max) : value);

    private void HandleMusic(NetworkMessage message)
    {
        // a music message names the track and who started it, the legacy MC relay
        var track = message.GetArgument(0);
        var by = message.GetArgument(1);
        Add(Events, string.IsNullOrEmpty(by) ? $"♪ {track}" : $"♪ {by} played {track}");
        _audio.PlayMusic(track);
    }

    private void PopulateAreas(NetworkMessage message)
    {
        Areas.Clear();
        foreach (var name in message.Arguments)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Areas.Add(new Area { Name = name });
            }
        }
    }

    private void PopulateMusic(NetworkMessage message)
    {
        Music.Clear();
        foreach (var name in message.Arguments)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Music.Add(new MusicTrack { Name = name, HasFile = _audio.HasMusicFile(name) });
            }
        }
    }

    private void PopulateUsers(NetworkMessage message)
    {
        // the user list is the players present in the current area
        Users.Clear();
        foreach (var name in message.Arguments)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Users.Add(name);
            }
        }
    }

    private void OnStateChanged(object? sender, ConnectionState state) =>
        Dispatcher.UIThread.Post(() =>
        {
            ConnectionStatus = state.ToString();
            IsConnected = state == ConnectionState.Connected;
        });

    private static void Add(ObservableCollection<string> feed, string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        feed.Add(line);
        while (feed.Count > 1000)
        {
            feed.RemoveAt(0);
        }
    }
}
