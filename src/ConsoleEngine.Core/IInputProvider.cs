namespace ConsoleEngine.Core;

/// <summary>
/// Abstraction for reading keyboard input. Decouples game logic from
/// <see cref="Console.ReadKey()"/> so modules can be tested without a real keyboard.
/// </summary>
/// <remarks>
/// Implementations:
/// <list type="bullet">
///   <item><c>ConsoleInputProvider</c> — wraps <c>Console.ReadKey()</c> (runtime).</item>
///   <item><c>MockInputProvider</c> — feeds a pre-defined key sequence (unit tests).</item>
/// </list>
/// Inject via constructor. For backwards compatibility, modules default to
/// <c>ConsoleInputProvider.Default</c> when no override is supplied.
/// </remarks>
public interface IInputProvider
{
    /// <summary>
    /// Blocks until a key is pressed and returns it.
    /// </summary>
    /// <param name="intercept">
    /// When <see langword="true"/> (default), the pressed key is not echoed to the terminal.
    /// </param>
    ConsoleKeyInfo ReadKey(bool intercept = true);

    /// <summary>
    /// <see langword="true"/> if at least one key press is available to be read
    /// without blocking.
    /// </summary>
    bool KeyAvailable { get; }

    /// <summary>
    /// Blocks until the user presses Enter and returns the typed text.
    /// Returns <see langword="null"/> if the stream ends (e.g. redirected stdin).
    /// </summary>
    string? ReadLine();
}
