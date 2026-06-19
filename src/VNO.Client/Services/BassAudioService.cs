using System;
using System.IO;
using ManagedBass;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VNO.Client.Services;

/// <summary>
/// Audio over the BASS engine, the same engine the legacy client used
/// </summary>
/// <remarks>
/// Wraps ManagedBass. The engine is initialized once, and if the native BASS
/// library is not present the service degrades to a safe no op so the client
/// still runs. Music streams from an http url or a file under the sounds folder,
/// sound effects and blips are one shot streams that free themselves when done.
/// Volumes are held and applied to each channel as it plays, mirroring the
/// legacy volume trackbars
/// </remarks>
public sealed class BassAudioService : IAudioService, IDisposable
{
    private readonly ILogger<BassAudioService> _logger;
    private readonly string _soundsDirectory;
    private readonly object _gate = new();

    private bool _initialized;
    private int _musicHandle;
    private double _musicVolume = 0.5;
    private double _sfxVolume = 0.5;
    private double _blipVolume = 0.5;

    /// <summary>
    /// Creates the service and tries to start the BASS engine
    /// </summary>
    public BassAudioService(IOptions<ClientSettings> options, ILogger<BassAudioService> logger)
    {
        _logger = logger;
        var dataDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, options.Value.DataDirectory));
        _soundsDirectory = Path.Combine(dataDirectory, "sounds");

        try
        {
            // device -1 is the default output, a missing native library throws
            _initialized = Bass.Init();
            if (!_initialized)
            {
                _logger.LogWarning("BASS did not initialize ({Error}), audio is disabled", Bass.LastError);
            }
        }
        catch (Exception ex)
        {
            // no native bass library, run without audio like a client missing bass.dll
            _initialized = false;
            _logger.LogWarning(ex, "BASS native library not available, audio is disabled");
        }
    }

    /// <inheritdoc />
    public bool IsAvailable => _initialized;

    /// <inheritdoc />
    public void PlayMusic(string source)
    {
        if (!_initialized || string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        lock (_gate)
        {
            try
            {
                if (_musicHandle != 0)
                {
                    Bass.StreamFree(_musicHandle);
                    _musicHandle = 0;
                }

                var isUrl = source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                _musicHandle = isUrl
                    ? Bass.CreateStream(source, 0, BassFlags.Default, null)
                    : Bass.CreateStream(ResolveMusicPath(source));

                if (_musicHandle == 0)
                {
                    _logger.LogWarning("Could not open music {Source} ({Error})", source, Bass.LastError);
                    return;
                }

                Bass.ChannelSetAttribute(_musicHandle, ChannelAttribute.Volume, _musicVolume);
                Bass.ChannelPlay(_musicHandle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to play music {Source}", source);
            }
        }
    }

    /// <inheritdoc />
    public void StopMusic()
    {
        if (!_initialized)
        {
            return;
        }
        lock (_gate)
        {
            if (_musicHandle != 0)
            {
                Bass.StreamFree(_musicHandle);
                _musicHandle = 0;
            }
        }
    }

    /// <inheritdoc />
    public bool HasMusicFile(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }
        // urls are always considered available since they stream on demand, this
        // check does not need the engine so it works even when audio degraded
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return File.Exists(ResolveMusicPath(source));
    }

    /// <inheritdoc />
    public void PlaySfx(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }
        PlayOneShot(Path.Combine(_soundsDirectory, "sfx", fileName), _sfxVolume);
    }

    /// <inheritdoc />
    public void PlayBlip()
    {
        // the legacy blip lives under the sounds folder, named blip.wav here
        PlayOneShot(Path.Combine(_soundsDirectory, "blip.wav"), _blipVolume);
    }

    private void PlayOneShot(string path, double volume)
    {
        if (!_initialized || !File.Exists(path))
        {
            return;
        }

        try
        {
            // auto free releases the stream once it finishes so one shots do not leak
            var handle = Bass.CreateStream(path, 0, 0, BassFlags.AutoFree);
            if (handle == 0)
            {
                return;
            }
            Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, volume);
            Bass.ChannelPlay(handle);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to play sound {Path}", path);
        }
    }

    /// <inheritdoc />
    public void SetMusicVolume(double volume)
    {
        _musicVolume = Clamp01(volume);
        if (_initialized && _musicHandle != 0)
        {
            Bass.ChannelSetAttribute(_musicHandle, ChannelAttribute.Volume, _musicVolume);
        }
    }

    /// <inheritdoc />
    public void SetSfxVolume(double volume) => _sfxVolume = Clamp01(volume);

    /// <inheritdoc />
    public void SetBlipVolume(double volume) => _blipVolume = Clamp01(volume);

    private string ResolveMusicPath(string source)
    {
        // absolute paths pass through, names resolve under the sounds music folder
        if (Path.IsPathRooted(source))
        {
            return source;
        }
        return Path.Combine(_soundsDirectory, "music", source);
    }

    private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

    /// <summary>
    /// Frees the engine on shutdown
    /// </summary>
    public void Dispose()
    {
        if (_initialized)
        {
            try
            {
                Bass.Free();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to free the audio engine");
            }
            _initialized = false;
        }
    }
}
