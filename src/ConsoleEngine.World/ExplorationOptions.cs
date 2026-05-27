using ConsoleEngine.Scenes;

namespace ConsoleEngine.World;

/// <summary>
/// Optional configuration passed to <see cref="ExplorationPlayer.Run"/>.
/// All fields have sensible defaults so you only need to set what you want to change.
/// </summary>
public sealed class ExplorationOptions
{
    // ── Visual ────────────────────────────────────────────────────────────────

    /// <summary>Colour for description text. Default <see cref="ConsoleColor.Gray"/>.</summary>
    public ConsoleColor TextColor { get; init; } = ConsoleColor.Gray;

    /// <summary>Colour for the HUD bar and separators. Default <see cref="ConsoleColor.Cyan"/>.</summary>
    public ConsoleColor HudColor  { get; init; } = ConsoleColor.Cyan;

    /// <summary>Colour for exits listing. Default <see cref="ConsoleColor.DarkCyan"/>.</summary>
    public ConsoleColor ExitColor { get; init; } = ConsoleColor.DarkCyan;

    /// <summary>Colour for feedback messages (e.g. "Nothing that way."). Default <see cref="ConsoleColor.Yellow"/>.</summary>
    public ConsoleColor MsgColor  { get; init; } = ConsoleColor.Yellow;

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Transition effect played when the player moves to a new location.
    /// Default <see cref="TransitionType.Cut"/> (instant, no animation).
    /// </summary>
    public TransitionType MoveTransition { get; init; } = TransitionType.Cut;

    /// <summary>
    /// Transition speed in milliseconds per frame.
    /// Only relevant when <see cref="MoveTransition"/> is not <see cref="TransitionType.Cut"/>.
    /// </summary>
    public int TransitionStepMs { get; init; } = 35;

    /// <summary>
    /// When <c>true</c> (default), displays the exits line each time the location is rendered.
    /// </summary>
    public bool ShowExits { get; init; } = true;

    // ── Extension points ──────────────────────────────────────────────────────

    /// <summary>
    /// Additional commands available in the exploration loop.
    /// Checked after all built-in commands have been tried.
    /// </summary>
    public IReadOnlyList<IExplorationAction>? CustomActions { get; init; }

    /// <summary>
    /// Called each time the player enters a new location, before the screen is rendered.
    /// Use this for scripted events, locked doors, or encounter triggers.
    /// </summary>
    public Action<WorldState, LocationDefinition>? OnLocationEnter { get; init; }
}
