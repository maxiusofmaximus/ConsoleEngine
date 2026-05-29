using System;
using System.Collections.Generic;
using ConsoleEngine.Core;

namespace ConsoleEngine.Locale;

/// <summary>
/// Parses a localisation Markdown file into a <see cref="LocalizationTable"/>.
///
/// <b>File format</b> — one key per row in a Markdown table under any heading:
/// <code>
/// # Locale: en
///
/// ## Entries
///
/// | Key                     | Value                            |
/// |-------------------------|----------------------------------|
/// | menu.title              | My Terminal Game                 |
/// | menu.new_game           | New Game                         |
/// | greeting                | Hello, {0}!                      |
/// </code>
///
/// The parser is intentionally tolerant:
/// extra columns, leading/trailing pipes, blank lines, and comment lines
/// (starting with <c>&gt;</c>) are all silently ignored.
/// </summary>
public static class MarkdownLocalizationLoader
{
    /// <summary>
    /// Parses <paramref name="markdownContent"/> and returns a <see cref="LocalizationTable"/>
    /// for the given <paramref name="languageCode"/>.
    /// </summary>
    public static LocalizationTable Parse(string languageCode, string markdownContent)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("A valid language code is required.", nameof(languageCode));
        ArgumentNullException.ThrowIfNull(markdownContent);

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        using var reader = new System.IO.StringReader(markdownContent);
        string? line;
        bool inTable = false;

        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '>')
                continue;

            if (trimmed[0] != '|')
            {
                inTable = false;
                continue;
            }

            if (IsSeparatorRow(trimmed))
            {
                inTable = true;
                continue;
            }

            if (!inTable) continue; // header row — skip

            string[] cells = SplitPipeRow(trimmed);
            if (cells.Length < 2) continue;

            string key   = cells[0].Trim();
            string value = cells[1].Trim();
            if (key.Length > 0)
                entries[key] = value;
        }

        return new LocalizationTable(languageCode, entries);
    }

    private static bool IsSeparatorRow(string trimmedRow)
    {
        string[] cells = SplitPipeRow(trimmedRow);
        if (cells.Length == 0) return false;
        foreach (string cell in cells)
        {
            string c = cell.Trim();
            if (c.Length == 0) return false;
            foreach (char ch in c)
                if (ch != '-' && ch != ':') return false;
        }
        return true;
    }

    private static string[] SplitPipeRow(string trimmedRow)
    {
        int start = trimmedRow[0]                    == '|' ? 1 : 0;
        int end   = trimmedRow[trimmedRow.Length - 1] == '|' ? trimmedRow.Length - 1 : trimmedRow.Length;
        if (end <= start) return Array.Empty<string>();
        return trimmedRow.Substring(start, end - start).Split('|');
    }
}
