namespace ConsoleEngine.Animation;

/// <summary>
/// A single frame of an <see cref="AnimationTimeline"/>. Pure data — no I/O.
/// </summary>
public sealed record Keyframe
{
    /// <summary>How long this frame is displayed, in milliseconds.</summary>
    public int DurationMs { get; init; } = 100;

    /// <summary>ASCII art lines drawn at (<see cref="Col"/>, <see cref="Row"/>). <see langword="null"/> = no art.</summary>
    public string[]? AsciiArt { get; init; }

    /// <summary>PNG sprite path rendered at (<see cref="Col"/>, <see cref="Row"/>). <see langword="null"/> = no sprite.</summary>
    public string? SpritePath { get; init; }

    /// <summary>Colour used when drawing <see cref="AsciiArt"/>.</summary>
    public ConsoleColor Color { get; init; } = ConsoleColor.White;

    /// <summary>Column (terminal X) for art and sprite origin.</summary>
    public int Col { get; init; }

    /// <summary>Row (terminal Y) for art and sprite origin.</summary>
    public int Row { get; init; }

    /// <summary>When <see langword="true"/>, <c>Console.Clear()</c> is called before drawing.</summary>
    public bool ClearBefore { get; init; }

    /// <summary>Sound effect ID to trigger when this frame begins. Reserved for the Audio module.</summary>
    public string? Sound { get; init; }
}
