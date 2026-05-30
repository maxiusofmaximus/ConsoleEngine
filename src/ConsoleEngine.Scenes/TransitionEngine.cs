namespace ConsoleEngine.Scenes;

using ConsoleEngine.Rendering;

/// <summary>
/// Screen transition effects between scenes.
///
/// <b>Usage</b>
/// <code>
/// // Wipe out the old scene, then draw the new one on the cleared screen.
/// TransitionEngine.Out(TransitionType.WipeDown);
/// ScenePlayer.Play(nextScene);
/// </code>
///
/// <b>Effects</b>
/// <list type="bullet">
///   <item><see cref="TransitionType.Cut"/> — instant clear, no animation.</item>
///   <item><see cref="TransitionType.FadeToBlack"/> — rows fill with ░→▒→▓→█ in four passes.</item>
///   <item><see cref="TransitionType.WipeDown"/> — black curtain sweeps from top to bottom.</item>
///   <item><see cref="TransitionType.WipeUp"/> — black curtain sweeps from bottom to top.</item>
///   <item><see cref="TransitionType.CloseIn"/> — darkness closes in from all four edges.</item>
/// </list>
/// </summary>
public static class TransitionEngine
{
    /// <summary>Default milliseconds between animation frames.</summary>
    public const int DefaultStepMs = 50;

    // OCP: adding a new TransitionType requires only a new entry here — Out() never changes.
    private static readonly Dictionary<TransitionType, Action<int>> Effects = new()
    {
        [TransitionType.FadeToBlack] = DoFade,
        [TransitionType.WipeDown]    = ms => DoWipe(topToBottom: true,  ms),
        [TransitionType.WipeUp]      = ms => DoWipe(topToBottom: false, ms),
        [TransitionType.CloseIn]     = ms => DoCloseIn(Math.Max(1, ms / 2)),
    };

    /// <summary>
    /// Runs the outgoing transition effect, then calls <see cref="Console.Clear"/>
    /// so the next scene starts on a clean screen.
    /// <see cref="TransitionType.Cut"/> clears immediately with no animation.
    /// </summary>
    /// <param name="type">The transition effect to apply.</param>
    /// <param name="stepMs">
    /// Milliseconds between animation frames.
    /// Defaults to <see cref="DefaultStepMs"/> (50 ms).
    /// Lower values = faster transition.
    /// </param>
    public static void Out(TransitionType type, int stepMs = DefaultStepMs)
    {
        if (type == TransitionType.Cut)
        {
            Console.Clear();
            return;
        }

        if (Effects.TryGetValue(type, out var effect))
            effect(stepMs);

        Console.Clear();
    }

    // ── FadeToBlack ──────────────────────────────────────────────────────────

    private static void DoFade(int stepMs)
    {
        AnimationEngine.HideCursor();
        int h = Console.WindowHeight;
        int w = Console.WindowWidth;
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;

        foreach (char block in new[] { '░', '▒', '▓', '█' })
        {
            string line = new string(block, w);
            for (int y = 0; y < h; y++)
            {
                try { Console.SetCursorPosition(0, y); Console.Write(line); }
                catch { /* ignore out-of-range */ }
            }
            AnimationEngine.Sleep(stepMs);
        }

        Console.ForegroundColor = prev;
    }

    // ── Wipe (down / up) ─────────────────────────────────────────────────────

    private static void DoWipe(bool topToBottom, int stepMs)
    {
        AnimationEngine.HideCursor();
        int h = Console.WindowHeight;
        int w = Console.WindowWidth;
        var prevFg = Console.ForegroundColor;
        var prevBg = Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.BackgroundColor = ConsoleColor.Black;
        string line = new string(' ', w);

        if (topToBottom)
        {
            for (int y = 0; y < h; y++)
            {
                try { Console.SetCursorPosition(0, y); Console.Write(line); }
                catch { }
                AnimationEngine.Sleep(stepMs);
            }
        }
        else
        {
            for (int y = h - 1; y >= 0; y--)
            {
                try { Console.SetCursorPosition(0, y); Console.Write(line); }
                catch { }
                AnimationEngine.Sleep(stepMs);
            }
        }

        Console.ForegroundColor = prevFg;
        Console.BackgroundColor = prevBg;
    }

    // ── CloseIn ───────────────────────────────────────────────────────────────

    private static void DoCloseIn(int stepMs)
    {
        AnimationEngine.HideCursor();
        int h        = Console.WindowHeight;
        int w        = Console.WindowWidth;
        int maxDepth = Math.Max(h, w) / 2 + 1;
        var prev     = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;

        for (int depth = 0; depth < maxDepth; depth++)
        {
            int top    = depth;
            int bottom = h - 1 - depth;
            int left   = depth;
            int right  = w - 1 - depth;

            if (left > right || top > bottom) break;

            // Top row at this depth
            int rowLen = right - left + 1;
            try { Console.SetCursorPosition(left, top);    Console.Write(new string('█', rowLen)); } catch { }

            // Bottom row (skip if same as top)
            if (bottom != top)
            {
                try { Console.SetCursorPosition(left, bottom); Console.Write(new string('█', rowLen)); } catch { }
            }

            // Left column (rows between top and bottom, exclusive)
            for (int y = top + 1; y < bottom; y++)
            {
                try { Console.SetCursorPosition(left, y);  Console.Write('█'); } catch { }
                if (right != left)
                {
                    try { Console.SetCursorPosition(right, y); Console.Write('█'); } catch { }
                }
            }

            AnimationEngine.Sleep(stepMs);
        }

        Console.ForegroundColor = prev;
    }
}
