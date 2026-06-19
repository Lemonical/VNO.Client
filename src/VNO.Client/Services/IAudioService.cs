namespace VNO.Client.Services;

/// <summary>
/// Plays music, sound effects, and typing blips for the stage
/// </summary>
/// <remarks>
/// The legacy client drove audio through BASS, streaming music from a url with
/// BASS_StreamCreateURL and playing short samples for sound effects and the per
/// character typing blip, with the volume trackbars mapped onto the channels.
/// This service is the seam over that engine so the view models never touch the
/// native library. When the native BASS library is missing every call is a safe
/// no op, so the client still runs without audio, the way the original needed
/// bass.dll present to make sound
/// </remarks>
public interface IAudioService
{
    /// <summary>
    /// True when the audio engine started, false when it degraded to no audio
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Streams music from an http url or a file under the sounds folder,
    /// replacing any music already playing
    /// </summary>
    void PlayMusic(string source);

    /// <summary>
    /// Stops the current music stream
    /// </summary>
    void StopMusic();

    /// <summary>
    /// True when the named track resolves to a local file the client has, or is a
    /// streamable url. Drives the music list availability coloring
    /// </summary>
    bool HasMusicFile(string source);

    /// <summary>
    /// Plays a one shot sound effect from a file under the sounds sfx folder
    /// </summary>
    void PlaySfx(string fileName);

    /// <summary>
    /// Plays the short typing blip, the legacy per character sound
    /// </summary>
    void PlayBlip();

    /// <summary>
    /// Sets the music volume from zero to one
    /// </summary>
    void SetMusicVolume(double volume);

    /// <summary>
    /// Sets the sound effect volume from zero to one
    /// </summary>
    void SetSfxVolume(double volume);

    /// <summary>
    /// Sets the typing blip volume from zero to one
    /// </summary>
    void SetBlipVolume(double volume);
}
