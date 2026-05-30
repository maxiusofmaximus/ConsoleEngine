using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleEngine.Core;

namespace ConsoleEngine.World;

/// <summary>
/// Loads a <see cref="WorldMap"/> from a <c>.world.json</c> file on disk,
/// decoupling world data from compiled code.
/// </summary>
/// <remarks>
/// <b>File format</b> (<c>.world.json</c>):
/// <code>
/// {
///   "locations": [
///     {
///       "id":          "town",
///       "name":        "Market Town",
///       "description": ["Merchants call out.", "Children chase a dog."],
///       "asciiArt":    ["  ╔═╗  ", "──╚═╝──"],
///       "artColor":    "DarkYellow",
///       "textColor":   "Gray",
///       "spritePath":  null,
///       "spriteWidth": 32,
///       "spriteRows":  16,
///       "exits":       { "north": "forest" }
///     }
///   ]
/// }
/// </code>
/// <c>artColor</c> and <c>textColor</c> accept any <see cref="ConsoleColor"/> name
/// (case-insensitive). Unknown values fall back to the field default.
/// </remarks>
public static class WorldLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
    };

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Loads and deserialises a <c>.world.json</c> file into a <see cref="WorldMap"/>.
    /// </summary>
    /// <param name="jsonPath">Absolute or relative path to the <c>.world.json</c> file.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file cannot be parsed, or contains no locations.</exception>
    public static WorldMap Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"World file not found: '{jsonPath}'.", jsonPath);

        string json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);

        WorldDto? dto = JsonSerializer.Deserialize<WorldDto>(json, Options)
                        ?? throw new InvalidDataException($"World file parsed as null: '{jsonPath}'.");

        int version = dto.SchemaVersion ?? 0; // 0 = pre-schemaVersion legacy files
        if (version > CurrentSchemaVersion)
            throw new InvalidDataException(
                $"World file '{jsonPath}' uses schema version {version}, but this engine only supports up to {CurrentSchemaVersion}. " +
                "Update ConsoleEngine to load this file.");

        if (dto.Locations is not { Length: > 0 })
            throw new InvalidDataException($"World file contains no locations: '{jsonPath}'.");

        return new WorldMap(dto.Locations.Select(l => l.ToDefinition()));
    }

    /// <summary>
    /// Attempts to load a <c>.world.json</c> file without throwing.
    /// </summary>
    public static bool TryLoad(string jsonPath, out WorldMap? map)
    {
        try   { map = Load(jsonPath); return true;  }
        catch { map = null;           return false; }
    }

    /// <summary>Current schema version written to new files.</summary>
    public const int CurrentSchemaVersion = 1;

    // ── Internal DTOs ─────────────────────────────────────────────────────

    private sealed class WorldDto
    {
        [JsonPropertyName("schemaVersion")]
        public int? SchemaVersion { get; init; }

        [JsonPropertyName("locations")]
        public LocationDto[]? Locations { get; init; }
    }

    private sealed class LocationDto
    {
        [JsonPropertyName("id")]          public string?              Id          { get; init; }
        [JsonPropertyName("name")]        public string?              Name        { get; init; }
        [JsonPropertyName("description")] public string[]?            Description { get; init; }
        [JsonPropertyName("asciiArt")]    public string[]?            AsciiArt    { get; init; }
        [JsonPropertyName("artColor")]    public string?              ArtColor    { get; init; }
        [JsonPropertyName("textColor")]   public string?              TextColor   { get; init; }
        [JsonPropertyName("spritePath")]  public string?              SpritePath  { get; init; }
        [JsonPropertyName("spriteWidth")] public int?                 SpriteWidth { get; init; }
        [JsonPropertyName("spriteRows")]  public int?                 SpriteRows  { get; init; }
        [JsonPropertyName("exits")]       public Dictionary<string, string>? Exits { get; init; }

        public LocationDefinition ToDefinition()
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new InvalidDataException("A location is missing its required 'id' field.");
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidDataException($"Location '{Id}' is missing its required 'name' field.");

            return new LocationDefinition
            {
                Id          = Id,
                Name        = Name,
                Description = Description  ?? [],
                AsciiArt    = AsciiArt     ?? [],
                ArtColor    = ConsoleColorParser.Parse(ArtColor,  ConsoleColor.DarkGray),
                TextColor   = ConsoleColorParser.Parse(TextColor, ConsoleColor.Gray),
                SpritePath  = SpritePath,
                SpriteWidth = SpriteWidth  ?? 32,
                SpriteRows  = SpriteRows   ?? 16,
                Exits       = new Dictionary<string, string>(
                                  Exits ?? [], StringComparer.OrdinalIgnoreCase),
            };
        }

    }
}
