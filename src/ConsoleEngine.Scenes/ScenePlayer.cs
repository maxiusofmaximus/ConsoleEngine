namespace ConsoleEngine.Scenes;

using ConsoleEngine.Locale;
using ConsoleEngine.Rendering;

/// <summary>
/// Renders a <see cref="SceneDefinition"/> to the terminal.
///
/// <b>Two-pass rendering</b>
/// <list type="number">
///   <item>Pass 1 — absolute: ASCII art and optional PNG sprite are placed
///         bottom-anchored at exact terminal row positions.</item>
///   <item>Pass 2 — flow: title and narration lines stream downward from row 0,
///         layered on top of the backdrop.</item>
/// </list>
/// </summary>
public static class ScenePlayer
{
    /// <summary>
    /// Renders <paramref name="scene"/> and optionally blocks until the player
    /// presses Enter (controlled by <see cref="SceneDefinition.PromptContinue"/>).
    /// </summary>
    public static void Play(SceneDefinition scene)
    {
        Console.Clear();
        AnimationEngine.HideCursor();

        int winH = Console.WindowHeight;
        int winW = Console.WindowWidth;

        // ── Pass 1a: bottom-anchored ASCII backdrop ────────────────────────────
        if (scene.AsciiArt is { Length: > 0 } art)
        {
            int artTopRow = winH - art.Length;
            for (int i = 0; i < art.Length; i++)
                AnimationEngine.DrawAt(0, artTopRow + i, art[i], scene.ArtColor);
        }

        // ── Pass 1b: right-anchored PNG sprite (feet on ground row) ───────────
        if (!string.IsNullOrEmpty(scene.SpritePath))
        {
            // Ground row is 2nd from bottom; sprite feet land there.
            int groundRow    = winH - 2;
            int spriteTopRow = groundRow - scene.SpriteRows;
            int spriteCol    = winW   - scene.SpriteWidth - 1;
            PixelArtRenderer.RenderPng(
                scene.SpritePath,
                Math.Max(0, spriteCol),
                Math.Max(0, spriteTopRow));
        }

        // ── Pass 2: flowing text from top ─────────────────────────────────────
        int row = 0;

        if (!string.IsNullOrEmpty(scene.Title))
        {
            AnimationEngine.DrawAt(0, row++, scene.Title, scene.TextColor);
            row++; // blank separator
        }

        foreach (string line in scene.Lines)
        {
            if (row >= winH - 3) break;
            AnimationEngine.DrawAt(0, row++, line, scene.TextColor);
        }

        // ── Continue prompt ───────────────────────────────────────────────────
        if (scene.PromptContinue)
        {
            string prompt = scene.ContinuePrompt ?? CL.Get(CK.Prompt.Continue);
            AnimationEngine.DrawAt(0, winH - 1, prompt, ConsoleColor.DarkGray);
            AnimationEngine.ShowCursor();
            Console.ReadLine();
        }
        else
        {
            AnimationEngine.ShowCursor();
        }
    }
}
