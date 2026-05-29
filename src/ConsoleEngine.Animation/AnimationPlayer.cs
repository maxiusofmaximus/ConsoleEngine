using ConsoleEngine.Rendering;

namespace ConsoleEngine.Animation;

/// <summary>
/// Executes an <see cref="AnimationTimeline"/> against the terminal.
/// Each frame draws its art/sprite, then waits <see cref="Keyframe.DurationMs"/> milliseconds.
/// </summary>
public static class AnimationPlayer
{
    /// <summary>
    /// Plays <paramref name="timeline"/> asynchronously.
    /// Respects <see cref="AnimationTimeline.Loop"/>; cancels cleanly when
    /// <paramref name="ct"/> is triggered.
    /// </summary>
    public static async Task PlayAsync(AnimationTimeline timeline, CancellationToken ct = default)
    {
        do
        {
            foreach (var frame in timeline.Frames)
            {
                if (ct.IsCancellationRequested) return;

                if (frame.ClearBefore)
                    try { Console.Clear(); } catch { }

                if (frame.AsciiArt is { Length: > 0 } art)
                    AnimationEngine.DrawSprite(art, frame.Col, frame.Row, frame.Color);

                if (!string.IsNullOrEmpty(frame.SpritePath))
                    PixelArtRenderer.RenderPng(frame.SpritePath, frame.Col, frame.Row);

                try { await Task.Delay(frame.DurationMs, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
        } while (timeline.Loop && !ct.IsCancellationRequested);
    }

    /// <summary>
    /// Synchronous wrapper around <see cref="PlayAsync"/>.
    /// Blocks the calling thread until the animation completes or is cancelled.
    /// </summary>
    public static void Play(AnimationTimeline timeline, CancellationToken ct = default)
        => PlayAsync(timeline, ct).GetAwaiter().GetResult();
}
