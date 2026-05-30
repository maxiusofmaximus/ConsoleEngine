using System.Text.RegularExpressions;

namespace ConsoleEngine.Editor.Terminal;

/// <summary>
/// Strips ANSI/VT100 escape sequences from raw terminal output so it can be
/// displayed in a plain-text Avalonia TextBlock without garbled characters.
/// </summary>
internal static partial class AnsiStripper
{
    // CSI sequences  : ESC [ <params> <final 0x40-0x7E>
    // OSC sequences  : ESC ] <text> BEL  or  ESC ] <text> ESC \
    // Other ESC seqs : ESC <any single char that isn't [ or ])
    [GeneratedRegex(
        @"\x1b(\[[^@-~]*[@-~]|\][^\x07\x1b]*(?:\x07|\x1b\\)|[^\[\]])",
        RegexOptions.Compiled)]
    private static partial Regex Csi();

    // Bare CR (used by terminal apps to overwrite the current line)
    [GeneratedRegex(@"\r(?!\n)", RegexOptions.Compiled)]
    private static partial Regex BareCr();

    /// <summary>Returns <paramref name="raw"/> with all ANSI escape sequences removed.</summary>
    public static string Strip(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        string s = Csi().Replace(raw, string.Empty);
        return BareCr().Replace(s, "\n");
    }
}
