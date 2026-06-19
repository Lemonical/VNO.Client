using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VNO.Client.Services;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers that the audio service degrades safely when the native engine is
/// missing, the client must still run without bass present
/// </summary>
public sealed class BassAudioServiceTests
{
    private static BassAudioService Build() =>
        new(Options.Create(new ClientSettings()), NullLogger<BassAudioService>.Instance);

    [Fact]
    public void Constructs_without_throwing_even_when_engine_is_unavailable()
    {
        using var audio = Build();
        // IsAvailable reflects whether the native library loaded, either is valid
        _ = audio.IsAvailable;
    }

    [Fact]
    public void All_operations_are_safe_no_ops_when_unavailable()
    {
        using var audio = Build();

        // none of these may throw regardless of whether the engine started
        var ex = Record.Exception(() =>
        {
            audio.SetMusicVolume(0.5);
            audio.SetSfxVolume(0.5);
            audio.SetMusicVolume(-5);
            audio.SetMusicVolume(50);
            audio.PlayMusic("https://example.com/song.mp3");
            audio.PlayMusic(string.Empty);
            audio.PlaySfx("nope.wav");
            audio.PlaySfx(string.Empty);
            audio.PlayBlip();
            audio.StopMusic();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Has_music_file_reports_urls_available_and_missing_files_absent()
    {
        using var audio = Build();

        // urls stream on demand so they always count as available, even with no
        // engine, and this check does not depend on the native library
        Assert.True(audio.HasMusicFile("http://example.com/song.mp3"));
        Assert.True(audio.HasMusicFile("https://example.com/song.mp3"));
        // a track with no matching local file is missing, and blanks are missing
        Assert.False(audio.HasMusicFile("definitely-not-here.mp3"));
        Assert.False(audio.HasMusicFile(string.Empty));
    }
}
