using System.Text.Json.Serialization;
using ConsoleEngine.Scenes;

namespace ConsoleEngine.Editor.Models;

/// <summary>
/// Serialisable representation of a ConsoleEngine scene.
/// Mirrors <c>ConsoleEngine.Scenes.SceneDefinition</c> so files are
/// drop-in compatible with the runtime.
/// </summary>
public sealed class SceneDocument
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("lines")]
    public string[] Lines { get; set; } = [];

    [JsonPropertyName("asciiArt")]
    public string[] AsciiArt { get; set; } = [];

    [JsonPropertyName("artColor")]
    public string ArtColor { get; set; } = "DarkGreen";

    [JsonPropertyName("textColor")]
    public string TextColor { get; set; } = "Gray";

    [JsonPropertyName("spritePath")]
    public string? SpritePath { get; set; }

    [JsonPropertyName("promptContinue")]
    public bool PromptContinue { get; set; } = true;

    // ── Conversion ───────────────────────────────────────────────────────
    /// <summary>Converts this document to a <see cref="SceneDefinition"/> for runtime rendering.</summary>
    public SceneDefinition ToDefinition() => new()
    {
        Title          = Title,
        Lines          = Lines,
        AsciiArt       = AsciiArt,
        ArtColor       = ParseColor(ArtColor,  ConsoleColor.DarkGray),
        TextColor      = ParseColor(TextColor, ConsoleColor.Gray),
        SpritePath     = SpritePath,
        PromptContinue = PromptContinue,
    };

    // ── Factory ──────────────────────────────────────────────────────────
    public static SceneDocument Empty() => new()
    {
        Title = "New Scene",
        Lines = ["Write your narration here.", ""],
        AsciiArt = ["  /\\  /\\  ", " /  \\/  \\ ", "══════════", "▓▓▓▓▓▓▓▓▓▓"],
        ArtColor = "DarkGreen",
        TextColor = "Gray",
        PromptContinue = true,
    };

    private static ConsoleColor ParseColor(string? name, ConsoleColor fallback) =>
        name is not null && Enum.TryParse(name, ignoreCase: true, out ConsoleColor c) ? c : fallback;
}
