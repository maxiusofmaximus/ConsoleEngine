namespace ConsoleEngine.Scenes;

/// <summary>
/// Data model for a single narrative scene: title, ASCII backdrop, narration lines,
/// and an optional pixel-art character sprite anchored to the bottom of the screen.
///
/// <b>Layout contract</b>
/// <list type="bullet">
///   <item>ASCII art is bottom-anchored. Its last two rows must be a ground line (═══) and a floor line (▓▓▓).</item>
///   <item>The optional PNG sprite stands to the right of the ASCII art, feet on the ground line.</item>
///   <item>Narration text flows from the top, over the art.</item>
/// </list>
/// </summary>
public sealed record SceneDefinition
{
    /// <summary>Scene heading shown above the narration text. <c>null</c> = no heading.</summary>
    public string?        Title          { get; init; }

    /// <summary>ASCII art backdrop. Last two rows should be ═══ (ground) and ▓▓▓ (floor).</summary>
    public string[]?      AsciiArt       { get; init; }

    /// <summary>Colour for the ASCII art backdrop.</summary>
    public ConsoleColor   ArtColor       { get; init; } = ConsoleColor.DarkGray;

    /// <summary>Narration lines displayed above the backdrop.</summary>
    public string[]       Lines          { get; init; } = Array.Empty<string>();

    /// <summary>Colour for title and narration lines.</summary>
    public ConsoleColor   TextColor      { get; init; } = ConsoleColor.Gray;

    /// <summary>
    /// Optional path to a PNG sprite (e.g. a character portrait).
    /// Rendered as ANSI half-blocks, right-aligned, feet on the ground row.
    /// </summary>
    public string?        SpritePath     { get; init; }

    /// <summary>
    /// Width of the PNG sprite in terminal characters (pixels / 1).
    /// Default 32 matches a 32×32 PNG.
    /// </summary>
    public int            SpriteWidth    { get; init; } = 32;

    /// <summary>
    /// Height of the PNG sprite in terminal rows (pixels / 2).
    /// Default 16 matches a 32×32 PNG.
    /// </summary>
    public int            SpriteRows     { get; init; } = 16;

    /// <summary>
    /// When <c>true</c> (default), the scene waits for the player to press Enter before returning.
    /// Set to <c>false</c> for cut-scenes that advance automatically.
    /// </summary>
    public bool           PromptContinue { get; init; } = true;

    /// <summary>Custom continue-prompt text. <c>null</c> = use <c>CK.Prompt.Continue</c> from locale.</summary>
    public string?        ContinuePrompt { get; init; }
}
