using ConsoleEngine.Animation;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class AnimationTimelineTests
{
    [Fact]
    public void TotalDurationMs_EmptyFrames_ReturnsZero()
    {
        var timeline = new AnimationTimeline { Frames = [] };
        Assert.Equal(0, timeline.TotalDurationMs);
    }

    [Fact]
    public void TotalDurationMs_SingleFrame_ReturnsDuration()
    {
        var timeline = new AnimationTimeline
        {
            Frames = [new Keyframe { DurationMs = 250 }],
        };
        Assert.Equal(250, timeline.TotalDurationMs);
    }

    [Fact]
    public void TotalDurationMs_MultipleFrames_ReturnsSum()
    {
        var timeline = new AnimationTimeline
        {
            Frames =
            [
                new Keyframe { DurationMs = 100 },
                new Keyframe { DurationMs = 200 },
                new Keyframe { DurationMs = 300 },
            ],
        };
        Assert.Equal(600, timeline.TotalDurationMs);
    }

    [Fact]
    public void Frames_Default_CanBeIteratedWithoutNullRef()
    {
        var timeline = new AnimationTimeline();
        int count = 0;
        foreach (var _ in timeline.Frames) count++;
        Assert.Equal(0, count);
    }

    [Fact]
    public void Name_Default_IsEmptyString()
    {
        var timeline = new AnimationTimeline();
        Assert.Equal(string.Empty, timeline.Name);
    }

    [Fact]
    public void Loop_Default_IsFalse()
    {
        var timeline = new AnimationTimeline();
        Assert.False(timeline.Loop);
    }

    [Fact]
    public void Loop_SetTrue_ReturnsTrue()
    {
        var timeline = new AnimationTimeline { Loop = true };
        Assert.True(timeline.Loop);
    }

    [Fact]
    public void Keyframe_Defaults_AreCorrect()
    {
        var frame = new Keyframe();
        Assert.Equal(100, frame.DurationMs);
        Assert.Equal(ConsoleColor.White, frame.Color);
        Assert.Null(frame.AsciiArt);
        Assert.Null(frame.SpritePath);
        Assert.False(frame.ClearBefore);
        Assert.Equal(0, frame.Col);
        Assert.Equal(0, frame.Row);
    }

    [Fact]
    public void Keyframe_WithValues_InitialisesCorrectly()
    {
        var frame = new Keyframe
        {
            DurationMs  = 500,
            AsciiArt    = ["line1", "line2"],
            Color       = ConsoleColor.Red,
            Col         = 10,
            Row         = 5,
            ClearBefore = true,
            SpritePath  = "hero.png",
            Sound       = "hit.wav",
        };

        Assert.Equal(500, frame.DurationMs);
        Assert.Equal(2, frame.AsciiArt!.Length);
        Assert.Equal(ConsoleColor.Red, frame.Color);
        Assert.Equal(10, frame.Col);
        Assert.Equal(5, frame.Row);
        Assert.True(frame.ClearBefore);
        Assert.Equal("hero.png", frame.SpritePath);
        Assert.Equal("hit.wav", frame.Sound);
    }
}
