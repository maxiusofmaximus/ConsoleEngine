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
WorldMap   map   = TheWorld.Build(gameDataRoot);
WorldState state = new() { CurrentLocationId = "crossroads", TimeOfDay = TimeOfDay.Morning, Day = 1 };

// ── Intro scene ────────────────────────────────────────────────────────────────
ScenePlayer.Play(SceneLoader.Load(Path.Combine(gameDataRoot, "scenes", "intro.scene.json")));

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

string[] outroLines = result == ExplorationResult.Quit
    ? ["", "  Goodbye.", "", $"  You reached Day {state.Day}, {CL.Get(state.TimeOfDay.LocaleKey())}."]
    : ["", "  Back to the menu.", "", $"  You reached Day {state.Day}, {CL.Get(state.TimeOfDay.LocaleKey())}."];

ScenePlayer.Play(
    SceneLoader.Load(Path.Combine(gameDataRoot, "scenes", "outro.scene.json"))
    with { Lines = outroLines });

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
