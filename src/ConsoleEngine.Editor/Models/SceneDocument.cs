using System.Text.Json.Serialization;

namespace ConsoleEngine.Editor.Models;

/// <summary>
/// Serialisable representation of a ConsoleEngine scene.
/// Mirrors <c>ConsoleEngine.Scenes.SceneDefinition</c> so files are
/// drop-in compatible with the runtime.
/// </summary>
public sealed class SceneDocument
{
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
}
