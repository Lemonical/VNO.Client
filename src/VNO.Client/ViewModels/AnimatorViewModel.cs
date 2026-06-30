using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VNO.Client.Services;
using VNO.Core;
using VNO.Core.Protocol;

namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the animator interface window
/// </summary>
/// <remarks>
/// Ports Form7, the animator interface, with its four tabs: self HP and MP edit,
/// inventory grants, server wide controls, and the targeted HP and MP edit grid.
/// Each action sends the matching protocol message
/// </remarks>
public sealed partial class AnimatorViewModel : ViewModelBase
{
    private const string HealthStat = "HP";
    private const string ManaStat = "MP";
    private const string MaxHealthStat = "MAXHP";
    private const string MaxManaStat = "MAXMP";
    private const string HealthColorStat = "HPCOLOR";
    private const string ManaColorStat = "MPCOLOR";
    private const string SelfTarget = "0";
    private const int EditSlotCount = 8;

    private readonly IServerConnection _server;
    private readonly IThemeService _theme;
    private CancellationTokenSource? _replayCts;

    [ObservableProperty]
    private string _ownHealth = "Your HP here";

    [ObservableProperty]
    private string _ownMaxHealth = "Edit_ownmaxhp";

    [ObservableProperty]
    private string _ownMana = "Your MP here";

    [ObservableProperty]
    private string _ownMaxMana = "Edit_ownmaxmana";

    [ObservableProperty]
    private string _itemUser = "User";

    [ObservableProperty]
    private string _itemAmount = "Amount";

    [ObservableProperty]
    private string _creditUser = "User";

    [ObservableProperty]
    private string _creditAmount = "Amount";

    [ObservableProperty]
    private string _effectName = "Effect name";

    [ObservableProperty]
    private string _healthColorValue = "#FFFFFFFF";

    [ObservableProperty]
    private string _manaColorValue = "#FFFFFFFF";

    [ObservableProperty]
    private string _timerSeconds = "Amount of Seconds";

    [ObservableProperty]
    private string _broadcast = "Type here";

    [ObservableProperty]
    private string _imageUrl = "URL to display image";

    [ObservableProperty]
    private string _musicUrl = "Force stream music";

    [ObservableProperty]
    private string _replayFile = "Replay File name";

    [ObservableProperty]
    private string _replayDelay = "Milliseconds Delay";

    [ObservableProperty]
    private string _status = "Status ";

    [ObservableProperty]
    private string _authState = "AUTH";

    /// <summary>
    /// Creates the view model and its eight HP and MP edit slots
    /// </summary>
    public AnimatorViewModel(IServerConnection server, IThemeService theme)
    {
        _server = server;
        _theme = theme;
        for (var i = 0; i < EditSlotCount; i++)
        {
            EditSlots.Add(new StatEditSlotViewModel(server));
        }

        // the server answers an item or credit check with a lookup result line
        _server.MessageReceived += OnServerMessage;
    }

    private void OnServerMessage(object? sender, NetworkMessage message)
    {
        if (message.Type == MessageType.StaffLookupResult)
        {
            Status = message.GetArgument(0);
        }
    }

    /// <summary>
    /// The give item roster shown on the inventory tab
    /// </summary>
    public ObservableCollection<string> Items { get; } = new();

    /// <summary>
    /// The eight targeted HP and MP edit slots
    /// </summary>
    public ObservableCollection<StatEditSlotViewModel> EditSlots { get; } = new();

    [RelayCommand]
    private Task SetOwnHealthAsync() => SendStat(SelfTarget, HealthStat, OwnHealth);

    [RelayCommand]
    private Task SetOwnMaxHealthAsync() => SendStat(SelfTarget, MaxHealthStat, OwnMaxHealth);

    [RelayCommand]
    private Task SetOwnManaAsync() => SendStat(SelfTarget, ManaStat, OwnMana);

    [RelayCommand]
    private Task SetOwnMaxManaAsync() => SendStat(SelfTarget, MaxManaStat, OwnMaxMana);

    [RelayCommand]
    private async Task SetEverythingAsync()
    {
        await SetOwnHealthAsync().ConfigureAwait(false);
        await SetOwnMaxHealthAsync().ConfigureAwait(false);
        await SetOwnManaAsync().ConfigureAwait(false);
        await SetOwnMaxManaAsync().ConfigureAwait(false);
        Status = "Set everything";
    }

    [RelayCommand]
    private Task AdjustHealthAsync(string delta) => SendStat(SelfTarget, HealthStat, delta);

    [RelayCommand]
    private Task AdjustManaAsync(string delta) => SendStat(SelfTarget, ManaStat, delta);

    [RelayCommand]
    private async Task SetHealthColorAsync()
    {
        await SendStat(SelfTarget, HealthColorStat, HealthColorValue).ConfigureAwait(false);
        Status = $"HP font color set to {HealthColorValue}";
    }

    [RelayCommand]
    private async Task SetManaColorAsync()
    {
        await SendStat(SelfTarget, ManaColorStat, ManaColorValue).ConfigureAwait(false);
        Status = $"MP font color set to {ManaColorValue}";
    }

    /// <summary>
    /// Argb hex colors a staff member can pick for the HP and MP readouts
    /// </summary>
    public IReadOnlyList<string> ColorChoices { get; } =
        ["#FFFFFFFF", "#FFFF0000", "#FF00FF00", "#FF00A2FF", "#FFFFFF00", "#FFFF8000", "#FF000000"];

    [RelayCommand]
    private async Task GiveItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemUser))
        {
            Status = "Enter a user first";
            return;
        }

        await _server.SendAsync(new NetworkMessage(MessageType.GiveItem, ItemUser, ItemAmount)).ConfigureAwait(false);
        Status = $"Gave item to {ItemUser}";
    }

    [RelayCommand]
    private async Task CheckItemAsync()
    {
        await _server.SendAsync(new NetworkMessage(MessageType.CheckInventory, ItemUser, "items")).ConfigureAwait(false);
        Status = $"Checking items for {ItemUser}";
    }

    [RelayCommand]
    private async Task GiveCreditsAsync()
    {
        if (string.IsNullOrWhiteSpace(CreditUser))
        {
            Status = "Enter a user first";
            return;
        }

        await _server.SendAsync(new NetworkMessage(MessageType.GiveItem, CreditUser, "credits", CreditAmount)).ConfigureAwait(false);
        Status = $"Gave {CreditAmount} credits to {CreditUser}";
    }

    [RelayCommand]
    private async Task CheckCreditsAsync()
    {
        await _server.SendAsync(new NetworkMessage(MessageType.CheckInventory, CreditUser, "credits")).ConfigureAwait(false);
        Status = $"Checking credits for {CreditUser}";
    }

    [RelayCommand]
    private async Task PlayEffectAsync()
    {
        if (string.IsNullOrWhiteSpace(EffectName))
        {
            Status = "Enter an effect name first";
            return;
        }

        // clients load data\background\effect_<name> and overlay it on the scene
        await _server.SendAsync(new NetworkMessage(MessageType.SceneEffect, EffectName)).ConfigureAwait(false);
        Status = $"Played effect '{EffectName}'";
    }

    [RelayCommand]
    private async Task StartTimerAsync()
    {
        // start a room wide countdown, the server broadcasts it to every client
        if (!TryGetTimerSeconds(out var seconds) || seconds <= 0)
        {
            Status = "Enter a number of seconds first";
            return;
        }
        await _server.SendAsync(new NetworkMessage(
            MessageType.Timer, seconds.ToString(CultureInfo.InvariantCulture))).ConfigureAwait(false);
        Status = $"Timer started for {seconds} seconds";
    }

    [RelayCommand]
    private async Task SendBroadcastAsync()
    {
        await _server.SendAsync(new NetworkMessage(MessageType.Notice, Broadcast)).ConfigureAwait(false);
        Status = "Server message sent";
    }

    [RelayCommand]
    private async Task StreamImageAsync()
    {
        if (string.IsNullOrWhiteSpace(ImageUrl))
        {
            Status = "Enter an image url first";
            return;
        }
        await _server.SendAsync(new NetworkMessage(MessageType.StreamImage, ImageUrl)).ConfigureAwait(false);
        Status = $"Streaming image {ImageUrl}";
    }

    [RelayCommand]
    private async Task StreamMusicAsync()
    {
        if (string.IsNullOrWhiteSpace(MusicUrl))
        {
            Status = "Enter a music url first";
            return;
        }
        await _server.SendAsync(new NetworkMessage(MessageType.StreamMusic, MusicUrl)).ConfigureAwait(false);
        Status = $"Streaming music {MusicUrl}";
    }

    [RelayCommand]
    private async Task StartReplayAsync()
    {
        var lines = _theme.ReadDataLines(ReplayFile);
        if (lines.Count == 0)
        {
            Status = $"Replay {ReplayFile} is empty or missing";
            return;
        }

        if (!int.TryParse(ReplayDelay, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delayMs) ||
            delayMs < 0)
        {
            delayMs = 1000;
        }

        // cancel any running replay before starting a new one
        _replayCts?.Cancel();
        _replayCts = new CancellationTokenSource();
        var token = _replayCts.Token;
        Status = $"Replay {ReplayFile} started";

        try
        {
            foreach (var line in lines)
            {
                token.ThrowIfCancellationRequested();
                var message = ReplayLine.Parse(line);
                if (message is not null)
                {
                    await _server.SendAsync(message, token).ConfigureAwait(false);
                    await Task.Delay(delayMs, token).ConfigureAwait(false);
                }
            }
            Status = "Replay finished";
        }
        catch (OperationCanceledException)
        {
            Status = "Replay stopped";
        }
    }

    [RelayCommand]
    private void StopReplay()
    {
        _replayCts?.Cancel();
        Status = "Replay stopped";
    }

    [RelayCommand]
    private async Task AllowHideAsync()
    {
        await SendPolicy("allowhide", true).ConfigureAwait(false);
        Status = "Players may hide";
    }

    [RelayCommand]
    private async Task DisallowHideAsync()
    {
        await SendPolicy("allowhide", false).ConfigureAwait(false);
        Status = "Players may not hide";
    }

    [RelayCommand]
    private async Task HideRoomCountAsync()
    {
        await SendPolicy("hideroomcount", true).ConfigureAwait(false);
        Status = "Room count hidden";
    }

    [RelayCommand]
    private async Task SelfHpEditOnAsync()
    {
        await SendPolicy("selfhpedit", true).ConfigureAwait(false);
        Status = "Self HP edit enabled";
    }

    [RelayCommand]
    private async Task SelfHpEditOffAsync()
    {
        await SendPolicy("selfhpedit", false).ConfigureAwait(false);
        Status = "Self HP edit disabled";
    }

    private Task SendPolicy(string key, bool on) =>
        _server.SendAsync(new NetworkMessage(MessageType.RoomPolicy, key, on ? "on" : "off"));

    private async Task SendStat(string target, string stat, string value)
    {
        var normalized = value.Trim();
        await _server.SendAsync(new NetworkMessage(MessageType.StatChange, target, stat, normalized)).ConfigureAwait(false);
        Status = $"Set {stat} to {normalized}";
    }

    /// <summary>
    /// Reads the timer field as seconds when it is a number
    /// </summary>
    public bool TryGetTimerSeconds(out int seconds) =>
        int.TryParse(TimerSeconds, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds);
}
