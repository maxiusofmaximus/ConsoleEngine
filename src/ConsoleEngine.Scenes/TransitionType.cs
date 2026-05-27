namespace ConsoleEngine.Scenes;

/// <summary>Available transition effects between scenes.</summary>
public enum TransitionType
{
    /// <summary>No transition — instant cut.</summary>
    Cut,

    /// <summary>Screen fades to black by filling rows with ░ → ▒ → ▓ → █.</summary>
    FadeToBlack,

    /// <summary>Black curtain sweeps from top to bottom.</summary>
    WipeDown,

    /// <summary>Black curtain sweeps from bottom to top.</summary>
    WipeUp,

    /// <summary>Darkness closes in from the edges toward the centre.</summary>
    CloseIn,
}
