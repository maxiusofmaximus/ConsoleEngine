using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleEngine.Core;

namespace ConsoleEngine.Scenes;

/// <summary>
/// Loads <see cref="SceneDefinition"/> objects from <c>.scene.json</c> files on disk,
/// decoupling scene data from compiled code.
/// </summary>
/// <remarks>
/// <b>File format</b> (<c>.scene.json</c>):
/// <code>
/// {
///   "title":          "Chapter 1",
///   "lines":          ["Line one.", "Line two."],
///   "asciiArt":       ["  /\\  ", "══════"],
///   "artColor":       "DarkGreen",
///   "textColor":      "Gray",
///   "spritePath":     null,
///   "spriteWidth":    32,
///   "spriteRows":     16,
///   "promptContinue": true,
///   "continuePrompt": "[ PRESS ENTER ]"
/// }
/// </code>
/// <c>artColor</c> and <c>textColor</c> accept any <see cref="ConsoleColor"/> name
/// (case-insensitive). Unknown values fall back to the field's default.
/// </remarks>
public static class SceneLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
    };

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Loads and deserialises a <c>.scene.json</c> file into a <see cref="SceneDefinition"/>.
    /// </summary>
    /// <param name="jsonPath">Absolute or relative path to the <c>.scene.json</c> file.</param>
    /// <exception cref="FileNotFoundException">The file does not exist at <paramref name="jsonPath"/>.</exception>
    /// <exception cref="InvalidDataException">The file cannot be parsed as a valid scene.</exception>
    public static SceneDefinition Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Scene file not found: '{jsonPath}'.", jsonPath);

        string json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);

        SceneDto? dto = JsonSerializer.Deserialize<SceneDto>(json, Options)
                        ?? throw new InvalidDataException(
                               $"Scene file parsed as null: '{jsonPath}'.");

        return dto.ToDefinition();
    }

    /// <summary>
    /// Attempts to load a <c>.scene.json</c> file without throwing.
    /// </summary>
    /// <returns><see langword="true"/> on success; <see langword="false"/> if the file is
    /// missing or malformed (the error is swallowed — use <see cref="Load"/> for diagnostics).</returns>
    public static bool TryLoad(string jsonPath, out SceneDefinition? scene)
    {
        try   { scene = Load(jsonPath); return true;  }
        catch { scene = null;           return false; }
    }

    /// <summary>Current schema version written to new files.</summary>
    public const int CurrentSchemaVersion = 1;

    // ── Internal DTO ──────────────────────────────────────────────────────

    private sealed class SceneDto
    {
        [JsonPropertyName("schemaVersion")]  public int?      SchemaVersion  { get; init; }
        [JsonPropertyName("title")]          public string?   Title          { get; init; }
        [JsonPropertyName("lines")]          public string[]? Lines          { get; init; }
        [JsonPropertyName("asciiArt")]       public string[]? AsciiArt       { get; init; }
        [JsonPropertyName("artColor")]       public string?   ArtColor       { get; init; }
        [JsonPropertyName("textColor")]      public string?   TextColor      { get; init; }
        [JsonPropertyName("spritePath")]     public string?   SpritePath     { get; init; }
        [JsonPropertyName("spriteWidth")]    public int?      SpriteWidth    { get; init; }
        [JsonPropertyName("spriteRows")]     public int?      SpriteRows     { get; init; }
        [JsonPropertyName("promptContinue")] public bool?     PromptContinue { get; init; }
        [JsonPropertyName("continuePrompt")] public string?   ContinuePrompt { get; init; }

        public SceneDefinition ToDefinition()
        {
            int version = SchemaVersion ?? 0; // 0 = pre-schemaVersion legacy files
            if (version > CurrentSchemaVersion)
                throw new InvalidDataException(
                    $"Scene file uses schema version {version}, but this engine only supports up to {CurrentSchemaVersion}. " +
                    "Update ConsoleEngine to load this file.");

            return new SceneDefinition
            {
                Title          = Title,
                Lines          = Lines          ?? [],
                AsciiArt       = AsciiArt       ?? [],
                ArtColor       = ConsoleColorParser.Parse(ArtColor,   ConsoleColor.DarkGray),
                TextColor      = ConsoleColorParser.Parse(TextColor,  ConsoleColor.Gray),
                SpritePath     = SpritePath,
                SpriteWidth    = SpriteWidth    ?? 32,
                SpriteRows     = SpriteRows     ?? 16,
                PromptContinue = PromptContinue ?? true,
                ContinuePrompt = ContinuePrompt,
            };
        }

    }
}
