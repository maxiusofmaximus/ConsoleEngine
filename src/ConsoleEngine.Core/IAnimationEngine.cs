namespace ConsoleEngine.Core;

/// <summary>
/// Abstraction for playing named animation sequences.
/// Decouples game logic from the terminal/native rendering backend.
/// </summary>
/// <remarks>
/// Register animations by name, then play them by ID.
/// Implementations: <c>TerminalAnimationEngine</c> (ConsoleEngine.Animation).
/// </remarks>
public interface IAnimationEngine
{
    /// <summary>Plays the named animation, waiting for it to finish. No-op if not registered.</summary>
    Task PlayAsync(string animationId, CancellationToken ct = default);

    /// <summary><see langword="true"/> if the named animation is currently running.</summary>
    bool IsPlaying(string animationId);

    /// <summary>Cancels a running animation immediately.</summary>
    void Cancel(string animationId);

    /// <summary>Cancels all running animations.</summary>
    void CancelAll();
}
