using System.Text;
using System.Text.Json;
using ConsoleEngine.Core;

namespace ConsoleEngine.Editor.Terminal;

/// <summary>
/// Writes <c>EDITOR_STATE.json</c> to the project root before launching the AI terminal.
/// The AI process receives this file as <c>--context</c> so it knows which scene is active.
/// Also bootstraps <c>GAME_CONTEXT.md</c> if the project does not have one yet.
/// </summary>
internal static class EditorStateWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// Writes (or overwrites) <c>EDITOR_STATE.json</c> and ensures <c>GAME_CONTEXT.md</c> exists.
    /// Safe to call frequently — both operations are idempotent.
    /// </summary>
    public static void Write(string projectPath, string? activeScenePath)
    {
        string stateFile = Path.Combine(projectPath, "EDITOR_STATE.json");

        var state = new
        {
            timestamp     = DateTime.UtcNow.ToString("O"),
            projectPath,
            activeScene   = activeScenePath is not null
                                ? Path.GetRelativePath(projectPath, activeScenePath)
                                : null,
            engineVersion = EngineVersion.Full,
        };

        File.WriteAllText(stateFile,
            JsonSerializer.Serialize(state, JsonOpts),
            Encoding.UTF8);

        EnsureGameContext(projectPath);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private static void EnsureGameContext(string projectPath)
    {
        string contextFile = Path.Combine(projectPath, "GAME_CONTEXT.md");
        if (File.Exists(contextFile)) return;

        File.WriteAllText(contextFile, """
            # GAME_CONTEXT.md — Project Context for AI Assistants

            > Keep this file updated. It is loaded by the AI terminal on every session
            > so the AI understands your project without re-explanation.

            ## Project Overview
            <!-- What is this game about? Genre, tone, target platform. -->

            ## Architecture
            - Engine: ConsoleEngine (terminal/2D hybrid, .NET 8)
            - Scene files: `*.scene.json`
            - Locale keys: defined in `CK.*` constants — always use `CL.Get(CK.*)`, never hardcode strings
            - Art: ASCII art + PixelArtRenderer (PNG → ▀ half-blocks)

            ## Current Work
            <!-- What are you building right now? Paste the relevant scene names or feature names. -->

            ## Conventions
            <!-- Any project-specific rules the AI should follow. -->
            """,
            Encoding.UTF8);
    }
}
