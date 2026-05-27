using ConsoleEngine.Core;
using ConsoleEngine.Locale;
using ConsoleEngine.Rendering;

// ── 1. Enable ANSI true-colour on Windows ────────────────────────────────────
PixelArtRenderer.EnableAnsi();

// ── 2. Initialise localisation from GameData/locale/ ─────────────────────────
//      Change "en" to "es" to see the Spanish translation.
string gameDataRoot = Path.Combine(AppContext.BaseDirectory, "GameData");
CL.Initialize(gameDataRoot, language: "en");

// ── 3. Render a pixel-art sprite using the half-block renderer ────────────────
//      This 8×8 smiley is built from a char-map palette — no PNG required.
var palette = new Dictionary<char, PixelArtRenderer.Rgb>
{
    ['.'] = new(0,   0,   0),   // transparent-ish (black background)
    ['Y'] = new(255, 220, 0),   // yellow face
    ['E'] = new(30,  30,  30),  // dark eyes
    ['S'] = new(180, 60,  60),  // red smile
};

string[] spriteMap =
{
    ".YYYYYY.",
    "YYYYYYYY",
    "YEYYYEYY",
    "YYYYYYYY",
    "YSSSSYYY",  // smile
    "YYSSSSYY",
    "YYYYYYYY",
    ".YYYYYY.",
};

var sprite = PixelArtRenderer.BuildSprite(spriteMap, palette, transparent: new(0, 0, 0));

Console.Clear();

// Draw sprite at column 4, row 2
PixelArtRenderer.RenderRgb(sprite, col: 4, row: 2);

// ── 4. Print localised welcome message below the sprite ───────────────────────
Console.SetCursorPosition(0, 7);
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine();
Console.WriteLine($"  {CL.Get("hello.welcome")}");
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"  {CL.Get("hello.description")}");
Console.WriteLine($"  {EngineVersion.Display}");
Console.ResetColor();
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.Write($"  {CL.Get("hello.prompt")} ");
Console.ResetColor();
Console.ReadLine();
