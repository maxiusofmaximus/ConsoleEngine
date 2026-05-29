using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleEngine.Core;

namespace ConsoleEngine.Scenes;

/// <summary>
/// Plays an ordered list of scenes, each with an optional condition and property overrides.
/// Implements <see cref="IScenePlayer"/> so it is interchangeable with a single <see cref="ScenePlayer"/> call.
/// </summary>
/// <remarks>
/// Inspired by Godot's node-tree model: a sequence is a tree where each leaf is a
/// <see cref="SceneNode"/> with a path, an optional guard condition, and optional
/// <see cref="SceneDefinition"/> overrides applied via C# <c>with</c> expressions.
/// </remarks>
public sealed class SceneSequencer : IScenePlayer
{
    private readonly IReadOnlyList<SceneNode> _nodes;

    /// <param name="nodes">Ordered list of scene nodes to play.</param>
    public SceneSequencer(IEnumerable<SceneNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        _nodes = nodes.ToList();
    }

    /// <summary>
    /// Loads and plays each node in order. Nodes whose <see cref="SceneNode.Condition"/> returns
    /// <see langword="false"/> are skipped. Property overrides are applied with <c>with</c> syntax.
    /// </summary>
    public void Play()
    {
        foreach (SceneNode node in _nodes)
        {
            if (node.Condition is not null && !node.Condition()) continue;

            SceneDefinition scene = SceneLoader.Load(node.ScenePath);

            if (node.Overrides is not null)
                scene = ApplyOverrides(scene, node.Overrides);

            ScenePlayer.Play(scene);
        }
    }

    // ── Factory ───────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a <see cref="SceneSequencer"/> from a <c>.sequence.json</c> file.
    /// </summary>
    /// <param name="jsonPath">Path to the <c>.sequence.json</c> file.</param>
    public static SceneSequencer Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Sequence file not found: '{jsonPath}'.", jsonPath);

        SequenceDto? dto = JsonSerializer.Deserialize<SequenceDto>(
                               File.ReadAllText(jsonPath, System.Text.Encoding.UTF8), SequenceOptions)
                           ?? throw new InvalidDataException($"Sequence file parsed as null: '{jsonPath}'.");

        int version = dto.SchemaVersion ?? 0;
        if (version > CurrentSchemaVersion)
            throw new InvalidDataException(
                $"Sequence file '{jsonPath}' uses schema version {version}, " +
                $"but this engine only supports up to {CurrentSchemaVersion}.");

        var nodes = (dto.Nodes ?? []).Select(n =>
            new SceneNode(
                ScenePath: n.ScenePath ?? throw new InvalidDataException(
                    "A sequence node is missing the required 'scenePath' field."),
                Condition: null, // conditions are code-defined; JSON only stores paths + overrides
                Overrides: null  // JSON overrides not yet supported in v1 — use code for overrides
            ));

        return new SceneSequencer(nodes);
    }

    /// <summary>
    /// Attempts to load a <c>.sequence.json</c> file without throwing.
    /// </summary>
    public static bool TryLoad(string jsonPath, out SceneSequencer? sequencer)
    {
        try   { sequencer = Load(jsonPath); return true;  }
        catch { sequencer = null;           return false; }
    }

    /// <summary>Current schema version written to new files.</summary>
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions SequenceOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
    };

    // ── Helpers ───────────────────────────────────────────────────────────

    private static SceneDefinition ApplyOverrides(SceneDefinition scene, SceneOverrides ov) => scene with
    {
        Title          = ov.Title          ?? scene.Title,
        Lines          = ov.Lines          ?? scene.Lines,
        AsciiArt       = ov.AsciiArt       ?? scene.AsciiArt,
        ArtColor       = ov.ArtColor       ?? scene.ArtColor,
        TextColor      = ov.TextColor      ?? scene.TextColor,
        SpritePath     = ov.SpritePath     ?? scene.SpritePath,
        PromptContinue = ov.PromptContinue ?? scene.PromptContinue,
        ContinuePrompt = ov.ContinuePrompt ?? scene.ContinuePrompt,
    };

    // ── Internal DTOs ─────────────────────────────────────────────────────

    private sealed class SequenceDto
    {
        [JsonPropertyName("schemaVersion")] public int?          SchemaVersion { get; init; }
        [JsonPropertyName("nodes")]         public NodeDto[]?    Nodes         { get; init; }
    }

    private sealed class NodeDto
    {
        [JsonPropertyName("scenePath")] public string? ScenePath { get; init; }
    }
}

/// <summary>
/// A single node in a <see cref="SceneSequencer"/>: a path to a scene file,
/// an optional runtime condition, and optional property overrides.
/// </summary>
/// <param name="ScenePath">Path to the <c>.scene.json</c> file for this node.</param>
/// <param name="Condition">
/// When non-<see langword="null"/>, the node is only played if this delegate returns <see langword="true"/>.
/// Evaluated at playback time.
/// </param>
/// <param name="Overrides">
/// When non-<see langword="null"/>, these values replace the corresponding fields from the loaded scene.
/// Uses C# <c>with</c> expressions — set only the fields you want to change.
/// </param>
public sealed record SceneNode(
    string ScenePath,
    Func<bool>? Condition = null,
    SceneOverrides? Overrides = null
);

/// <summary>
/// Partial overrides for a <see cref="SceneDefinition"/> within a <see cref="SceneNode"/>.
/// Only non-<see langword="null"/> fields replace the original values.
/// </summary>
public sealed class SceneOverrides
{
    public string?        Title          { get; init; }
    public string[]?      Lines          { get; init; }
    public string[]?      AsciiArt       { get; init; }
    public ConsoleColor?  ArtColor       { get; init; }
    public ConsoleColor?  TextColor      { get; init; }
    public string?        SpritePath     { get; init; }
    public bool?          PromptContinue { get; init; }
    public string?        ContinuePrompt { get; init; }
}
