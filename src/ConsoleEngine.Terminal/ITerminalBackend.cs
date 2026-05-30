namespace ConsoleEngine.Terminal;

/// <summary>
/// A live pseudo-terminal (PTY) session hosting a child process.
/// Abstracts the platform mechanism (ConPTY on Windows, <c>posix_spawn</c> + PTY on
/// Linux/macOS) so consumers never touch native APIs directly.
/// </summary>
/// <remarks>
/// Output is delivered as raw bytes (UTF-8 text interleaved with ANSI/VT escape
/// sequences) — decoding and terminal emulation are the frontend's job, not the backend's.
/// </remarks>
public interface ITerminalBackend : IAsyncDisposable
{
    /// <summary>
    /// Raised on a background thread with a chunk of raw output from the child process.
    /// The memory is only valid for the duration of the callback — copy if retaining.
    /// </summary>
    event Action<ReadOnlyMemory<byte>>? Output;

    /// <summary>Raised once when the child process exits. Argument is the exit code.</summary>
    event Action<int>? Exited;

    /// <summary>Writes raw bytes to the child's standard input (e.g. keystrokes).</summary>
    void Write(ReadOnlySpan<byte> data);

    /// <summary>
    /// Resizes the pseudo-terminal window. Triggers <c>ResizePseudoConsole</c> on Windows
    /// or <c>ioctl(TIOCSWINSZ)</c> + SIGWINCH on POSIX, so the child re-lays out its TUI.
    /// </summary>
    void Resize(int cols, int rows);

    /// <summary><see langword="true"/> while the child process is alive.</summary>
    bool IsRunning { get; }

    /// <summary>The child's exit code. Only meaningful once <see cref="IsRunning"/> is <see langword="false"/>.</summary>
    int ExitCode { get; }
}
