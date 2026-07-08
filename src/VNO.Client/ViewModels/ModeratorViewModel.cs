using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VNO.Client.Services;
using VNO.Core.Protocol;

namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the moderator interface window
/// </summary>
/// <remarks>
/// Ports Form1, the moderator interface. The staff member selects a player, types
/// an optional reason or address, and presses an action. Each action sends the
/// matching protocol message and reports the outcome in the status line
/// </remarks>
public sealed partial class ModeratorViewModel : ViewModelBase
{
    private readonly IServerConnection _server;

    // shown names whose chat the moderator has chosen to hide, client side only, keyed
    // by shown name because that is all an IC line carries to a receiver
    private readonly HashSet<string> _ignored = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private string _targetUserId = string.Empty;

    [ObservableProperty]
    private string _targetAddress = string.Empty;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _authState = "AUTH";

    [ObservableProperty]
    private string _moderatorPassword = string.Empty;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string? _selectedUser;

    /// <summary>
    /// Creates the view model with the server link
    /// </summary>
    public ModeratorViewModel(IServerConnection server)
    {
        _server = server;
        // keep the player list in step with the server's user list traffic
        _server.MessageReceived += OnServerMessage;
    }

    private void OnServerMessage(object? sender, NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.UserList:
                Dispatcher.UIThread.Post(() =>
                {
                    Users.Clear();
                    foreach (var name in message.Arguments)
                    {
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            Users.Add(name);
                        }
                    }
                });
                break;
            case MessageType.AreaList:
                Dispatcher.UIThread.Post(() =>
                {
                    Rooms.Clear();
                    foreach (var name in message.Arguments)
                    {
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            Rooms.Add(name);
                        }
                    }
                });
                break;
            case MessageType.ModeratorGranted:
                Dispatcher.UIThread.Post(() =>
                {
                    IsAuthenticated = true;
                    AuthState = "STAFF";
                    Status = "Moderator access granted";
                });
                break;
            case MessageType.ModeratorDenied:
                Dispatcher.UIThread.Post(() =>
                {
                    IsAuthenticated = false;
                    AuthState = "AUTH";
                    Status = "Wrong Password.";
                });
                break;
            case MessageType.StaffLookupResult:
                Dispatcher.UIThread.Post(() =>
                {
                    var text = message.GetArgument(0);
                    Status = text;
                    LogHistory(text);
                });
                break;
            case MessageType.InCharacter:
                // an IC line carries the shown name then the text, hide it when the
                // moderator has ignored that name
                Dispatcher.UIThread.Post(() =>
                {
                    var name = message.GetArgument(0);
                    if (_ignored.Contains(name))
                    {
                        return;
                    }
                    AppendChat($"{name}: {message.GetArgument(1)}");
                });
                break;
            case MessageType.OutOfCharacter:
                // OOC is relayed without an author, so it cannot be name filtered, show it
                Dispatcher.UIThread.Post(() => AppendChat($"(OOC) {message.GetArgument(0)}"));
                break;
        }
    }

    [RelayCommand]
    private async Task AuthenticateAsync()
    {
        // submit the staff password, the server replies granted or denied
        await _server.SendAsync(new NetworkMessage(
            MessageType.ModeratorAuth,
            LegacyHash.ToWireCredential(ModeratorPassword)))
            .ConfigureAwait(false);
        ModeratorPassword = string.Empty;
    }

    partial void OnSelectedUserChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            TargetUserId = value;
        }
    }

    /// <summary>
    /// The combined player and room list shown on the right, ListBox1
    /// </summary>
    public ObservableCollection<string> Users { get; } = new();

    /// <summary>
    /// The room list shown top left, ListBox2
    /// </summary>
    public ObservableCollection<string> Rooms { get; } = new();

    /// <summary>
    /// The history list shown bottom left, ListBox3
    /// </summary>
    public ObservableCollection<string> History { get; } = new();

    /// <summary>
    /// Live IC and OOC chat for the moderator's area, the feed that ID Ignore hides from
    /// </summary>
    public ObservableCollection<string> ChatFeed { get; } = new();

    /// <summary>
    /// Whether the moderator is currently hiding chat from the given shown name
    /// </summary>
    public bool IsIgnoring(string name) => _ignored.Contains(name);

    [RelayCommand]
    private async Task CharLookupAsync()
    {
        // char lookup takes the character name typed into the reason field, since
        // Form1's Char Lookup asked for a name rather than a selected id
        if (string.IsNullOrWhiteSpace(Reason))
        {
            Status = "Type a character name in the reason box first";
            return;
        }
        await _server.SendAsync(new NetworkMessage(MessageType.StaffLookup, "char", Reason)).ConfigureAwait(false);
    }

    [RelayCommand]
    private Task KickAsync() => SendForUser(MessageType.Kick, "Kick");

    [RelayCommand]
    private Task MuteAsync() => SendForUser(MessageType.Mute, "Mute");

    [RelayCommand]
    private Task UnmuteAsync() => SendForUser(MessageType.Unmute, "Unmute");

    [RelayCommand]
    private Task DjOnAsync() => SendForUser(MessageType.DjOn, "DJ on");

    [RelayCommand]
    private Task DjOffAsync() => SendForUser(MessageType.DjOff, "DJ off");

    [RelayCommand]
    private Task RoomIpAsync() =>
        _server.SendAsync(new NetworkMessage(MessageType.StaffLookup, "roomip", string.Empty));

    [RelayCommand]
    private Task MassMuteAsync() => SendRoomCommand(MessageType.MassMute, "Mass mute");

    [RelayCommand]
    private Task MassUnmuteAsync() => SendRoomCommand(MessageType.MassUnmute, "Mass unmute");

    [RelayCommand]
    private Task IpAsync() => SendLookup("ip");

    [RelayCommand]
    private Task UserLookupAsync() => SendLookup("user");

    private async Task SendLookup(string kind)
    {
        if (string.IsNullOrWhiteSpace(TargetUserId))
        {
            Status = "Select a player first";
            return;
        }
        await _server.SendAsync(new NetworkMessage(MessageType.StaffLookup, kind, TargetUserId)).ConfigureAwait(false);
    }

    [RelayCommand]
    private Task BanUserAsync() => SendForUser(MessageType.Ban, "Ban user");

    [RelayCommand]
    private Task UnbanUserAsync() => SendForUser(MessageType.Unban, "Unban user");

    [RelayCommand]
    private Task BanIpAsync() => SendForAddress(MessageType.BanIp, "Ban IP");

    [RelayCommand]
    private Task UnbanIpAsync() => SendForAddress(MessageType.UnbanIp, "Unban IP");

    [RelayCommand]
    private Task IdDisconnectAsync() => SendForUser(MessageType.Kick, "ID disconnect");

    [RelayCommand]
    private Task IdMuteAsync() => SendForUser(MessageType.Mute, "ID mute");

    [RelayCommand]
    private void IdIgnore()
    {
        // client side per moderator ignore, toggles hiding the selected name's IC chat
        // from the feed, keyed by shown name since that is what a received IC line carries
        if (string.IsNullOrWhiteSpace(TargetUserId))
        {
            Status = "Select a player first";
            return;
        }

        if (_ignored.Add(TargetUserId))
        {
            // drop any lines already shown from this name so the toggle takes effect at once
            var prefix = $"{TargetUserId}: ";
            for (var i = ChatFeed.Count - 1; i >= 0; i--)
            {
                if (ChatFeed[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    ChatFeed.RemoveAt(i);
                }
            }
            Status = $"Ignoring {TargetUserId}, their chat is hidden";
        }
        else
        {
            _ignored.Remove(TargetUserId);
            Status = $"No longer ignoring {TargetUserId}";
        }
    }

    [RelayCommand]
    private Task IdIsolateAsync() => SendForUser(MessageType.Isolate, "ID isolate");

    [RelayCommand]
    private Task LockRoomAsync() => SendRoomCommand(MessageType.LockRoom, "Room lock");

    [RelayCommand]
    private Task UnlockRoomAsync() => SendRoomCommand(MessageType.UnlockRoom, "Room unlock");

    private async Task SendRoomCommand(MessageType type, string label)
    {
        // these act on the moderator's own area, so they carry no target
        await _server.SendAsync(NetworkMessage.Create(type)).ConfigureAwait(false);
        Status = $"{label} sent";
        LogHistory(label);
    }

    private async Task SendForUser(MessageType type, string label)
    {
        if (string.IsNullOrWhiteSpace(TargetUserId))
        {
            Status = "Enter a player id first";
            return;
        }

        await _server.SendAsync(new NetworkMessage(type, TargetUserId, Reason)).ConfigureAwait(false);
        Status = $"{label} sent for player {TargetUserId}";
        LogHistory($"{label} player {TargetUserId}");
    }

    private async Task SendForAddress(MessageType type, string label)
    {
        if (string.IsNullOrWhiteSpace(TargetAddress))
        {
            Status = "Enter an address first";
            return;
        }

        await _server.SendAsync(new NetworkMessage(type, TargetAddress, Reason)).ConfigureAwait(false);
        Status = $"{label} sent for {TargetAddress}";
        LogHistory($"{label} {TargetAddress}");
    }

    private void LogHistory(string action)
    {
        // the ListBox3 bans and unbans log, newest first, capped so it never grows
        var stamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        History.Insert(0, $"[{stamp}] {action}");
        while (History.Count > 200)
        {
            History.RemoveAt(History.Count - 1);
        }
    }

    private void AppendChat(string line)
    {
        // newest last so the feed reads top to bottom like the game window, capped so it
        // never grows without bound during a long moderation session
        ChatFeed.Add(line);
        while (ChatFeed.Count > 500)
        {
            ChatFeed.RemoveAt(0);
        }
    }
}
