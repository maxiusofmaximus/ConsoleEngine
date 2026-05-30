using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleEngine.Core;

namespace ConsoleEngine.Scenes;

/// <summary>
/// Loads <see cref="DialogueDefinition"/> objects from <c>.dialogue.json</c> files on disk.
/// </summary>
/// <remarks>
/// <b>File format</b> (<c>.dialogue.json</c>):
/// <code>
/// {
///   "schemaVersion": 1,
///   "leftLabel":     "Hero",
///   "leftColor":     "Cyan",
///   "leftArt":       ["  O  ", " /|\\ ", " / \\ "],
///   "leftSpritePath": null,
///   "rightLabel":    "Merchant",
///   "rightColor":    "Yellow",
///   "rightArt":      [" _O_ ", "  |  ", " / \\ "],
///   "textColor":     "Gray",
///   "lines":         ["Hero: What do you sell?", "Merchant: Everything."],
///   "promptContinue": true
/// }
/// </code>
/// </remarks>
public static class DialogueLoader
{
    /// <summary>Current schema version written to new files.</summary>
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
    };

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Loads and deserialises a <c>.dialogue.json</c> file into a <see cref="DialogueDefinition"/>.
    /// </summary>
    /// <param name="jsonPath">Absolute or relative path to the <c>.dialogue.json</c> file.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file cannot be parsed as a valid dialogue.</exception>
    public static DialogueDefinition Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Dialogue file not found: '{jsonPath}'.", jsonPath);

        string json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);

        DialogueDto? dto = JsonSerializer.Deserialize<DialogueDto>(json, Options)
                           ?? throw new InvalidDataException(
                                  $"Dialogue file parsed as null: '{jsonPath}'.");

        return dto.ToDefinition();
    }

    /// <summary>
    /// Attempts to load a <c>.dialogue.json</c> file without throwing.
    /// </summary>
    /// <returns><see langword="true"/> on success; <see langword="false"/> if the file is missing or malformed.</returns>
    public static bool TryLoad(string jsonPath, out DialogueDefinition? dialogue)
    {
        try   { dialogue = Load(jsonPath); return true;  }
        catch { dialogue = null;           return false; }
    }

    // ── Internal DTO ──────────────────────────────────────────────────────

    private sealed class DialogueDto
    {
        [JsonPropertyName("schemaVersion")]  public int?      SchemaVersion  { get; init; }
        [JsonPropertyName("leftLabel")]      public string?   LeftLabel      { get; init; }
        [JsonPropertyName("leftColor")]      public string?   LeftColor      { get; init; }
        [JsonPropertyName("leftArt")]        public string[]? LeftArt        { get; init; }
        [JsonPropertyName("leftSpritePath")] public string?   LeftSpritePath { get; init; }
        [JsonPropertyName("rightLabel")]     public string?   RightLabel     { get; init; }
        [JsonPropertyName("rightColor")]     public string?   RightColor     { get; init; }
        [JsonPropertyName("rightArt")]       public string[]? RightArt       { get; init; }
        [JsonPropertyName("textColor")]      public string?   TextColor      { get; init; }
        [JsonPropertyName("lines")]          public string[]? Lines          { get; init; }
        [JsonPropertyName("promptContinue")] public bool?     PromptContinue { get; init; }

        public DialogueDefinition ToDefinition()
        {
            int version = SchemaVersion ?? 0;
            if (version > CurrentSchemaVersion)
                throw new InvalidDataException(
                    $"Dialogue file uses schema version {version}, but this engine only supports up to {CurrentSchemaVersion}. " +
                    "Update ConsoleEngine to load this file.");

            return new DialogueDefinition
            {
                LeftLabel      = LeftLabel      ?? string.Empty,
                LeftColor      = ConsoleColorParser.Parse(LeftColor,  ConsoleColor.DarkCyan),
                LeftArt        = LeftArt        ?? [],
                LeftSpritePath = LeftSpritePath,
                RightLabel     = RightLabel     ?? string.Empty,
                RightColor     = ConsoleColorParser.Parse(RightColor, ConsoleColor.DarkYellow),
                RightArt       = RightArt       ?? [],
                TextColor      = ConsoleColorParser.Parse(TextColor,  ConsoleColor.Gray),
                Lines          = Lines          ?? [],
                PromptContinue = PromptContinue ?? true,
            };
        }

    }
}
