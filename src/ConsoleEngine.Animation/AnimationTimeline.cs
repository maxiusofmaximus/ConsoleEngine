namespace ConsoleEngine.Animation;

/// <summary>
/// Ordered sequence of <see cref="Keyframe"/>s that <see cref="AnimationPlayer"/> executes.
/// Loaded from <c>.anim.json</c> via <see cref="AnimationTimelineLoader"/> or built in code.
/// </summary>
/// <example>
/// <code>
/// var timeline = new AnimationTimeline
/// {
///     Name   = "hit_flash",
///     Loop   = false,
///     Frames =
///     [
///         new Keyframe { DurationMs = 80,  AsciiArt = ["╔══╗", "║X ║", "╚══╝"], Color = ConsoleColor.Red },
///         new Keyframe { DurationMs = 80,  AsciiArt = ["╔══╗", "║  ║", "╚══╝"], Color = ConsoleColor.White },
///         new Keyframe { DurationMs = 80,  AsciiArt = ["╔══╗", "║X ║", "╚══╝"], Color = ConsoleColor.Red },
///     ],
/// };
/// await AnimationPlayer.PlayAsync(timeline);
/// </code>
/// </example>
public sealed class AnimationTimeline
{
    /// <summary>Unique name used to register with <see cref="TerminalAnimationEngine"/>.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>When <see langword="true"/>, the timeline repeats until cancelled.</summary>
    public bool Loop { get; init; }

    /// <summary>Ordered frames. Must not be empty for playback to do anything.</summary>
    public IReadOnlyList<Keyframe> Frames { get; init; } = [];

    /// <summary>Sum of all <see cref="Keyframe.DurationMs"/> values.</summary>
    public int TotalDurationMs => Frames.Sum(f => f.DurationMs);
}
