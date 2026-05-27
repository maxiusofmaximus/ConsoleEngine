namespace ConsoleEngine.World;

/// <summary>
/// Data model for a single location in the world graph.
///
/// <b>Exit directions</b> are free-form strings (e.g. <c>"north"</c>, <c>"in"</c>, <c>"enter"</c>).
/// ConsoleEngine.World normalises aliases (n→north, s→south, e→east, w→west, u→up, d→down)
/// before matching, so a location only needs to list canonical names.
/// </summary>
public sealed record LocationDefinition
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Unique identifier used in <see cref="WorldMap"/> and <see cref="WorldState"/>.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable name shown in the exploration HUD and content area.</summary>
    public string Name { get; init; } = string.Empty;

    // ── Description ───────────────────────────────────────────────────────────

    /// <summary>Narration lines displayed when the player arrives at or looks around this location.</summary>
    public string[] Description { get; init; } = Array.Empty<string>();

    /// <summary>Colour for description text.</summary>
    public ConsoleColor TextColor { get; init; } = ConsoleColor.Gray;

    // ── Backdrop ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Optional ASCII art backdrop.
    /// Displayed bottom-anchored above the ground line, same convention as <c>SceneDefinition</c>.
    /// </summary>
    public string[]? AsciiArt { get; init; }

    /// <summary>Colour for the ASCII art backdrop.</summary>
    public ConsoleColor ArtColor { get; init; } = ConsoleColor.DarkGray;

    /// <summary>
    /// Optional PNG sprite rendered as ANSI half-blocks (right-aligned, feet on ground row).
    /// </summary>
    public string? SpritePath { get; init; }

    /// <summary>Width of the sprite in terminal characters. Default 32 for a 32×32 PNG.</summary>
    public int SpriteWidth { get; init; } = 32;

    /// <summary>Height of the sprite in terminal rows. Default 16 for a 32×32 PNG.</summary>
    public int SpriteRows { get; init; } = 16;

    // ── Exits ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Map of exit direction → destination location ID.
    /// Use canonical direction names: <c>north</c>, <c>south</c>, <c>east</c>, <c>west</c>,
    /// <c>up</c>, <c>down</c>, <c>in</c>, <c>out</c>.
    /// Custom named exits (e.g. <c>"through the portal"</c>) are also supported.
    /// </summary>
    public IReadOnlyDictionary<string, string> Exits { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
