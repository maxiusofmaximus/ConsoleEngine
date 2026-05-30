using ConsoleEngine.Animation;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class AnimationPlayerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    // No-art timeline: only awaits Task.Delay — safe in headless CI
    private static AnimationTimeline NoArtTimeline(int frames, int durationMs = 1, bool loop = false) =>
        new()
        {
            Name   = "test",
            Loop   = loop,
            Frames = Enumerable.Range(0, frames)
                               .Select(_ => new Keyframe { DurationMs = durationMs })
                               .ToList(),
        };

    // ── PlayAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_EmptyFrames_CompletesImmediately()
    {
        var timeline = NoArtTimeline(0);
        // Should return without hanging
        await AnimationPlayer.PlayAsync(timeline).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PlayAsync_SingleFrameNoArt_CompletesAfterDelay()
    {
        var timeline = NoArtTimeline(1, durationMs: 1);
        await AnimationPlayer.PlayAsync(timeline).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PlayAsync_MultiFrame_CompletesInTime()
    {
        var timeline = NoArtTimeline(5, durationMs: 1);
        await AnimationPlayer.PlayAsync(timeline).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PlayAsync_CancellationToken_StopsLoop()
    {
        var timeline = NoArtTimeline(1, durationMs: 1, loop: true);
        using var cts = new CancellationTokenSource(millisecondsDelay: 50);

        // Should complete when CTS fires, not run forever
        await AnimationPlayer.PlayAsync(timeline, cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PlayAsync_PreCancelledToken_ReturnsImmediately()
    {
        var timeline = NoArtTimeline(5, durationMs: 10, loop: false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Pre-cancelled token: should return without executing any frames
        await AnimationPlayer.PlayAsync(timeline, cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task PlayAsync_NonLooping_DoesNotRepeat()
    {
        // If the animation were looping, it would never complete without cancellation.
        // Non-looping: must complete on its own.
        var timeline = NoArtTimeline(3, durationMs: 1, loop: false);
        await AnimationPlayer.PlayAsync(timeline).WaitAsync(TimeSpan.FromSeconds(5));
    }

    // ── Play (sync) ───────────────────────────────────────────────────────────

    [Fact]
    public void Play_SyncWrapper_CompletesForEmptyTimeline()
    {
        var timeline = NoArtTimeline(0);
        AnimationPlayer.Play(timeline); // must not block indefinitely
    }

    [Fact]
    public void Play_SyncWrapper_CompletesForSingleFrame()
    {
        var timeline = NoArtTimeline(1, durationMs: 1);
        AnimationPlayer.Play(timeline);
    }
}
