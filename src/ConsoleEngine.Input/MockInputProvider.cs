using ConsoleEngine.Core;

namespace ConsoleEngine.Input;

/// <summary>
/// Test-only implementation of <see cref="IInputProvider"/> that serves a pre-defined
/// sequence of key presses without blocking or requiring a real keyboard.
/// </summary>
/// <remarks>
/// <b>Usage in tests</b>
/// <code>
/// var input = new MockInputProvider();
/// input.Enqueue(ConsoleKey.Enter);
/// input.Enqueue(ConsoleKey.Escape);
///
/// var player = new ScenePlayer(input);
/// player.Play(scene); // Enter advances the scene; Escape could quit
/// </code>
/// </remarks>
public sealed class MockInputProvider : IInputProvider
{
    private readonly Queue<ConsoleKeyInfo> _keys = new();

    /// <summary>
    /// Enqueues a key press with no modifier keys.
    /// </summary>
    public void Enqueue(ConsoleKey key, char keyChar = '\0')
        => _keys.Enqueue(new ConsoleKeyInfo(keyChar, key, shift: false, alt: false, control: false));

    /// <summary>Enqueues a fully specified <see cref="ConsoleKeyInfo"/>.</summary>
    public void Enqueue(ConsoleKeyInfo key) => _keys.Enqueue(key);

    /// <summary>Clears all pending keys.</summary>
    public void Clear() => _keys.Clear();

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="ReadKey"/> is called but the queue is empty.
    /// This indicates a test that did not enqueue enough keys — an assertion failure, not a crash.
    /// </exception>
    public ConsoleKeyInfo ReadKey(bool intercept = true)
    {
        if (_keys.Count == 0)
            throw new InvalidOperationException(
                "MockInputProvider has no more keys. " +
                "Call Enqueue() before ReadKey() in your test setup.");

        return _keys.Dequeue();
    }

    /// <inheritdoc/>
    public bool KeyAvailable => _keys.Count > 0;

    /// <inheritdoc/>
    /// <remarks>In mock mode, returns an empty string (simulates pressing Enter with no text).</remarks>
    public string? ReadLine() => string.Empty;

    /// <summary>Number of keys remaining in the queue.</summary>
    public int Remaining => _keys.Count;
}
