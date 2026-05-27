using ConsoleEngine.Locale;
using ConsoleEngine.Rendering;
using ConsoleEngine.Scenes;
using ConsoleEngine.World;
using WorldDemo;

// ── Bootstrap ──────────────────────────────────────────────────────────────────
PixelArtRenderer.EnableAnsi();
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CursorVisible  = false;
Console.Title          = "WorldDemo — ConsoleEngine v0.3.0";

string gameDataRoot = Path.Combine(AppContext.BaseDirectory, "GameData");
CL.Initialize(gameDataRoot, "en");

// ── Build world ────────────────────────────────────────────────────────────────
WorldMap   map   = TheWorld.Build();
WorldState state = new() { CurrentLocationId = "crossroads", TimeOfDay = TimeOfDay.Morning, Day = 1 };

// ── Intro scene ────────────────────────────────────────────────────────────────
int w = Console.WindowWidth;
ScenePlayer.Play(new SceneDefinition
{
    AsciiArt = new[]
    {
        new string(' ', Math.Max(0, w - 20)) + "  ▲▲  ▲  ",
        new string(' ', Math.Max(0, w - 20)) + " ████ ██  ",
        new string(' ', Math.Max(0, w - 20)) + " ████ ██  ",
        new string(' ', Math.Max(0, w - 20)) + "──┼───────",
        new string('═', w),
        new string('▓', w),
    },
    Lines = new[]
    {
        "",
        "  ┌──────────────────────────────────────┐",
        "  │                                      │",
        "  │   W O R L D   D E M O               │",
        "  │   ConsoleEngine  v0.3.0              │",
        "  │                                      │",
        "  │   A five-room exploration demo.      │",
        "  │                                      │",
        "  │   Commands: north · south · east     │",
        "  │             west · in · out · look   │",
        "  │             exits · wait · help      │",
        "  │             menu · quit              │",
        "  │                                      │",
        "  └──────────────────────────────────────┘",
    },
    TextColor      = ConsoleColor.Cyan,
    ArtColor       = ConsoleColor.DarkGreen,
    ContinuePrompt = "  [ PRESS ENTER TO EXPLORE ]",
});

// ── Exploration options ────────────────────────────────────────────────────────
var options = new ExplorationOptions
{
    HudColor       = ConsoleColor.Cyan,
    TextColor      = ConsoleColor.Gray,
    ExitColor      = ConsoleColor.DarkCyan,
    MsgColor       = ConsoleColor.Yellow,
    MoveTransition = TransitionType.FadeToBlack,
    TransitionStepMs = 40,
    ShowExits      = true,

    // Custom action: "look up" only available on the hilltop
    CustomActions = new IExplorationAction[] { new LookUpAction() },
};

// ── Main exploration loop ──────────────────────────────────────────────────────
ExplorationResult result = ExplorationPlayer.Run(map, state, options);

// ── Outro ──────────────────────────────────────────────────────────────────────
Console.Clear();
AnimationEngine.ShowCursor();
Console.ResetColor();

ScenePlayer.Play(new SceneDefinition
{
    Lines = result == ExplorationResult.Quit
        ? new[] { "", "  Goodbye.", "", $"  You reached Day {state.Day}, {state.TimeOfDay}." }
        : new[] { "", "  Back to the menu.", "", $"  You reached Day {state.Day}, {state.TimeOfDay}." },
    TextColor      = ConsoleColor.DarkGray,
    ContinuePrompt = "  [ PRESS ENTER ]",
});

// ── Custom action example ──────────────────────────────────────────────────────

/// <summary>
/// "look up" command: only available on the hilltop.
/// Demonstrates the IExplorationAction extension point.
/// </summary>
sealed class LookUpAction : IExplorationAction
{
    public string Command     => "look up";
    public string Description => "Gaze at the sky from the hilltop.";

    public bool IsAvailable(WorldState state, LocationDefinition location)
        => location.Id == "hilltop";

    public ExplorationOutcome Execute(WorldState state, LocationDefinition location)
        => ExplorationOutcome.Continue(
            "You tilt your head back. Stars are already appearing, even in daylight.");
}
