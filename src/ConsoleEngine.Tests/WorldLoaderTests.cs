using ConsoleEngine.World;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class WorldLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public WorldLoaderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string Write(string name, string json)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Load_ValidWorld_ReturnsMap()
    {
        string path = Write("test.world.json", """
            {
              "schemaVersion": 1,
              "locations": [
                {
                  "id": "town",
                  "name": "Market Town",
                  "description": ["Merchants call out."],
                  "exits": { "north": "forest" }
                }
              ]
            }
            """);

        WorldMap map = WorldLoader.Load(path);

        LocationDefinition town = map.Get("town");
        Assert.Equal("Market Town", town.Name);
        Assert.Single(town.Description);
        Assert.True(town.Exits.ContainsKey("north"));
    }

    [Fact]
    public void Load_MissingId_ThrowsInvalidDataException()
    {
        string path = Write("noid.world.json", """
            { "locations": [{ "name": "Unnamed" }] }
            """);

        Assert.Throws<InvalidDataException>(() => WorldLoader.Load(path));
    }

    [Fact]
    public void Load_EmptyLocations_ThrowsInvalidDataException()
    {
        string path = Write("empty.world.json", """
            { "schemaVersion": 1, "locations": [] }
            """);

        Assert.Throws<InvalidDataException>(() => WorldLoader.Load(path));
    }

    [Fact]
    public void Load_FutureSchemaVersion_ThrowsInvalidDataException()
    {
        string path = Write("future.world.json", """
            {
              "schemaVersion": 999,
              "locations": [{ "id": "x", "name": "X" }]
            }
            """);

        Assert.Throws<InvalidDataException>(() => WorldLoader.Load(path));
    }

    [Fact]
    public void TryMove_ValidDirection_ReturnsDestination()
    {
        string path = Write("nav.world.json", """
            {
              "schemaVersion": 1,
              "locations": [
                { "id": "start", "name": "Start", "exits": { "north": "end" } },
                { "id": "end",   "name": "End",   "exits": {} }
              ]
            }
            """);

        WorldMap map = WorldLoader.Load(path);
        bool moved = map.TryMove("start", "north", out LocationDefinition? dest);

        Assert.True(moved);
        Assert.Equal("end", dest!.Id);
    }

    [Fact]
    public void TryLoad_ValidFile_ReturnsTrueAndMap()
    {
        string path = Write("ok.world.json", """
            {
              "schemaVersion": 1,
              "locations": [{ "id": "town", "name": "Town" }]
            }
            """);

        bool ok = WorldLoader.TryLoad(path, out WorldMap? map);

        Assert.True(ok);
        Assert.NotNull(map);
    }

    [Fact]
    public void TryLoad_MissingFile_ReturnsFalse()
    {
        bool ok = WorldLoader.TryLoad(Path.Combine(_tempDir, "nope.world.json"), out WorldMap? map);
        Assert.False(ok);
        Assert.Null(map);
    }

    [Fact]
    public void TryMove_InvalidDirection_ReturnsFalse()
    {
        string path = Write("oneexit.world.json", """
            {
              "schemaVersion": 1,
              "locations": [
                { "id": "room", "name": "Room", "exits": { "north": "room" } }
              ]
            }
            """);

        WorldMap map = WorldLoader.Load(path);
        bool moved = map.TryMove("room", "south", out LocationDefinition? dest);

        Assert.False(moved);
        Assert.Null(dest);
    }

    [Fact]
    public void Get_UnknownLocation_ThrowsKeyNotFoundException()
    {
        string path = Write("single.world.json", """
            {
              "schemaVersion": 1,
              "locations": [{ "id": "home", "name": "Home" }]
            }
            """);

        WorldMap map = WorldLoader.Load(path);
        Assert.Throws<KeyNotFoundException>(() => map.Get("nowhere"));
    }

    [Fact]
    public void Load_LocationWithNoExits_HasEmptyExits()
    {
        string path = Write("noexit.world.json", """
            {
              "schemaVersion": 1,
              "locations": [{ "id": "island", "name": "Island" }]
            }
            """);

        WorldMap map = WorldLoader.Load(path);
        LocationDefinition loc = map.Get("island");
        Assert.Empty(loc.Exits);
    }
}
