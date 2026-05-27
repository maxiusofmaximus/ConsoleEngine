namespace ConsoleEngine.Rendering;

/// <summary>
/// Low-level terminal animation primitives.
/// Draws sprites at exact cursor positions, clears rectangles, and controls frame timing.
///
/// <b>Typical usage</b>
/// <code>
/// AnimationEngine.HideCursor();
/// for (int frame = 0; frame &lt; totalFrames; frame++)
/// {
///     AnimationEngine.ClearRect(spriteX, spriteY, spriteW, spriteH);
///     AnimationEngine.DrawSprite(frames[frame], spriteX, spriteY, ConsoleColor.White);
///     AnimationEngine.Sleep(frameDurationMs);
/// }
/// AnimationEngine.ShowCursor();
/// </code>
/// </summary>
public static class AnimationEngine
{
    /// <summary>Hides the terminal cursor during animation playback.</summary>
    public static void HideCursor() { try { Console.CursorVisible = false; } catch { } }

    /// <summary>Restores the terminal cursor after animation playback.</summary>
    public static void ShowCursor() { try { Console.CursorVisible = true;  } catch { } }

    /// <summary>
    /// Draws <paramref name="text"/> at terminal position (<paramref name="x"/>, <paramref name="y"/>)
    /// using the specified colour. Silently ignores positions outside the buffer.
    /// </summary>
    public static void DrawAt(int x, int y, string text, ConsoleColor color = ConsoleColor.Gray)
    {
        if (y < 0 || y >= Console.WindowHeight) return;
        int safeX = Math.Max(0, x);
        if (safeX >= Console.WindowWidth) return;
        try
        {
            Console.SetCursorPosition(safeX, y);
            int maxLen = Console.WindowWidth - safeX;
            string safe = text.Length > maxLen ? text[..maxLen] : text;
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(safe);
            Console.ForegroundColor = prev;
        }
        catch { /* ignore out-of-bounds positioning errors */ }
    }

    /// <summary>
    /// Draws a sprite (array of text lines) with its top-left corner at
    /// (<paramref name="x"/>, <paramref name="y"/>).
    /// </summary>
    public static void DrawSprite(string[] lines, int x, int y, ConsoleColor color = ConsoleColor.Gray)
    {
        for (int i = 0; i < lines.Length; i++)
            DrawAt(x, y + i, lines[i], color);
    }

    /// <summary>
    /// Clears a rectangular region by overwriting with spaces.
    /// Safe to call with coordinates partially outside the window.
    /// </summary>
    public static void ClearRect(int x, int y, int w, int h)
    {
        int safeW = Math.Max(0, Math.Min(w, Console.WindowWidth - Math.Max(0, x)));
        if (safeW == 0) return;
        string blank = new string(' ', safeW);
        for (int i = 0; i < h; i++)
            DrawAt(x, y + i, blank);
    }

    /// <summary>Pauses execution for <paramref name="ms"/> milliseconds.</summary>
    public static void Sleep(int ms) => Thread.Sleep(ms);
}
