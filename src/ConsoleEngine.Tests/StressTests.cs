using System.Text;
using System.Text.Json;
using ConsoleEngine.Animation;
using ConsoleEngine.Core;
using ConsoleEngine.Locale;
using ConsoleEngine.Persistence;
using ConsoleEngine.Rendering;
using ConsoleEngine.Scenes;
using ConsoleEngine.World;
using Xunit;

namespace ConsoleEngine.Tests;

/// <summary>
/// Stress and destructive tests that probe edge cases, extreme inputs, and concurrent access.
/// These tests establish evidence about the system's resilience before optimization work begins.
/// </summary>
[Trait("Category", "Stress")]
public sealed class StressTests : IDisposable
{
    private readonly string _tempDir;

    public StressTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ce_stress_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string TempFile(string name, string content)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private string TempBinary(string name, byte[] bytes)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    // ── PixelArtRenderer — extreme sizes and corrupt input ────────────────────

    [Fact]
    public void PixelArtRenderer_ToAnsiString_1024x1024_DoesNotCrash()
    {
        var rows = Enumerable.Range(0, 1024).Select(_ => new string('R', 1024)).ToArray();
        var palette = new Dictionary<char, PixelArtRenderer.Rgb> { ['R'] = new(200, 50, 50) };
        var pixels = PixelArtRenderer.BuildSprite(rows, palette);

        string result = PixelArtRenderer.ToAnsiString(pixels);
        Assert.NotNull(result);
        Assert.NotEmpty(result); // 1M pixels must produce non-empty ANSI
    }

    [Fact]
    public void PixelArtRenderer_BuildSprite_1000x1000_DoesNotCrash()
    {
        var rows    = Enumerable.Range(0, 1000).Select(_ => new string('G', 1000)).ToArray();
        var palette = new Dictionary<char, PixelArtRenderer.Rgb> { ['G'] = new(0, 200, 0) };
        var pixels  = PixelArtRenderer.BuildSprite(rows, palette);

        Assert.Equal(1000, pixels.GetLength(0));
        Assert.Equal(1000, pixels.GetLength(1));
    }

    [Fact]
    public void PixelArtRenderer_RenderPng_CorruptedFile_ReturnsFalse()
    {
        string path = TempBinary("corrupt.png", [0xFF, 0xFE, 0x00, 0x01, 0xAB, 0xCD]);
        bool result = PixelArtRenderer.RenderPng(path, 0, 0);
        Assert.False(result);
    }

    [Fact]
    public void PixelArtRenderer_RenderPng_EmptyFile_ReturnsFalse()
    {
        string path = TempFile("empty.png", "");
        bool result = PixelArtRenderer.RenderPng(path, 0, 0);
        Assert.False(result);
    }

    [Fact]
    public void PixelArtRenderer_ToAnsiString_MissingPath_ReturnsNull()
    {
        string? result = PixelArtRenderer.ToAnsiString(Path.Combine(_tempDir, "ghost.png"));
        Assert.Null(result);
    }

    [Fact]
    public void PixelArtRenderer_RenderPng_NonExistentPath_ReturnsFalse()
    {
        bool result = PixelArtRenderer.RenderPng(Path.Combine(_tempDir, "ghost.png"), 0, 0);
        Assert.False(result);
    }

    [Fact]
    public void PixelArtRenderer_ToAnsiString_1x1_ProducesNonEmptyString()
    {
        var pixels = PixelArtRenderer.BuildSprite(
            ["R"],
            new Dictionary<char, PixelArtRenderer.Rgb> { ['R'] = new(255, 0, 0) });
        string result = PixelArtRenderer.ToAnsiString(pixels);
        Assert.NotEmpty(result);
    }

    // ── SceneLoader — destructive inputs ──────────────────────────────────────

    [Fact]
    public void SceneLoader_TryLoad_CorruptedJson_ReturnsFalse()
    {
        string path = TempFile("bad.scene.json", "{ not valid json [[[");
        bool ok = SceneLoader.TryLoad(path, out var scene);
        Assert.False(ok);
        Assert.Null(scene);
    }

    [Fact]
    public void SceneLoader_TryLoad_BinaryFile_ReturnsFalse()
    {
        string path = TempBinary("binary.scene.json", [0x00, 0xFF, 0xFE, 0xAB, 0xCD]);
        bool ok = SceneLoader.TryLoad(path, out _);
        Assert.False(ok);
    }

    [Fact]
    public void SceneLoader_Load_1000Lines_LoadsSuccessfully()
    {
        var lines = Enumerable.Range(0, 1000).Select(i => $"Line {i} content here.");
        string json = $$"""
            {
              "title": "Big Scene",
              "lines": [{{string.Join(", ", lines.Select(l => $"\"{l}\""))}}],
              "promptContinue": false
            }
            """;
        string path = TempFile("big.scene.json", json);
        var scene = SceneLoader.Load(path);
        Assert.Equal(1000, scene.Lines.Length);
    }

    [Fact]
    public void SceneLoader_Load_MaxLengthTitle_LoadsSuccessfully()
    {
        string bigTitle = new('X', 5000);
        string json = $$"""{"title":"{{bigTitle}}","lines":[],"promptContinue":false}""";
        string path = TempFile("bigtitle.scene.json", json);
        var scene = SceneLoader.Load(path);
        Assert.Equal(5000, scene.Title!.Length);
    }

    [Fact]
    public void SceneLoader_TryLoad_MissingFile_ReturnsFalse()
    {
        bool ok = SceneLoader.TryLoad(Path.Combine(_tempDir, "ghost.scene.json"), out _);
        Assert.False(ok);
    }

    // ── AnimationTimelineLoader — destructive inputs ───────────────────────────

    [Fact]
    public void AnimationTimelineLoader_TryLoad_BinaryFile_ReturnsFalse()
    {
        string path = TempBinary("binary.anim.json", [0x00, 0xFF, 0xFE]);
        bool ok = AnimationTimelineLoader.TryLoad(path, out _);
        Assert.False(ok);
    }

    [Fact]
    public void AnimationTimelineLoader_Load_1000Frames_LoadsSuccessfully()
    {
        var frames = Enumerable.Range(0, 1000).Select(_ => """{"durationMs":1}""");
        string json = $$"""{"name":"huge","frames":[{{string.Join(",", frames)}}]}""";
        string path = TempFile("huge.anim.json", json);
        var tl = AnimationTimelineLoader.Load(path);
        Assert.Equal(1000, tl.Frames.Count);
        Assert.Equal(1000, tl.TotalDurationMs);
    }

    // ── FlagStore — extreme sizes, wrong types, corrupt JSON ─────────────────

    [Fact]
    public void FlagStore_Set_10000Flags_ThenToJson_DoesNotCrash()
    {
        var store = new FlagStore();
        for (int i = 0; i < 10_000; i++)
            store.Set($"flag_{i}", i % 3 == 0 ? (object)i : i % 3 == 1 ? (object)$"val_{i}" : (object)(i % 2 == 0));

        string json = store.ToJson();
        Assert.NotEmpty(json);
        Assert.Equal(10_000, store.Count);
    }

    [Fact]
    public void FlagStore_FromJson_ToJson_RoundTrip_1000Flags()
    {
        var store = new FlagStore();
        for (int i = 0; i < 1000; i++) store.Set($"k{i}", i);

        string json    = store.ToJson();
        var restored   = FlagStore.FromJson(json);

        Assert.Equal(1000, restored.Count);
        for (int i = 0; i < 1000; i++)
            Assert.Equal(i, restored.Get<int>($"k{i}"));
    }

    [Fact]
    public void FlagStore_Get_WrongType_ReturnsDefault()
    {
        var store = new FlagStore();
        store.Set("key", "a string value");
        int result = store.Get<int>("key");
        Assert.Equal(0, result); // default(int)
    }

    [Fact]
    public void FlagStore_FromJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FlagStore.FromJson(null!));
    }

    [Fact]
    public void FlagStore_FromJson_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => FlagStore.FromJson(""));
    }

    [Fact]
    public void FlagStore_FromJson_MalformedJson_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => FlagStore.FromJson("{ bad json ]]]"));
    }

    [Fact]
    public void FlagStore_Set_EmptyKey_ThrowsArgumentException()
    {
        var store = new FlagStore();
        Assert.Throws<ArgumentException>(() => store.Set("", 42));
    }

    // ── MarkdownLocalizationLoader — extreme inputs ────────────────────────────

    [Fact]
    public void MarkdownLocalizationLoader_Parse_10000Entries_CompletesUnder10Seconds()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## en");
        sb.AppendLine("| Key | Value |");
        sb.AppendLine("|-----|-------|");
        for (int i = 0; i < 10_000; i++)
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"| locale.key.{i} | Value {i} |");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var table = MarkdownLocalizationLoader.Parse("en", sb.ToString());
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10),
            $"Parsing 10000 entries took {sw.Elapsed.TotalSeconds:F2}s — too slow");
        Assert.Equal(10_000, table.Count);
    }

    [Fact]
    public void MarkdownLocalizationLoader_Parse_EmptyContent_ReturnsEmptyTable()
    {
        var table = MarkdownLocalizationLoader.Parse("en", "");
        Assert.Equal(0, table.Count);
    }

    [Fact]
    public void MarkdownLocalizationLoader_Parse_NullContent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MarkdownLocalizationLoader.Parse("en", null!));
    }

    [Fact]
    public void MarkdownLocalizationLoader_Parse_EmptyLanguage_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MarkdownLocalizationLoader.Parse("", "| Key | Value |\n|---|---|\n| a | b |"));
    }

    [Fact]
    public void MarkdownLocalizationLoader_Parse_RowWith100Columns_DoesNotCrash()
    {
        string manyPipes = "| key | val |" + string.Concat(Enumerable.Repeat("| extra |", 98));
        string content   = "## en\n" + "| Key | Value |\n|---|---|\n" + manyPipes;
        var table = MarkdownLocalizationLoader.Parse("en", content);
        Assert.True(table.Count >= 0); // just verify it doesn't crash
    }

    // ── SaveRepository — missing/corrupt/large ────────────────────────────────

    [Fact]
    public void SaveRepository_LoadMostRecent_EmptyDirectory_ThrowsInvalidOperation()
    {
        string dir = Path.Combine(_tempDir, "empty_saves");
        Directory.CreateDirectory(dir);
        var repo = new SaveRepository<SimpleState>(dir, s => s.Id);
        Assert.Throws<InvalidOperationException>(() => repo.LoadMostRecent());
    }

    [Fact]
    public void SaveRepository_Load_CorruptedSlot_ThrowsJsonException()
    {
        string dir = Path.Combine(_tempDir, "corrupt_saves");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "bad_slot.json"), "{ not valid json");
        var repo = new SaveRepository<SimpleState>(dir, s => s.Id);
        Assert.ThrowsAny<Exception>(() => repo.Load("bad_slot"));
    }

    [Fact]
    public void SaveRepository_Save_VeryLargeState_CompletesUnder2Seconds()
    {
        string dir   = Path.Combine(_tempDir, "large_saves");
        var repo     = new SaveRepository<LargeState>(dir, s => s.Id);
        var bigState = new LargeState
        {
            Id   = "large",
            Data = new string('X', 1_000_000), // 1 MB payload
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        repo.Save(bigState);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2),
            $"Saving 1MB state took {sw.Elapsed.TotalSeconds:F2}s");
        var loaded = repo.LoadMostRecent();
        Assert.Equal(1_000_000, loaded.Data.Length);
    }

    // ── WorldMap — large graphs, duplicates ──────────────────────────────────

    [Fact]
    public void WorldMap_1000Locations_BuildsAndQueriesCorrectly()
    {
        var locations = Enumerable.Range(0, 1000).Select(i =>
            new LocationDefinition
            {
                Id    = $"loc_{i}",
                Name  = $"Location {i}",
                Exits = i < 999
                    ? new Dictionary<string, string> { ["next"] = $"loc_{i + 1}" }
                    : new Dictionary<string, string>(),
            });

        var map = new WorldMap(locations);
        Assert.Equal(1000, map.AllLocations.Count);

        bool moved = map.TryMove("loc_0", "next", out var dest);
        Assert.True(moved);
        Assert.Equal("loc_1", dest!.Id);
    }

    [Fact]
    public void WorldMap_DuplicateIds_ThrowsArgumentException()
    {
        var locations = new[]
        {
            new LocationDefinition { Id = "town", Name = "Town" },
            new LocationDefinition { Id = "TOWN", Name = "Also Town" }, // case-insensitive duplicate
        };
        Assert.Throws<ArgumentException>(() => new WorldMap(locations));
    }

    [Fact]
    public void WorldMap_NullLocations_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new WorldMap(null!));
    }

    // ── Concurrency — TerminalAnimationEngine ─────────────────────────────────

    [Fact]
    public async Task TerminalAnimationEngine_ConcurrentPlayAndCancelAll_NoException()
    {
        var engine = new TerminalAnimationEngine();

        // Register distinct IDs to avoid the "same ID concurrent overwrite" scenario
        for (int i = 0; i < 5; i++)
        {
            engine.Register($"anim_{i}", new AnimationTimeline
            {
                Name   = $"anim_{i}",
                Loop   = true,
                Frames = [new Keyframe { DurationMs = 1 }],
            });
        }

        // Start all 5 looping animations
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Task.Run(() => engine.PlayAsync($"anim_{i}")))
            .ToList();

        await Task.Delay(50); // let them all start
        engine.CancelAll();   // cancel all — must not throw or deadlock

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task TerminalAnimationEngine_SameId_ConcurrentPlay_NoCorruption()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("shared", new AnimationTimeline
        {
            Name   = "shared",
            Loop   = true,
            Frames = [new Keyframe { DurationMs = 1 }],
        });

        // Two tasks play the same ID — the lock prevents dictionary corruption
        var t1 = Task.Run(() => engine.PlayAsync("shared"));
        var t2 = Task.Run(() => engine.PlayAsync("shared"));

        await Task.Delay(30);
        engine.Cancel("shared");

        await Task.WhenAll(t1, t2).WaitAsync(TimeSpan.FromSeconds(10));
    }

    // ── Memory: repeated loading should not grow unboundedly ─────────────────

    [Fact]
    public void SceneLoader_Load100Times_MemoryDoesNotGrowUnboundedly()
    {
        string json = """{"title":"T","lines":["line1"],"promptContinue":false}""";
        string path = TempFile("repeat.scene.json", json);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        long before = GC.GetTotalMemory(true);

        for (int i = 0; i < 100; i++)
            SceneLoader.Load(path);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        long after = GC.GetTotalMemory(true);

        long growthBytes = after - before;
        Assert.True(growthBytes < 50_000_000, // 50 MB tolerance
            $"Memory grew by {growthBytes / 1024 / 1024} MB after 100 loads — possible leak");
    }

    [Fact]
    public void AnimationTimelineLoader_Load100Times_MemoryDoesNotGrowUnboundedly()
    {
        string json = """{"name":"tl","frames":[{"durationMs":1}]}""";
        string path = TempFile("repeat.anim.json", json);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        long before = GC.GetTotalMemory(true);

        for (int i = 0; i < 100; i++)
            AnimationTimelineLoader.Load(path);

        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        long after = GC.GetTotalMemory(true);

        long growthBytes = after - before;
        Assert.True(growthBytes < 50_000_000,
            $"Memory grew by {growthBytes / 1024 / 1024} MB after 100 loads — possible leak");
    }

    // ── Helper types ─────────────────────────────────────────────────────────

    private sealed class SimpleState { public string Id { get; set; } = "slot"; }
    private sealed class LargeState  { public string Id { get; set; } = "slot"; public string Data { get; set; } = ""; }
}
