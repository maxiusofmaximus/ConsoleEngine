using ConsoleEngine.Input;
using ConsoleEngine.World;
using Xunit;

namespace ConsoleEngine.Tests;

/// <summary>
/// Tests for ExplorationPlayer command routing.
/// Rendering is attempted but safe: AnimationEngine wraps all Console calls in try-catch.
/// Every test must end with "quit" or "menu" in the command queue to avoid an infinite loop.
/// </summary>
public sealed class ExplorationPlayerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorldMap MakeMap(params (string id, string name, Dictionary<string, string>? exits)[] locations)
    {
        var defs = locations.Select(l => new LocationDefinition
        {
            Id          = l.id,
            Name        = l.name,
            Description = [],
            Exits       = l.exits ?? new Dictionary<string, string>(),
        });
        return new WorldMap(defs);
    }

    private static WorldState MakeState(string startId) =>
        new() { CurrentLocationId = startId };

    private static MockInputProvider Commands(params string[] lines)
    {
        var mock = new MockInputProvider();
        foreach (string line in lines)
            mock.EnqueueLine(line);
        return mock;
    }

    // ── Exit commands ─────────────────────────────────────────────────────────

    [Fact]
    public void Quit_ReturnsQuitResult()
    {
        WorldMap   map   = MakeMap(("start", "Start", null));
        WorldState state = MakeState("start");

        ExplorationResult result = ExplorationPlayer.Run(map, state, inputProvider: Commands("quit"));

        Assert.Equal(ExplorationResult.Quit, result);
    }

    [Fact]
    public void Menu_ReturnsReturnToMenuResult()
    {
        WorldMap   map   = MakeMap(("start", "Start", null));
        WorldState state = MakeState("start");

        ExplorationResult result = ExplorationPlayer.Run(map, state, inputProvider: Commands("menu"));

        Assert.Equal(ExplorationResult.ReturnToMenu, result);
    }

    [Fact]
    public void Back_AliasForMenu_ReturnsReturnToMenu()
    {
        WorldMap   map   = MakeMap(("start", "Start", null));
        WorldState state = MakeState("start");

        ExplorationResult result = ExplorationPlayer.Run(map, state, inputProvider: Commands("back"));

        Assert.Equal(ExplorationResult.ReturnToMenu, result);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    [Fact]
    public void North_ValidExit_MovesPlayer()
    {
        WorldMap map = MakeMap(
            ("town",   "Town",   new() { ["north"] = "forest" }),
            ("forest", "Forest", new()));
        WorldState state = MakeState("town");

        ExplorationPlayer.Run(map, state, inputProvider: Commands("north", "quit"));

        Assert.Equal("forest", state.CurrentLocationId);
    }

    [Fact]
    public void DirectionAlias_N_MovesNorth()
    {
        WorldMap map = MakeMap(
            ("a", "A", new() { ["north"] = "b" }),
            ("b", "B", new()));
        WorldState state = MakeState("a");

        ExplorationPlayer.Run(map, state, inputProvider: Commands("n", "quit"));

        Assert.Equal("b", state.CurrentLocationId);
    }

    [Fact]
    public void Go_Prefix_MovesPlayer()
    {
        WorldMap map = MakeMap(
            ("a", "A", new() { ["south"] = "b" }),
            ("b", "B", new()));
        WorldState state = MakeState("a");

        ExplorationPlayer.Run(map, state, inputProvider: Commands("go south", "quit"));

        Assert.Equal("b", state.CurrentLocationId);
    }

    [Fact]
    public void InvalidDirection_DoesNotMovePlayer()
    {
        WorldMap   map   = MakeMap(("island", "Island", new()));
        WorldState state = MakeState("island");

        ExplorationPlayer.Run(map, state, inputProvider: Commands("north", "quit"));

        Assert.Equal("island", state.CurrentLocationId);
    }

    [Fact]
    public void MultipleMovements_UpdatesLocationEachTime()
    {
        WorldMap map = MakeMap(
            ("a", "A", new() { ["east"] = "b" }),
            ("b", "B", new() { ["east"] = "c" }),
            ("c", "C", new()));
        WorldState state = MakeState("a");

        ExplorationPlayer.Run(map, state, inputProvider: Commands("east", "east", "quit"));

        Assert.Equal("c", state.CurrentLocationId);
    }

    // ── Wait command ──────────────────────────────────────────────────────────

    [Fact]
    public void Wait_AdvancesTime()
    {
        WorldMap   map   = MakeMap(("room", "Room", null));
        WorldState state = MakeState("room");
        var initialTime = state.TimeOfDay;

        ExplorationPlayer.Run(map, state, inputProvider: Commands("wait", "quit"));

        Assert.NotEqual(initialTime, state.TimeOfDay);
    }

    // ── OnLocationEnter callback ──────────────────────────────────────────────

    [Fact]
    public void OnLocationEnter_FiredOnFirstEnter()
    {
        WorldMap   map     = MakeMap(("start", "Start", null));
        WorldState state   = MakeState("start");
        string?    visited = null;

        var options = new ExplorationOptions
        {
            OnLocationEnter = (_, loc) => visited = loc.Id,
        };

        ExplorationPlayer.Run(map, state, options, Commands("quit"));

        Assert.Equal("start", visited);
    }

    [Fact]
    public void OnLocationEnter_FiredOnMovement()
    {
        WorldMap map = MakeMap(
            ("a", "A", new() { ["north"] = "b" }),
            ("b", "B", new()));
        WorldState state    = MakeState("a");
        var        visited  = new List<string>();

        var options = new ExplorationOptions
        {
            OnLocationEnter = (_, loc) => visited.Add(loc.Id),
        };

        ExplorationPlayer.Run(map, state, options, Commands("north", "quit"));

        Assert.Contains("a", visited);
        Assert.Contains("b", visited);
    }

    // ── Invalid start location ────────────────────────────────────────────────

    [Fact]
    public void InvalidStartLocation_ThrowsInvalidOperationException()
    {
        WorldMap   map   = MakeMap(("town", "Town", null));
        WorldState state = MakeState("nowhere");

        Assert.Throws<InvalidOperationException>(() =>
            ExplorationPlayer.Run(map, state, inputProvider: Commands("quit")));
    }
}
