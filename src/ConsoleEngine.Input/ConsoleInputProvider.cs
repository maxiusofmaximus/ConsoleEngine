using ConsoleEngine.Core;

namespace ConsoleEngine.Input;

/// <summary>
/// Production implementation of <see cref="IInputProvider"/> that wraps
/// <see cref="Console.ReadKey()"/> directly.
/// </summary>
public sealed class ConsoleInputProvider : IInputProvider
{
    /// <summary>Shared singleton instance for the common case.</summary>
    public static readonly ConsoleInputProvider Default = new();

    private ConsoleInputProvider() { }

    /// <inheritdoc/>
    public ConsoleKeyInfo ReadKey(bool intercept = true) => Console.ReadKey(intercept);

    /// <inheritdoc/>
    public bool KeyAvailable => Console.KeyAvailable;

    /// <inheritdoc/>
    public string? ReadLine() => Console.ReadLine();
}
