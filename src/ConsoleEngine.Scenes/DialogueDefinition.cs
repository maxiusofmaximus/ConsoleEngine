namespace ConsoleEngine.Scenes;

/// <summary>
/// Data model for a two-character dialogue scene.
/// Both characters are anchored to the bottom of the screen;
/// dialogue lines flow from the top.
/// </summary>
public sealed class DialogueDefinition
{
    // ── Left character ────────────────────────────────────────────────────────

    /// <summary>ASCII art for the left character (protagonist side).</summary>
    public string[]     LeftArt      { get; init; } = Array.Empty<string>();

    /// <summary>Label shown under the left character.</summary>
    public string       LeftLabel    { get; init; } = string.Empty;

    /// <summary>Colour for the left character's art and label.</summary>
    public ConsoleColor LeftColor    { get; init; } = ConsoleColor.DarkCyan;

    /// <summary>
    /// Optional PNG sprite for the left character.
    /// When provided, renders as ANSI half-blocks instead of ASCII art.
    /// </summary>
    public string?      LeftSpritePath { get; init; }

    // ── Right character ───────────────────────────────────────────────────────

    /// <summary>ASCII art for the right character (NPC / companion side).</summary>
    public string[]     RightArt     { get; init; } = Array.Empty<string>();

    /// <summary>Label shown under the right character.</summary>
    public string       RightLabel   { get; init; } = string.Empty;

    /// <summary>Colour for the right character's art and label.</summary>
    public ConsoleColor RightColor   { get; init; } = ConsoleColor.DarkYellow;

    // ── Dialogue lines ────────────────────────────────────────────────────────

    /// <summary>
    /// Dialogue lines. Indent with spaces to distinguish speech from narration.
    /// Include the speaker prefix in the string for translator control.
    /// </summary>
    public string[]     Lines        { get; init; } = Array.Empty<string>();

    /// <summary>Colour for all dialogue lines.</summary>
    public ConsoleColor TextColor    { get; init; } = ConsoleColor.Gray;

    /// <summary>When <c>true</c> (default), waits for Enter before returning.</summary>
    public bool         PromptContinue { get; init; } = true;
}
