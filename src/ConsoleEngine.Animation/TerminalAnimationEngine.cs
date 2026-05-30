using ConsoleEngine.Core;

namespace ConsoleEngine.Animation;

/// <summary>
/// <see cref="IAnimationEngine"/> implementation for terminal rendering.
/// Maintains a registry of named <see cref="AnimationTimeline"/> instances and
/// supports concurrent playback with per-animation cancellation.
/// </summary>
/// <remarks>
/// Thread-safe: all dictionary access is synchronised with a lock.
/// The await inside <see cref="PlayAsync"/> runs outside the lock to avoid
/// holding it across async suspension points.
/// </remarks>
public sealed class TerminalAnimationEngine : IAnimationEngine
{
    private readonly object _lock = new();
    private readonly Dictionary<string, AnimationTimeline>       _timelines = new();
    private readonly Dictionary<string, CancellationTokenSource> _running   = new();

    /// <summary>
    /// Registers a timeline under <paramref name="id"/>.
    /// Overwrites any previously registered timeline with the same ID.
    /// </summary>
    public void Register(string id, AnimationTimeline timeline)
    {
        lock (_lock) { _timelines[id] = timeline; }
    }

    /// <inheritdoc/>
    public async Task PlayAsync(string animationId, CancellationToken ct = default)
    {
        AnimationTimeline? timeline;
        CancellationTokenSource cts;

        lock (_lock)
        {
            if (!_timelines.TryGetValue(animationId, out timeline)) return;

            // Cancel any existing run for this ID before starting a new one
            CancelCore(animationId);

            cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _running[animationId] = cts;
        }

        try
        {
            await AnimationPlayer.PlayAsync(timeline, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_lock)
            {
                // Only remove if it's still our CTS (another PlayAsync may have replaced it)
                if (_running.TryGetValue(animationId, out var current) && ReferenceEquals(current, cts))
                    _running.Remove(animationId);
                cts.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public bool IsPlaying(string animationId)
    {
        lock (_lock) { return _running.ContainsKey(animationId); }
    }

    /// <inheritdoc/>
    public void Cancel(string animationId)
    {
        lock (_lock) { CancelCore(animationId); }
    }

    /// <inheritdoc/>
    public void CancelAll()
    {
        List<string> ids;
        lock (_lock) { ids = [.. _running.Keys]; }
        foreach (string id in ids) Cancel(id);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    // Must be called while holding _lock
    private void CancelCore(string animationId)
    {
        if (_running.Remove(animationId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
