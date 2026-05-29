namespace ConsoleEngine.Scenes;

using ConsoleEngine.Core;
using ConsoleEngine.Locale;
using ConsoleEngine.Rendering;

/// <summary>
/// Renders a <see cref="DialogueDefinition"/> to the terminal:
/// two characters anchored to the bottom, dialogue lines flowing from the top.
///
/// <b>Layout</b>
/// <code>
/// ┌────────────────────────────────────────────────┐
/// │  [dialogue line 0]                             │  ← rows 0..N
/// │  [dialogue line 1]                             │
/// │  ...                                           │
/// │                                                │
/// │  ██ (left art)           ██ (right art)        │  ← bottom-anchored
/// │  [Left Label]            [Right Label]         │
/// │  [continue prompt]                             │
/// └────────────────────────────────────────────────┘
/// </code>
/// </summary>
public static class DialoguePlayer
{
    /// <summary>
    /// Renders <paramref name="dialogue"/> and blocks until the player presses Enter
    /// (or returns immediately if <see cref="DialogueDefinition.PromptContinue"/> is <c>false</c>).
    /// </summary>
    /// <param name="dialogue">Dialogue to render.</param>
    /// <param name="input">
    /// Input provider used for the continue prompt.
    /// <see langword="null"/> falls back to <see cref="Console.ReadLine"/>.
    /// </param>
    public static void Play(DialogueDefinition dialogue, IInputProvider? input = null)
    {
        Console.Clear();
        AnimationEngine.HideCursor();

        int winH = Console.WindowHeight;
        int winW = Console.WindowWidth;

        // ── Character metrics ──────────────────────────────────────────────────
        int leftW  = dialogue.LeftArt.Length  > 0 ? dialogue.LeftArt.Max(l  => l.Length) : 0;
        int rightW = dialogue.RightArt.Length > 0 ? dialogue.RightArt.Max(l => l.Length) : 0;
        int charH  = Math.Max(dialogue.LeftArt.Length, dialogue.RightArt.Length);

        // Reserve bottom 3 rows: art rows, label row, prompt row.
        int artBottomRow = winH - 3;

        // ── Left character (ASCII art) ─────────────────────────────────────────
        DrawCharacter(
            dialogue.LeftArt,
            dialogue.LeftLabel,
            dialogue.LeftColor,
            x: 1,
            artBottomRow);

        // ── Left PNG sprite (replaces or supplements ASCII art) ───────────────
        if (!string.IsNullOrEmpty(dialogue.LeftSpritePath))
        {
            int spriteRows = 16; // assumes 32×32 PNG (32px / 2 = 16 terminal rows)
            int spriteTop  = artBottomRow - spriteRows + 1;
            PixelArtRenderer.RenderPng(dialogue.LeftSpritePath, 0, Math.Max(0, spriteTop));
        }

        // ── Right character (ASCII art) ────────────────────────────────────────
        int rightX = Math.Max(leftW + 4, winW - rightW - 2);
        DrawCharacter(
            dialogue.RightArt,
            dialogue.RightLabel,
            dialogue.RightColor,
            x: Math.Max(0, rightX),
            artBottomRow);

        // ── Dialogue lines from top ────────────────────────────────────────────
        // Leave two blank rows above the characters as breathing room.
        int textAreaBottom = artBottomRow - charH - 1;
        int row            = 0;

        foreach (string line in dialogue.Lines)
        {
            if (row >= textAreaBottom) break;
            AnimationEngine.DrawAt(0, row++, line, dialogue.TextColor);
        }

        // ── Continue prompt ───────────────────────────────────────────────────
        if (dialogue.PromptContinue)
        {
            string prompt = CL.Get(CK.Prompt.Continue);
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DrawCharacter(
        string[] art, string label, ConsoleColor color, int x, int artBottomRow)
    {
        if (art.Length == 0) return;

        int topRow = artBottomRow - art.Length + 1;
        for (int i = 0; i < art.Length; i++)
            AnimationEngine.DrawAt(x, topRow + i, art[i], color);

        if (!string.IsNullOrEmpty(label))
        {
            int maxW   = art.Max(l => l.Length);
            int labelX = x + Math.Max(0, (maxW - label.Length) / 2);
            AnimationEngine.DrawAt(labelX, artBottomRow + 1, label, color);
        }
    }
}
