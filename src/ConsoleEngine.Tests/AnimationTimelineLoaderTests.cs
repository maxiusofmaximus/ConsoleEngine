using ConsoleEngine.Animation;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class AnimationTimelineLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public AnimationTimelineLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ce_anim_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string Write(string fileName, string content)
    {
        string path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Load_ValidJson_ReturnsTimeline()
    {
        string path = Write("anim.anim.json", """
            {
              "name": "bounce",
              "loop": false,
              "frames": [
                { "durationMs": 200, "color": "Red" },
                { "durationMs": 100 }
              ]
            }
            """);

        var tl = AnimationTimelineLoader.Load(path);

        Assert.Equal("bounce", tl.Name);
        Assert.False(tl.Loop);
        Assert.Equal(2, tl.Frames.Count);
        Assert.Equal(200, tl.Frames[0].DurationMs);
        Assert.Equal(ConsoleColor.Red, tl.Frames[0].Color);
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            AnimationTimelineLoader.Load(Path.Combine(_tempDir, "missing.anim.json")));
    }

    [Fact]
    public void Load_InvalidJson_Throws()
    {
        string path = Write("bad.anim.json", "{ bad json [[[");
        Assert.ThrowsAny<Exception>(() => AnimationTimelineLoader.Load(path));
    }

    [Fact]
    public void Load_EmptyFramesArray_ReturnsTimelineWithNoFrames()
    {
        string path = Write("empty.anim.json", """{"name":"x","frames":[]}""");
        var tl = AnimationTimelineLoader.Load(path);
        Assert.Empty(tl.Frames);
    }

    [Fact]
    public void Load_LoopFlagTrue_ReturnsLoopingTimeline()
    {
        string path = Write("loop.anim.json", """{"name":"l","loop":true,"frames":[]}""");
        var tl = AnimationTimelineLoader.Load(path);
        Assert.True(tl.Loop);
    }

    [Fact]
    public void Load_LoopFlagAbsent_DefaultsFalse()
    {
        string path = Write("noloop.anim.json", """{"name":"nl","frames":[]}""");
        var tl = AnimationTimelineLoader.Load(path);
        Assert.False(tl.Loop);
    }

    [Fact]
    public void Load_FrameDefaultDurationMs_Is100()
    {
        string path = Write("def.anim.json", """{"name":"d","frames":[{}]}""");
        var tl = AnimationTimelineLoader.Load(path);
        Assert.Equal(100, tl.Frames[0].DurationMs);
    }

    [Fact]
    public void Load_FrameColor_ParsedFromString()
    {
        string path = Write("col.anim.json", """{"name":"c","frames":[{"color":"Cyan"}]}""");
        var tl = AnimationTimelineLoader.Load(path);
        Assert.Equal(ConsoleColor.Cyan, tl.Frames[0].Color);
    }

    [Fact]
    public void Load_FrameAsciiArt_ParsedCorrectly()
    {
        string path = Write("art.anim.json", """{"name":"a","frames":[{"asciiArt":["line1","line2"]}]}""");
        var tl = AnimationTimelineLoader.Load(path);
        Assert.Equal(2, tl.Frames[0].AsciiArt!.Length);
        Assert.Equal("line1", tl.Frames[0].AsciiArt![0]);
    }

    [Fact]
    public void Load_FramePositionAndClearBefore_ParsedCorrectly()
    {
        string path = Write("pos.anim.json",
            """{"name":"p","frames":[{"col":5,"row":10,"clearBefore":true}]}""");
        var tl = AnimationTimelineLoader.Load(path);
        Assert.Equal(5, tl.Frames[0].Col);
        Assert.Equal(10, tl.Frames[0].Row);
        Assert.True(tl.Frames[0].ClearBefore);
    }

    // ── TryLoad ───────────────────────────────────────────────────────────────

    [Fact]
    public void TryLoad_ValidJson_ReturnsTrueAndTimeline()
    {
        string path = Write("valid.anim.json", """{"name":"v","frames":[]}""");
        bool ok = AnimationTimelineLoader.TryLoad(path, out var tl);
        Assert.True(ok);
        Assert.NotNull(tl);
        Assert.Equal("v", tl!.Name);
    }

    [Fact]
    public void TryLoad_MissingFile_ReturnsFalseAndNull()
    {
        bool ok = AnimationTimelineLoader.TryLoad(
            Path.Combine(_tempDir, "ghost.anim.json"), out var tl);
        Assert.False(ok);
        Assert.Null(tl);
    }

    [Fact]
    public void TryLoad_InvalidJson_ReturnsFalseAndNull()
    {
        string path = Write("inv.anim.json", "not json");
        bool ok = AnimationTimelineLoader.TryLoad(path, out var tl);
        Assert.False(ok);
        Assert.Null(tl);
    }
}
