using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using VNO.Client.Services;
using VNO.Client.ViewModels;
using VNO.Core.Models;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers saving the recorded scene replay buffer to a data file
/// </summary>
public sealed class GameStageReplayTests
{
    private sealed class FakeServer : IServerConnection
    {
        public ConnectionState State => ConnectionState.Disconnected;
#pragma warning disable CS0067 // required by the interface, not exercised here
        public event EventHandler<NetworkMessage>? MessageReceived;
        public event EventHandler<ConnectionState>? StateChanged;
#pragma warning restore CS0067
        public Task ConnectAsync(string displayName, string? host = null, int? port = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendInCharacterAsync(string characterName, string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendChatAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeWindows : IWindowService
    {
        public void ShowModerator() { }
        public void ShowAnimator() { }
        public Task<bool> ShowPasswordDialogAsync() => Task.FromResult(false);
        public Task ShowMessageAsync(string message) => Task.CompletedTask;
        public Task<string?> InputBoxAsync(string title, string prompt) => Task.FromResult<string?>(null);
    }

    private sealed class FakeAssets : ICharacterAssetService
    {
        public LoadedCharacter? LoadCharacter(string folderName) => null;
        public Bitmap? LoadBackground(string name) => null;
        public Bitmap? LoadDefaultBackground() => null;
    }

    private sealed class FakeImages : IImageFetcher
    {
        public Task<Bitmap?> FetchAsync(string url, CancellationToken cancellationToken = default) =>
            Task.FromResult<Bitmap?>(null);
    }

    private sealed class FakeAudio : IAudioService
    {
        public bool IsAvailable => false;
        public void PlayMusic(string source) { }
        public void StopMusic() { }
        public bool HasMusicFile(string source) => false;
        public void PlaySfx(string fileName) { }
        public void PlayBlip() { }
        public void SetMusicVolume(double volume) { }
        public void SetSfxVolume(double volume) { }
        public void SetBlipVolume(double volume) { }
    }

    private sealed class CapturingTheme : IThemeService
    {
        public string? WrittenFile { get; private set; }
        public IReadOnlyList<string>? WrittenLines { get; private set; }

        public string DesignName => "test";
        public Bitmap? GetImage(string relativePath) => null;
        public Avalonia.Media.Color GetColor(string key) => Avalonia.Media.Colors.White;
        public Avalonia.Media.IBrush GetBrush(string key) => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
        public Avalonia.Media.FontFamily GetFontFamily(string key) => Avalonia.Media.FontFamily.Default;
        public double GetFontSize(string key) => 12;
        public string ReadSetting(string section, string key, string fallback) => fallback;
        public int ReadSettingInteger(string section, string key, int fallback) => fallback;
        public void WriteSetting(string section, string key, string value) { }
        public IReadOnlyList<string> ReadDataLines(string fileName) => Array.Empty<string>();
        public void WriteDataLines(string fileName, IReadOnlyList<string> lines)
        {
            WrittenFile = fileName;
            WrittenLines = lines;
        }
    }

    private static GameStageViewModel Build(CapturingTheme theme) =>
        new(new FakeServer(), new FakeWindows(), theme, new ClientSession(), new FakeAssets(), new FakeImages(), new FakeAudio());

    [Fact]
    public void Save_replay_writes_the_recorded_lines_to_a_dated_file()
    {
        var theme = new CapturingTheme();
        var vm = Build(theme);
        vm.Replay.Add("IC|Phoenix|Objection!");
        vm.Replay.Add("OOC|hello");

        vm.SaveReplayCommand.Execute(null);

        Assert.NotNull(theme.WrittenFile);
        Assert.StartsWith("replay-", theme.WrittenFile);
        Assert.EndsWith(".txt", theme.WrittenFile);
        Assert.Equal(new[] { "IC|Phoenix|Objection!", "OOC|hello" }, theme.WrittenLines);
    }

    [Fact]
    public void Save_replay_with_empty_buffer_writes_nothing()
    {
        var theme = new CapturingTheme();
        var vm = Build(theme);

        vm.SaveReplayCommand.Execute(null);

        Assert.Null(theme.WrittenFile);
    }
}
