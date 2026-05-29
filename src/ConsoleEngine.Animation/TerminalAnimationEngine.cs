using ConsoleEngine.Core;

namespace ConsoleEngine.Animation;

/// <summary>
/// <see cref="IAnimationEngine"/> implementation for terminal rendering.
/// Maintains a registry of named <see cref="AnimationTimeline"/> instances and
/// supports concurrent playback with per-animation cancellation.
/// </summary>
/// <example>
/// <code>
/// var engine = new TerminalAnimationEngine();
/// engine.Register("hit", AnimationTimelineLoader.Load("hit.anim.json"));
/// engine.Register("idle", idleTimeline);
///
/// await engine.PlayAsync("hit");
/// engine.StopAll();
/// </code>
/// </example>
public sealed class TerminalAnimationEngine : IAnimationEngine
{
    private readonly Dictionary<string, AnimationTimeline>        _timelines = new();
    private readonly Dictionary<string, CancellationTokenSource>  _running   = new();

    /// <summary>
    /// Registers a timeline under <paramref name="id"/>.
    /// Overwrites any previously registered timeline with the same ID.
    /// </summary>
    public void Register(string id, AnimationTimeline timeline)
        => _timelines[id] = timeline;

    /// <inheritdoc/>
    public async Task PlayAsync(string animationId, CancellationToken ct = default)
    {
        if (!_timelines.TryGetValue(animationId, out var timeline)) return;

        Cancel(animationId);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _running[animationId] = cts;

        try
        {
            await AnimationPlayer.PlayAsync(timeline, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            _running.Remove(animationId);
            cts.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool IsPlaying(string animationId) => _running.ContainsKey(animationId);

    /// <inheritdoc/>
    public void Cancel(string animationId)
    {
        if (_running.Remove(animationId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <inheritdoc/>
    public void CancelAll()
    {
        foreach (string id in _running.Keys.ToList())
            Cancel(id);
    }
}
