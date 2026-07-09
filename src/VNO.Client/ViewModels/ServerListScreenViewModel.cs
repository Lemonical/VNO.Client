using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using VNO.Client.Services;
using VNO.Core.Models;

namespace VNO.Client.ViewModels;

/// <summary>
/// The list tab the server list screen is showing
/// </summary>
public enum ServerListTab
{
    /// <summary>
    /// The curated server list
    /// </summary>
    Servers,

    /// <summary>
    /// The player's saved favorites
    /// </summary>
    Favorites,

    /// <summary>
    /// The open community list
    /// </summary>
    Community
}

/// <summary>
/// View model for the server list screen
/// </summary>
/// <remarks>
/// Ports groupbox_srvrlst from Form15. It shows the servers, favorites, and
/// community tabs, a description and player count for the selection, and a connect
/// button that moves on to character select. The servers tab fills from the AS
/// directory like the legacy SDA packets did, with the configured game server as
/// the offline fallback entry
/// </remarks>
public sealed partial class ServerListScreenViewModel : ViewModelBase
{
    // the legacy form had a fixed column of thirteen per server status images
    private const int ServerIconSlots = 13;
    private const string FavoritesFile = "favorites.txt";

    private readonly IClientNavigator _navigator;
    private readonly IAuthServerLink _authLink;
    private readonly IWindowService _windows;
    private readonly IServerConnection _server;
    private readonly IClientSession _session;
    private readonly Bitmap? _iconOn;
    private readonly Bitmap? _iconOff;
    private bool _hasDirectoryEntries;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsServersTab))]
    [NotifyPropertyChangedFor(nameof(IsFavoritesTab))]
    [NotifyPropertyChangedFor(nameof(IsCommunityTab))]
    private ServerListTab _activeTab = ServerListTab.Servers;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private ServerEntryViewModel? _selectedServer;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _playerCount = "Offline";

    [ObservableProperty]
    private double _loadProgress;

    [ObservableProperty]
    private string _loadStatus = string.Empty;

    /// <summary>
    /// Creates the screen and seeds it from configuration
    /// </summary>
    public ServerListScreenViewModel(
        IClientNavigator navigator,
        IOptions<ClientSettings> settings,
        IThemeService theme,
        IAuthServerLink authLink,
        IWindowService windows,
        IServerConnection server,
        IClientSession session)
    {
        _navigator = navigator;
        _server = server;
        _session = session;
        // the server sends its roster after Master-backed authentication, before character
        // select is shown, so capture it here into the shared session
        _server.MessageReceived += OnServerMessage;
        _authLink = authLink;
        _windows = windows;
        Theme = theme;

        _iconOn = theme.GetImage("buttons/on.png");
        _iconOff = theme.GetImage("buttons/off.png");

        var value = settings.Value;
        Servers.Add(new ServerEntryViewModel
        {
            Name = "Default Game Server",
            Host = value.GameServerHost,
            Port = value.GameServerPort,
            Description = "The configured game server",
            PlayerCount = "Unknown"
        });

        LoadFavorites();
        RefreshSlotIcons();

        _authLink.ServerDiscovered += OnServerDiscovered;
    }

    private void LoadFavorites()
    {
        // the legacy client kept favorites in data/favorites.txt
        foreach (var line in Theme.ReadDataLines(FavoritesFile))
        {
            var parts = line.Split('|');
            if (parts.Length < 3 || !int.TryParse(parts[2], out var port))
            {
                continue;
            }
            Favorites.Add(new ServerEntryViewModel
            {
                Name = parts[0],
                Host = parts[1],
                Port = port,
                Description = parts.Length > 3 ? parts[3] : string.Empty,
                PlayerCount = "Unknown"
            });
        }
    }

    private void SaveFavorites() =>
        Theme.WriteDataLines(
            FavoritesFile,
            Favorites
                .Select(f => string.Join('|', f.Name, f.Host,
                    f.Port.ToString(CultureInfo.InvariantCulture), f.Description))
                .ToList());

    private void RefreshSlotIcons()
    {
        SlotIcons.Clear();
        for (var i = 0; i < ServerIconSlots; i++)
        {
            SlotIcons.Add(i < Servers.Count ? _iconOn : _iconOff);
        }
    }

    private void OnServerMessage(object? sender, VNO.Core.Protocol.NetworkMessage message)
    {
        if (message.Type == VNO.Core.Protocol.MessageType.CharacterList)
        {
            var names = message.Arguments.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            Dispatcher.UIThread.Post(() => _session.ServerRoster = names);
        }
        else if (message.Type == VNO.Core.Protocol.MessageType.CharacterTaken)
        {
            var taken = message.Arguments.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            Dispatcher.UIThread.Post(() => _session.TakenCharacters = taken);
        }
    }

    private void OnServerDiscovered(object? sender, ServerListing listing)
    {
        // entries arrive off the UI thread, one per legacy SDA packet. The first
        // directory entry replaces the configuration seed
        Dispatcher.UIThread.Post(() =>
        {
            if (!_hasDirectoryEntries)
            {
                _hasDirectoryEntries = true;
                Servers.Clear();
            }

            var existing = Servers.FirstOrDefault(s => s.Host == listing.Host && s.Port == listing.Port);
            if (existing is not null)
            {
                Servers.Remove(existing);
            }

            Servers.Add(new ServerEntryViewModel
            {
                Name = listing.Name,
                Host = listing.Host,
                Port = listing.Port,
                Description = listing.Description,
                PlayerCount = "Unknown"
            });
            LoadStatus = string.Empty;
            RefreshSlotIcons();
        });
    }

    /// <summary>
    /// The player theme, views resolve their skin images through it
    /// </summary>
    public IThemeService Theme { get; }

    /// <summary>
    /// The curated server list
    /// </summary>
    public ObservableCollection<ServerEntryViewModel> Servers { get; } = new();

    /// <summary>
    /// The player's saved favorites
    /// </summary>
    public ObservableCollection<ServerEntryViewModel> Favorites { get; } = new();

    /// <summary>
    /// The open community server list
    /// </summary>
    public ObservableCollection<ServerEntryViewModel> Community { get; } = new();

    /// <summary>
    /// The thirteen per server status images beside the list, on or off
    /// </summary>
    public ObservableCollection<Bitmap?> SlotIcons { get; } = new();

    /// <summary>True when the curated servers tab is active</summary>
    public bool IsServersTab => ActiveTab == ServerListTab.Servers;

    /// <summary>True when the favorites tab is active</summary>
    public bool IsFavoritesTab => ActiveTab == ServerListTab.Favorites;

    /// <summary>True when the community tab is active</summary>
    public bool IsCommunityTab => ActiveTab == ServerListTab.Community;

    partial void OnSelectedServerChanged(ServerEntryViewModel? value)
    {
        Description = value?.Description ?? string.Empty;
        PlayerCount = value?.PlayerCount ?? "Offline";
    }

    [RelayCommand]
    private void ShowServers() => ActiveTab = ServerListTab.Servers;

    [RelayCommand]
    private void ShowFavorites() => ActiveTab = ServerListTab.Favorites;

    [RelayCommand]
    private void ShowCommunity() => ActiveTab = ServerListTab.Community;

    [RelayCommand]
    private void Refresh()
    {
        // the legacy refresh asked the AS to resend the SDA list
        LoadProgress = 0;
        LoadStatus = "Loading...";
        _ = _authLink.RequestServersAsync();
    }

    [RelayCommand]
    private void AddFavorite()
    {
        // the legacy heart button stored the selected server in favorites.txt
        var server = SelectedServer;
        if (server is null || Favorites.Any(f => f.Host == server.Host && f.Port == server.Port))
        {
            return;
        }
        Favorites.Add(new ServerEntryViewModel
        {
            Name = server.Name,
            Host = server.Host,
            Port = server.Port,
            Description = server.Description,
            PlayerCount = server.PlayerCount
        });
        SaveFavorites();
    }

    [RelayCommand]
    private Task ShowCredits() =>
        // the legacy clickme easter egg image popped the staff credits, the
        // decompiled string is truncated so the tail is a best effort completion
        _windows.ShowMessageAsync("Visual Novel Online.\n\nVNO Staff:\nNoevain: Programing.\nCronnicossy: Production.");

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        var server = SelectedServer;
        if (server is null)
        {
            return;
        }

        // Ask Master for a one-use handoff immediately before opening the game socket.
        // The game server accepts no player messages until Master redeems it.
        LoadStatus = "Connecting...";
        try
        {
            var handoffToken = await _authLink.RequestGameTokenAsync();
            if (handoffToken is null)
            {
                throw new UnauthorizedAccessException("Master could not authorize this game connection");
            }

            await _server.ConnectAsync(handoffToken, server.Host, server.Port);
            LoadStatus = string.Empty;
            _navigator.ShowCharacterSelect();
        }
        catch (Exception ex)
        {
            LoadStatus = string.Empty;
            await _windows.ShowMessageAsync($"Could not connect to {server.Name}: {ex.Message}");
        }
    }

    private bool CanConnect() => SelectedServer is not null;
}
