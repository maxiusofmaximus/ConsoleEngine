namespace ConsoleEngine.World;

/// <summary>
/// Reason the <see cref="ExplorationPlayer"/> loop terminated.
/// Returned to the caller so it can decide what to do next (show a menu, quit, etc.).
/// </summary>
public enum ExplorationResult
{
    /// <summary>Player typed <c>menu</c> or <c>back</c> — show the main menu.</summary>
    ReturnToMenu,

    /// <summary>Player typed <c>quit</c> — exit the application.</summary>
    Quit,
}
