using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleEngine.Rendering;

/// <summary>
/// Renders pixel-art sprites in the terminal using Unicode ▀ half-block characters.
///
/// <b>Encoding</b>
/// One terminal character represents two vertically-stacked pixels:
/// <list type="bullet">
///   <item>top pixel    → foreground colour  <c>\x1b[38;2;R;G;Bm</c></item>
///   <item>bottom pixel → background colour  <c>\x1b[48;2;R;G;Bm</c></item>
/// </list>
/// A 32×32 PNG therefore occupies 32 chars wide × 16 terminal rows.
///
/// <b>Requirements</b>
/// Requires ANSI true-colour support. Call <see cref="EnableAnsi"/> once at startup on Windows.
/// On Windows Terminal, cmd, and PowerShell 7+ this works out of the box.
/// </summary>
public static class PixelArtRenderer
{
    // ── Colour type ───────────────────────────────────────────────────────────

    /// <summary>24-bit RGB colour.</summary>
    public readonly record struct Rgb(byte R, byte G, byte B);

    // ── Win32 — enable Virtual Terminal Processing ────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(nint handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(nint handle, uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int nStdHandle);

    private const int  StdOutputHandle                = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    /// <summary>Enables ANSI escape sequences in the Windows console. No-op on other platforms.</summary>
    public static void EnableAnsi()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            nint handle = GetStdHandle(StdOutputHandle);
            if (GetConsoleMode(handle, out uint mode))
                SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }
        catch { /* non-fatal; ANSI may still work in Windows Terminal */ }
    }

    // ── PNG renderer ─────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a PNG file and renders it at terminal position (<paramref name="col"/>, <paramref name="row"/>).
    /// Pixels with alpha &lt; 128 are treated as transparent.
    /// </summary>
    /// <returns><c>true</c> on success; <c>false</c> if the file cannot be loaded.</returns>
    public static bool RenderPng(string path, int col, int row)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        try
        {
            using var bmp = new System.Drawing.Bitmap(path);
            int w = bmp.Width, h = bmp.Height;
            var pix   = new Rgb[h, w];
            var solid = new bool[h, w];

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = bmp.GetPixel(x, y);
                pix[y, x]   = new Rgb(c.R, c.G, c.B);
                solid[y, x] = c.A >= 128;
            }

            RenderCore(pix, solid, col, row);
            return true;
        }
        catch { return false; }
    }

    // ── Embedded sprite renderer ──────────────────────────────────────────────

    /// <summary>
    /// Renders a pre-built RGB pixel grid at (<paramref name="col"/>, <paramref name="row"/>).
    /// Pixels equal to <paramref name="transparent"/> are skipped.
    /// </summary>
    public static void RenderRgb(Rgb[,] pixels, int col, int row, Rgb transparent = default)
    {
        int h = pixels.GetLength(0), w = pixels.GetLength(1);
        var solid = new bool[h, w];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            solid[y, x] = pixels[y, x] != transparent;
        RenderCore(pixels, solid, col, row);
    }

    // ── Core ─────────────────────────────────────────────────────────────────

    private static void RenderCore(Rgb[,] pixels, bool[,] solid, int col, int row)
    {
        int h = pixels.GetLength(0), w = pixels.GetLength(1);
        var sb = new StringBuilder(w * 28);

        for (int y = 0; y < h; y += 2)
        {
            int termRow = row + y / 2;
            if (termRow >= Console.WindowHeight) break;
            Console.SetCursorPosition(Math.Max(0, col), termRow);
            sb.Clear();

            for (int x = 0; x < w; x++)
            {
                bool topOk = solid[y, x];
                bool botOk = (y + 1 < h) && solid[y + 1, x];

                if (!topOk && !botOk) { sb.Append("\x1b[0m "); continue; }

                Rgb top = topOk ? pixels[y,     x] : default;
                Rgb bot = botOk ? pixels[y + 1, x] : default;

                if      (topOk && botOk) sb.Append(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"\x1b[38;2;{top.R};{top.G};{top.B}m\x1b[48;2;{bot.R};{bot.G};{bot.B}m▀"));
                else if (topOk)          sb.Append(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"\x1b[38;2;{top.R};{top.G};{top.B}m\x1b[49m▀"));
                else                     sb.Append(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"\x1b[38;2;{bot.R};{bot.G};{bot.B}m\x1b[49m▄"));
            }

            sb.Append("\x1b[0m");
            Console.Write(sb.ToString());
        }

        Console.Write("\x1b[0m");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Number of terminal rows a sprite of <paramref name="pixelHeight"/> pixel rows occupies.</summary>
    public static int TerminalRows(int pixelHeight) => (pixelHeight + 1) / 2;

    /// <summary>
    /// Builds an <c>Rgb[,]</c> from a character map and palette dictionary.
    /// Unmatched characters map to <paramref name="transparent"/>.
    /// </summary>
    public static Rgb[,] BuildSprite(string[] rows, Dictionary<char, Rgb> palette, Rgb transparent = default)
    {
        int h = rows.Length;
        int w = rows.Max(r => r.Length);
        var result = new Rgb[h, w];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            char ch = x < rows[y].Length ? rows[y][x] : ' ';
            result[y, x] = palette.TryGetValue(ch, out var rgb) ? rgb : transparent;
        }
        return result;
    }
}
