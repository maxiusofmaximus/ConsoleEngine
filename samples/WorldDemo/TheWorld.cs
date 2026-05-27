using ConsoleEngine.World;

namespace WorldDemo;

/// <summary>
/// The five-location world used by the WorldDemo sample.
///
/// Layout:
/// <code>
///        [hilltop]
///            |
///        [forest]
///            |
///   [ruins]──[crossroads]
///     |
///  [cave]
/// </code>
/// </summary>
internal static class TheWorld
{
    public static WorldMap Build() => new(new LocationDefinition[]
    {
        Crossroads(),
        Forest(),
        Hilltop(),
        Ruins(),
        Cave(),
    });

    // ── Locations ──────────────────────────────────────────────────────────────

    private static LocationDefinition Crossroads() => new()
    {
        Id          = "crossroads",
        Name        = "The Crossroads",
        Description = new[]
        {
            "A dirt road splits here in two directions.",
            "A weathered signpost stands at the centre, its paint long faded.",
            "The air smells of pine resin and distant rain.",
        },
        AsciiArt = new[]
        {
            "           |           ",
            "   ────────┼────────   ",
            "           |           ",
        },
        ArtColor  = ConsoleColor.DarkYellow,
        TextColor = ConsoleColor.Gray,
        Exits     = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["north"] = "forest",
            ["east"]  = "ruins",
        },
    };

    private static LocationDefinition Forest() => new()
    {
        Id          = "forest",
        Name        = "Dark Forest",
        Description = new[]
        {
            "Tall pines crowd out most of the light.",
            "The undergrowth rustles — something watches from the shadows.",
            "A faint path continues north toward higher ground.",
        },
        AsciiArt = new[]
        {
            "  ▲  ▲▲  ▲  ▲▲ ▲  ",
            " ███ ████ ██ ███   ",
            " ███ ████ ██ ███   ",
        },
        ArtColor  = ConsoleColor.DarkGreen,
        TextColor = ConsoleColor.Gray,
        Exits     = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["north"] = "hilltop",
            ["south"] = "crossroads",
        },
    };

    private static LocationDefinition Hilltop() => new()
    {
        Id          = "hilltop",
        Name        = "Windy Hilltop",
        Description = new[]
        {
            "You emerge from the trees onto a windswept hilltop.",
            "The view stretches for miles — forests, ruins, and a distant river.",
            "Up here the sky feels enormous.",
        },
        AsciiArt = new[]
        {
            "          /\\          ",
            "         /  \\         ",
            "        / /\\ \\        ",
            "───────/      \\───────",
        },
        ArtColor  = ConsoleColor.DarkGray,
        TextColor = ConsoleColor.Gray,
        Exits     = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["south"] = "forest",
        },
    };

    private static LocationDefinition Ruins() => new()
    {
        Id          = "ruins",
        Name        = "Ancient Ruins",
        Description = new[]
        {
            "Crumbling stone columns rise from the earth.",
            "Carvings cover every surface — a language no one has spoken for centuries.",
            "A dark opening in the hillside beckons. Type [in] to enter.",
        },
        AsciiArt = new[]
        {
            " | |  | |  | |  ",
            " | |  | |  | |  ",
            "─┴─┴──┴─┴──┴─┴─",
        },
        ArtColor  = ConsoleColor.DarkGray,
        TextColor = ConsoleColor.Gray,
        Exits     = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["west"] = "crossroads",
            ["in"]   = "cave",
        },
    };

    private static LocationDefinition Cave() => new()
    {
        Id          = "cave",
        Name        = "The Cave",
        Description = new[]
        {
            "Stone walls glisten with moisture.",
            "Crude paintings cover the walls — hunters, beasts, and a spiral symbol.",
            "At the far end lies a small altar. Whatever was placed here is long gone.",
        },
        AsciiArt = new[]
        {
            "    ╔════════╗    ",
            "   ╔╝ ◉    ◉ ╚╗   ",
            "   ╚══════════╝   ",
        },
        ArtColor  = ConsoleColor.DarkMagenta,
        TextColor = ConsoleColor.DarkGray,
        Exits     = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["out"] = "ruins",
        },
    };
}
