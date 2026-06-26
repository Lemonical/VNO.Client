using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VNO.Client.Services;
using VNO.Client.ViewModels;
using VNO.Core.Models;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers how character select chooses between the server roster and the local one
/// </summary>
public sealed class CharacterSelectRosterTests
{
    private sealed class FakeNavigator : IClientNavigator
    {
        public void ShowLogin() { }
        public void ShowServerList() { }
        public void ShowCharacterSelect() { }
        public void ShowGameStage() { }
    }

    private sealed class FakeRoster : ICharacterRosterService
    {
        private readonly List<RosterCharacter> _list;
        public FakeRoster(params string[] names)
        {
            _list = new List<RosterCharacter>();
            foreach (var n in names)
            {
                _list.Add(new RosterCharacter { Name = n });
            }
        }
        public IReadOnlyList<RosterCharacter> Load() => _list;
    }

    private sealed class FakeServer : IServerConnection
    {
        public List<NetworkMessage> Sent { get; } = new();
        public ConnectionState State => ConnectionState.Connected;
        public event EventHandler<NetworkMessage>? MessageReceived;
        public event EventHandler<ConnectionState>? StateChanged;
        public Task ConnectAsync(string displayName, string? host = null, int? port = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
        public Task SendInCharacterAsync(string characterName, string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendChatAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Raise() { MessageReceived?.Invoke(this, null!); StateChanged?.Invoke(this, ConnectionState.Connected); }
    }

    // a theme is not touched by the roster logic, a minimal stand in is enough
    private sealed class FakeTheme : IThemeService
    {
        public string DesignName => "test";
        public Avalonia.Media.Imaging.Bitmap? GetImage(string relativePath) => null;
        public Avalonia.Media.Color GetColor(string key) => Avalonia.Media.Colors.White;
        public Avalonia.Media.IBrush GetBrush(string key) => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
        public Avalonia.Media.FontFamily GetFontFamily(string key) => Avalonia.Media.FontFamily.Default;
        public double GetFontSize(string key) => 12;
        public int GetDesignInteger(string section, string key, int fallback) => fallback;
        public string ReadSetting(string section, string key, string fallback) => fallback;
        public int ReadSettingInteger(string section, string key, int fallback) => fallback;
        public void WriteSetting(string section, string key, string value) { }
        public IReadOnlyList<string> ReadDataLines(string fileName) => Array.Empty<string>();
        public void WriteDataLines(string fileName, IReadOnlyList<string> lines) { }
    }

    [Fact]
    public void Uses_local_roster_when_the_server_sent_none()
    {
        var vm = new CharacterSelectScreenViewModel(
            new FakeNavigator(), new FakeTheme(), new FakeRoster("Alpha", "Beta"), new ClientSession(), new FakeServer());

        Assert.Equal(2, CountSlots(vm));
    }

    [Fact]
    public void Prefers_the_server_roster_when_present()
    {
        var session = new ClientSession { ServerRoster = new[] { "OnlyServerChar" } };

        var vm = new CharacterSelectScreenViewModel(
            new FakeNavigator(), new FakeTheme(), new FakeRoster("Alpha", "Beta"), session, new FakeServer());

        var slots = NonNullSlots(vm);
        Assert.Single(slots);
        Assert.Equal("OnlyServerChar", slots[0]!.Name);
    }

    [Fact]
    public void Taken_characters_from_the_session_mark_slots()
    {
        var session = new ClientSession();
        var vm = new CharacterSelectScreenViewModel(
            new FakeNavigator(), new FakeTheme(), new FakeRoster("Alpha", "Beta"), session, new FakeServer());

        session.TakenCharacters = new[] { "Beta" };

        var slots = NonNullSlots(vm);
        Assert.False(slots.Find(s => s!.Name == "Alpha")!.IsTaken);
        Assert.True(slots.Find(s => s!.Name == "Beta")!.IsTaken);
    }

    private static int CountSlots(CharacterSelectScreenViewModel vm) => NonNullSlots(vm).Count;

    private static List<CharacterSlotViewModel?> NonNullSlots(CharacterSelectScreenViewModel vm)
    {
        var result = new List<CharacterSlotViewModel?>();
        foreach (var slot in vm.PageSlots)
        {
            if (slot is not null)
            {
                result.Add(slot);
            }
        }
        return result;
    }
}
