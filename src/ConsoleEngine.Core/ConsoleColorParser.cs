namespace ConsoleEngine.Core;

/// <summary>
/// Parses <see cref="ConsoleColor"/> values from strings, shared across all loaders
/// that accept color names in JSON/Markdown data files.
/// </summary>
public static class ConsoleColorParser
{
    /// <summary>
    /// Converts <paramref name="name"/> to a <see cref="ConsoleColor"/> (case-insensitive).
    /// Returns <paramref name="fallback"/> when <paramref name="name"/> is
    /// <see langword="null"/>, empty, or not a valid color name.
    /// </summary>
    public static ConsoleColor Parse(string? name, ConsoleColor fallback = ConsoleColor.Gray) =>
        name is not null && Enum.TryParse(name, ignoreCase: true, out ConsoleColor c)
            ? c
            : fallback;
}
