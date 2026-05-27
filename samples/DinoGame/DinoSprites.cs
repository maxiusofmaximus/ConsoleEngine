namespace DinoGame;

/// <summary>
/// All ASCII-art sprites for the DinoGame sample.
/// Sprites use Unicode block characters for a pixel-art look.
///
/// Coordinate system: top-left = (0,0), Y increases downward.
/// Sprite arrays are drawn top-row-first.
/// </summary>
internal static class DinoSprites
{
    // ── Dino (4 wide × 4 tall) ────────────────────────────────────────────────

    /// <summary>Running frame A — left leg forward.</summary>
    public static readonly string[] RunA =
    {
        " ▄█",   // head / neck
        "▄███",  // body
        " ██ ",  // lower body
        " █  ",  // left leg up
    };

    /// <summary>Running frame B — right leg forward.</summary>
    public static readonly string[] RunB =
    {
        " ▄█",
        "▄███",
        " ██ ",
        "  █ ",  // right leg up
    };

    /// <summary>Airborne — both legs tucked.</summary>
    public static readonly string[] Jump =
    {
        " ▄█",
        "▄███",
        " ██ ",
        " ▀▀ ",  // legs tucked
    };

    /// <summary>Dead — X marks the eye.</summary>
    public static readonly string[] Dead =
    {
        " ▄▀",
        "▄X██",  // X = dead eye
        " ██ ",
        " ██ ",  // both legs down
    };

    /// <summary>Logical width of the dino sprite.</summary>
    public const int Width = 4;

    /// <summary>Logical height of the dino sprite.</summary>
    public const int Height = 4;

    // ── Cacti ─────────────────────────────────────────────────────────────────
    // All cactus variants are 3 characters wide.
    // CactusVariants[0] = short (3 rows), [1] = medium (4 rows), [2] = tall (5 rows).

    public static readonly string[][] CactusVariants =
    {
        // Short
        new[]
        {
            " █ ",
            "▄█▄",
            "███",
        },

        // Medium
        new[]
        {
            " █ ",
            " █ ",
            "▄█▄",
            "███",
        },

        // Tall
        new[]
        {
            " █ ",
            "▄█▄",
            "███",
            " █ ",
            " █ ",
        },
    };

    /// <summary>Width of every cactus sprite.</summary>
    public const int CactusWidth = 3;

    // ── Ground ────────────────────────────────────────────────────────────────

    /// <summary>Fills a row with ground-line characters (═).</summary>
    public static string GroundLine(int width) => new('═', width);

    /// <summary>Fills a row with floor-block characters (▓).</summary>
    public static string FloorLine(int width) => new('▓', width);

    // ── Clouds ────────────────────────────────────────────────────────────────

    /// <summary>Simple cloud sprite (7 wide × 2 tall).</summary>
    public static readonly string[] Cloud =
    {
        " ▄▄▄▄ ",
        "▀▀▀▀▀▀",
    };
}
