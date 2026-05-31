using ConsoleEngine.World;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class WorldMapTests
{
    private static LocationDefinition Loc(string id, string? north = null, string? south = null) =>
        new()
        {
            Id   = id,
            Name = $"Location {id}",
            Exits = new Dictionary<string, string>(
                new[] { ("north", north), ("south", south) }
                    .Where(p => p.Item2 is not null)
                    .Select(p => new KeyValuePair<string, string>(p.Item1, p.Item2!)))
        };

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_EmptyLocations_CreatesEmptyMap()
    {
        var map = new WorldMap([]);
        Assert.Empty(map.AllLocations);
    }

    [Fact]
    public void Constructor_DuplicateIds_ThrowsArgumentException()
    {
        var locs = new[] { Loc("town"), Loc("TOWN") }; // case-insensitive duplicate
        Assert.Throws<ArgumentException>(() => new WorldMap(locs));
    }

    [Fact]
    public void Constructor_NullLocations_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => new WorldMap(null!));
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Get_ExistingId_ReturnsLocation()
    {
        var map = new WorldMap([Loc("town"), Loc("forest")]);
        var loc = map.Get("town");
        Assert.Equal("town", loc.Id);
    }

    [Fact]
    public void Get_CaseInsensitive_ReturnsLocation()
    {
        var map = new WorldMap([Loc("town")]);
        var loc = map.Get("TOWN");
        Assert.Equal("town", loc.Id);
    }

    [Fact]
    public void Get_MissingId_ThrowsKeyNotFoundException()
    {
        var map = new WorldMap([Loc("town")]);
        Assert.Throws<KeyNotFoundException>(() => map.Get("ghost"));
    }

    // ── TryGet ────────────────────────────────────────────────────────────────

    [Fact]
    public void TryGet_ExistingId_ReturnsTrueAndLocation()
    {
        var map = new WorldMap([Loc("town")]);
        bool found = map.TryGet("town", out var loc);
        Assert.True(found);
        Assert.NotNull(loc);
        Assert.Equal("town", loc!.Id);
    }

    [Fact]
    public void TryGet_MissingId_ReturnsFalse()
    {
        var map = new WorldMap([Loc("town")]);
        bool found = map.TryGet("ghost", out var loc);
        Assert.False(found);
        Assert.Null(loc);
    }

    // ── TryMove ───────────────────────────────────────────────────────────────

    [Fact]
    public void TryMove_ValidExit_ReturnsTrueAndDestination()
    {
        var map = new WorldMap([Loc("town", north: "forest"), Loc("forest", south: "town")]);
        bool moved = map.TryMove("town", "north", out var dest);
        Assert.True(moved);
        Assert.Equal("forest", dest!.Id);
    }

    [Fact]
    public void TryMove_InvalidDirection_ReturnsFalse()
    {
        var map = new WorldMap([Loc("town")]);
        bool moved = map.TryMove("town", "east", out var dest);
        Assert.False(moved);
        Assert.Null(dest);
    }

    [Fact]
    public void TryMove_DestinationNotInMap_ReturnsFalse()
    {
        // Exits references "ghost" which doesn't exist in the map
        var loc = new LocationDefinition
        {
            Id = "town", Name = "Town",
            Exits = new Dictionary<string, string> { ["north"] = "ghost" },
        };
        var map = new WorldMap([loc]);
        bool moved = map.TryMove("town", "north", out var dest);
        Assert.False(moved);
        Assert.Null(dest);
    }

    [Fact]
    public void TryMove_FromMissingLocation_ReturnsFalse()
    {
        var map = new WorldMap([Loc("town")]);
        bool moved = map.TryMove("ghost", "north", out _);
        Assert.False(moved);
    }

    // ── AllLocations ──────────────────────────────────────────────────────────

    [Fact]
    public void AllLocations_ReturnsAllAdded()
    {
        var map = new WorldMap([Loc("a"), Loc("b"), Loc("c")]);
        Assert.Equal(3, map.AllLocations.Count);
    }
}
