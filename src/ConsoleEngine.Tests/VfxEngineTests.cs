using ConsoleEngine.Animation;
using Xunit;

namespace ConsoleEngine.Tests;

/// <summary>
/// VfxEngine tests run headless: AnimationEngine.DrawAt has an internal try/catch,
/// so Console I/O failures are silently swallowed. All tests verify the async methods
/// complete without exception (including with edge-case inputs).
/// </summary>
public sealed class VfxEngineTests
{
    // ── FlashAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FlashAsync_CompletesWithoutException()
    {
        await VfxEngine.FlashAsync(0, 0, 5, 2, ConsoleColor.Red, durationMs: 1);
    }

    [Fact]
    public async Task FlashAsync_ZeroSize_CompletesWithoutException()
    {
        await VfxEngine.FlashAsync(0, 0, 0, 0, ConsoleColor.Red, durationMs: 1);
    }

    [Fact]
    public async Task FlashAsync_PreCancelledToken_CompletesWithoutException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await VfxEngine.FlashAsync(0, 0, 5, 2, ConsoleColor.Green, durationMs: 1, cts.Token);
    }

    // ── FullScreenFlashAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task FullScreenFlashAsync_CompletesWithoutException()
    {
        await VfxEngine.FullScreenFlashAsync(ConsoleColor.White, durationMs: 1);
    }

    [Fact]
    public async Task FullScreenFlashAsync_PreCancelledToken_CompletesWithoutException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await VfxEngine.FullScreenFlashAsync(ConsoleColor.Blue, durationMs: 100, cts.Token);
    }

    // ── ScreenShakeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task ScreenShakeAsync_CompletesWithoutException()
    {
        await VfxEngine.ScreenShakeAsync(intensity: 1, durationMs: 5, stepsMs: 1);
    }

    [Fact]
    public async Task ScreenShakeAsync_ZeroIntensity_CompletesWithoutException()
    {
        await VfxEngine.ScreenShakeAsync(intensity: 0, durationMs: 5, stepsMs: 1);
    }

    [Fact]
    public async Task ScreenShakeAsync_PreCancelledToken_CompletesWithoutException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await VfxEngine.ScreenShakeAsync(intensity: 2, durationMs: 1000, stepsMs: 10, cts.Token);
    }

    // ── ParticleBurstAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ParticleBurstAsync_EmptyChars_ReturnsImmediately()
    {
        await VfxEngine.ParticleBurstAsync(10, 5, string.Empty, ConsoleColor.Yellow, count: 10, lifetimeMs: 1);
    }

    [Fact]
    public async Task ParticleBurstAsync_ValidChars_CompletesWithoutException()
    {
        await VfxEngine.ParticleBurstAsync(10, 5, "*+•·", ConsoleColor.Cyan, count: 20, lifetimeMs: 1);
    }

    [Fact]
    public async Task ParticleBurstAsync_ZeroCount_CompletesWithoutException()
    {
        await VfxEngine.ParticleBurstAsync(10, 5, "*", ConsoleColor.White, count: 0, lifetimeMs: 1);
    }

    [Fact]
    public async Task ParticleBurstAsync_LargeCount_CompletesWithoutException()
    {
        await VfxEngine.ParticleBurstAsync(10, 5, "*", ConsoleColor.Red, count: 500, lifetimeMs: 1);
    }

    [Fact]
    public async Task ParticleBurstAsync_PreCancelledToken_CompletesWithoutException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await VfxEngine.ParticleBurstAsync(10, 5, "*", ConsoleColor.Gray, count: 10, lifetimeMs: 5000, ct: cts.Token);
    }

    // ── DissolveAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DissolveAsync_CompletesWithoutException()
    {
        await VfxEngine.DissolveAsync(0, 0, width: 5, height: 2, steps: 3, stepMs: 1);
    }

    [Fact]
    public async Task DissolveAsync_ZeroArea_CompletesWithoutException()
    {
        await VfxEngine.DissolveAsync(0, 0, width: 0, height: 0, steps: 3, stepMs: 1);
    }

    [Fact]
    public async Task DissolveAsync_PreCancelledToken_CompletesWithoutException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await VfxEngine.DissolveAsync(0, 0, width: 10, height: 5, steps: 10, stepMs: 100, ct: cts.Token);
    }
}
