namespace ConsoleEngine.Terminal.Backends;

/// <summary>
/// Bridges a backend's read loop to its public <c>Output</c> event. Chunks produced before
/// the first subscriber attaches are buffered and replayed on subscription, closing the race
/// where a fast child (or a shell's initial prompt) emits output before the consumer can
/// subscribe. <see cref="Emit"/> is called from the single read thread; <see cref="Subscribe"/>
/// and <see cref="Unsubscribe"/> from any thread.
/// </summary>
internal sealed class BufferedOutput
{
    private readonly object _lock = new();
    private readonly List<byte[]> _pending = new();
    private Action<ReadOnlyMemory<byte>>? _handler;

    public void Subscribe(Action<ReadOnlyMemory<byte>> handler)
    {
        lock (_lock)
        {
            _handler += handler;
            if (_pending.Count == 0) return;
            foreach (byte[] chunk in _pending) handler(chunk);
            _pending.Clear();
        }
    }

    public void Unsubscribe(Action<ReadOnlyMemory<byte>> handler)
    {
        lock (_lock) _handler -= handler;
    }

    /// <summary>Delivers a chunk to the subscriber, or buffers it (copied) if none is attached yet.</summary>
    public void Emit(ReadOnlyMemory<byte> data)
    {
        lock (_lock)
        {
            if (_handler is null) { _pending.Add(data.ToArray()); return; }
            _handler(data);
        }
    }
}
