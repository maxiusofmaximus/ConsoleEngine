namespace ConsoleEngine.World;

/// <summary>
/// Extension point for location-specific commands in the exploration loop.
///
/// <b>Example — add a "rest" command that advances time:</b>
/// <code>
/// public sealed class RestAction : IExplorationAction
/// {
///     public string Command     => "rest";
///     public string Description => "Rest until the next time period.";
///
///     public bool IsAvailable(WorldState state, LocationDefinition location)
///         => location.Id == "tavern";
///
///     public ExplorationOutcome Execute(WorldState state, LocationDefinition location)
///     {
///         state.AdvanceTime();
///         return ExplorationOutcome.Continue(CL.Get("tavern.rested"));
///     }
/// }
/// </code>
/// Pass custom actions via <see cref="ExplorationOptions.CustomActions"/>.
/// </summary>
public interface IExplorationAction
{
    /// <summary>The exact command string the player must type (lower-case).</summary>
    string Command { get; }

    /// <summary>Short description shown in the <c>help</c> listing.</summary>
    string Description { get; }

    /// <summary>
    /// Returns <c>true</c> when this action is available given the current
    /// <paramref name="state"/> and <paramref name="location"/>.
    /// Unavailable actions are hidden from the help listing.
    /// </summary>
    bool IsAvailable(WorldState state, LocationDefinition location);

    /// <summary>
    /// Executes the action.
    /// Returns an <see cref="ExplorationOutcome"/> describing what should happen next.
    /// </summary>
    ExplorationOutcome Execute(WorldState state, LocationDefinition location);
}

/// <summary>
/// Outcome returned by <see cref="IExplorationAction.Execute"/>.
/// Use the static factory methods to construct instances.
/// </summary>
public readonly struct ExplorationOutcome
{
    internal ExplorationResult? ExitResult { get; }
    internal string? Message              { get; }
    internal bool    ShouldRerender       { get; }

    private ExplorationOutcome(ExplorationResult? exit, string? message, bool rerender)
    {
        ExitResult    = exit;
        Message       = message;
        ShouldRerender = rerender;
    }

    /// <summary>
    /// Remain in the exploration loop. Re-renders the location and shows <paramref name="message"/>.
    /// </summary>
    public static ExplorationOutcome Continue(string? message = null)
        => new(null, message, rerender: true);

    /// <summary>Exit the exploration loop and return to the main menu.</summary>
    public static ExplorationOutcome ReturnToMenu(string? message = null)
        => new(ExplorationResult.ReturnToMenu, message, rerender: false);

    /// <summary>Exit the exploration loop and quit the application.</summary>
    public static ExplorationOutcome Quit()
        => new(ExplorationResult.Quit, null, rerender: false);
}
