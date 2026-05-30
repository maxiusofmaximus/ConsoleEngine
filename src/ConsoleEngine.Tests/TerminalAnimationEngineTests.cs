using ConsoleEngine.Animation;
using ConsoleEngine.Core;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class TerminalAnimationEngineTests
{
    private static AnimationTimeline NoArtTimeline(bool loop = false, int durationMs = 1) =>
        new()
        {
            Name   = "test",
            Loop   = loop,
            Frames = [new Keyframe { DurationMs = durationMs }],
        };

    private static AnimationTimeline EmptyTimeline(bool loop = false) =>
        new() { Name = "empty", Loop = loop, Frames = [] };

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public void Register_NewId_DoesNotThrow()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("hit", NoArtTimeline());
    }

    [Fact]
    public void Register_OverwritesSameId_DoesNotThrow()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("hit", NoArtTimeline());
        engine.Register("hit", EmptyTimeline()); // overwrite — no exception
    }

    // ── IsPlaying ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsPlaying_BeforePlay_ReturnsFalse()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("hit", NoArtTimeline());
        Assert.False(engine.IsPlaying("hit"));
    }

    [Fact]
    public void IsPlaying_UnregisteredId_ReturnsFalse()
    {
        var engine = new TerminalAnimationEngine();
        Assert.False(engine.IsPlaying("nonexistent"));
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_UnregisteredId_DoesNotThrow()
    {
        var engine = new TerminalAnimationEngine();
        engine.Cancel("ghost"); // must not throw
    }

    [Fact]
    public void Cancel_IdThatIsNotPlaying_DoesNotThrow()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("idle", NoArtTimeline());
        engine.Cancel("idle"); // not playing yet — safe
    }

    // ── CancelAll ─────────────────────────────────────────────────────────────

    [Fact]
    public void CancelAll_WithNoRunning_DoesNotThrow()
    {
        var engine = new TerminalAnimationEngine();
        engine.CancelAll();
    }

    // ── PlayAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_UnregisteredId_ReturnsImmediately()
    {
        var engine = new TerminalAnimationEngine();
        // Must return without exception even for unknown ID
        await engine.PlayAsync("ghost").WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task PlayAsync_EmptyTimeline_CompletesImmediately()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("empty", EmptyTimeline());
        await engine.PlayAsync("empty").WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task PlayAsync_ThenCancel_StopsAnimation()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("loop", NoArtTimeline(loop: true, durationMs: 1));

        // Fire and forget — starts the loop
        var task = Task.Run(() => engine.PlayAsync("loop"));

        await Task.Delay(20); // let it start
        engine.Cancel("loop");

        // Task must complete after cancel, not hang
        await task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CancelAll_StopsAllRunning()
    {
        var engine = new TerminalAnimationEngine();
        engine.Register("a", NoArtTimeline(loop: true, durationMs: 1));
        engine.Register("b", NoArtTimeline(loop: true, durationMs: 1));

        var taskA = Task.Run(() => engine.PlayAsync("a"));
        var taskB = Task.Run(() => engine.PlayAsync("b"));

        await Task.Delay(20); // let both start
        engine.CancelAll();

        await Task.WhenAll(taskA, taskB).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ImplementsIAnimationEngine()
    {
        Assert.IsAssignableFrom<IAnimationEngine>(new TerminalAnimationEngine());
    }
}
