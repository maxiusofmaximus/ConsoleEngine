namespace ConsoleEngine.Scenes;

using System.Globalization;
using System.Text;
using ConsoleEngine.Core;
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
    /// <param name="scene">Scene to render.</param>
    /// <param name="input">
    /// Input provider used for the continue prompt.
    /// <see langword="null"/> falls back to <see cref="Console.ReadLine"/>.
    /// </param>
    public static void Play(SceneDefinition scene, IInputProvider? input = null)
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
            string prompt = scene.ContinuePrompt ?? CL.Get(CK.Prompt.Continue, "[ PRESS ENTER TO CONTINUE ]");
            AnimationEngine.DrawAt(0, winH - 1, prompt, ConsoleColor.DarkGray);
            AnimationEngine.ShowCursor();
            if (input is not null) input.ReadLine();
            else Console.ReadLine();
        }
        else
        {
            AnimationEngine.ShowCursor();
        }
    }

    /// <summary>
    /// Renders <paramref name="scene"/> to a plain text string for editor preview or testing.
    /// Uses the same layout logic as <see cref="Play"/> but writes to a <see cref="StringBuilder"/>
    /// instead of the real terminal. No console I/O is performed.
    /// </summary>
    /// <param name="scene">Scene to render.</param>
    /// <param name="previewWidth">Simulated terminal width in characters. Default 54.</param>
    /// <param name="previewHeight">Simulated terminal height in rows. Default 24.</param>
    public static string RenderToString(SceneDefinition scene, int previewWidth = 54, int previewHeight = 24)
    {
        var sb = new StringBuilder();
        string bar  = new('═', previewWidth);
        string line2 = new('─', previewWidth);

        sb.AppendLine(bar);

        // ── Title ─────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(scene.Title))
        {
            sb.Append("  ").AppendLine(scene.Title);
            sb.AppendLine(line2);
        }

        sb.AppendLine();

        // ── Narration lines ───────────────────────────────────────────────────
        foreach (string l in scene.Lines)
            sb.Append("  ").AppendLine(l);

        sb.AppendLine();
        sb.AppendLine(line2);

        // ── ASCII art ─────────────────────────────────────────────────────────
        if (scene.AsciiArt is { Length: > 0 } art)
        {
            sb.Append("  [Art · color: ").Append(scene.ArtColor).AppendLine("]");
            foreach (string artRow in art)
                sb.Append("  ").AppendLine(artRow);
        }

        // ── Sprite note ───────────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(scene.SpritePath))
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"  [Sprite: {Path.GetFileName(scene.SpritePath)} · {scene.SpriteWidth}×{scene.SpriteRows * 2}px]"));

        sb.AppendLine(bar);

        // ── Continue prompt ───────────────────────────────────────────────────
        if (scene.PromptContinue)
        {
            string prompt = scene.ContinuePrompt ?? "[ PRESS ENTER TO CONTINUE ]";
            sb.Append("  ").AppendLine(prompt);
        }

        return sb.ToString();
    }
}
