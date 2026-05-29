namespace ConsoleEngine.Core;

/// <summary>
/// Abstraction for audio playback. Keeps audio concerns out of game logic
/// and allows testing without a real audio backend.
/// </summary>
/// <remarks>
/// The default implementation is <c>NullAudioPlayer</c> (no-op).
/// A real implementation (<c>NAudioPlayer</c>) can be injected at startup without
/// changing any game code.
/// </remarks>
public interface IAudioPlayer
{
    /// <summary>
    /// Starts playing the audio file at <paramref name="filePath"/>.
    /// If audio is already playing, behaviour depends on the implementation
    /// (e.g. queue or interrupt).
    /// </summary>
    void Play(string filePath);

    /// <summary>Stops playback immediately. No-op if nothing is playing.</summary>
    void StopPlayback();

    /// <summary>
    /// Sets the master volume to <paramref name="volume"/> (0 = silent, 100 = full).
    /// Values outside [0, 100] are clamped.
    /// </summary>
    void SetVolume(int volume);

    /// <summary><see langword="true"/> if audio is currently playing.</summary>
    bool IsPlaying { get; }
}
