namespace ConsoleEngine.Core;

/// <summary>
/// No-op implementation of <see cref="IAudioPlayer"/>. All calls are silent.
/// Use as the default when no audio backend is configured — the game stays functional.
/// </summary>
public sealed class NullAudioPlayer : IAudioPlayer
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly NullAudioPlayer Instance = new();

    private NullAudioPlayer() { }

    /// <inheritdoc/>
    public void Play(string filePath) { }

    /// <inheritdoc/>
    public void StopPlayback() { }

    /// <inheritdoc/>
    public void SetVolume(int volume) { }

    /// <inheritdoc/>
    public bool IsPlaying => false;
}
