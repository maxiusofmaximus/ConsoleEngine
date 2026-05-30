using System.Diagnostics;
using System.Text;

namespace ConsoleEngine.Editor.Terminal;

/// <summary>
/// Manages a child AI process (claude, opencode, agy, …) with redirected I/O.
/// Raw output is delivered via <see cref="OutputReceived"/> — callers should
/// pass it through <see cref="AnsiStripper.Strip"/> before displaying.
/// </summary>
/// <remarks>
/// Phase A limitation: redirected I/O means the child process does not get a
/// real PTY. Full-TUI tools (like Claude Code's interactive mode) may render
/// incorrectly. Use the "Open in External Terminal" button for those cases.
/// Phase B will replace this with a ConPTY or WebView2 + xterm.js backend.
/// </remarks>
internal sealed class AiTerminalSession : IAsyncDisposable
{
    private readonly Process                 _process;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Fired on the thread pool with raw bytes from stdout or stderr.</summary>
    public event Action<string>? OutputReceived;

    public bool IsRunning => !(_process.HasExited);

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns <paramref name="command"/> (via <c>cmd.exe /c</c>) in <paramref name="workDir"/>.
    /// Starts background reader tasks immediately.
    /// </summary>
    public static AiTerminalSession Spawn(string command, string workDir)
    {
        var psi = new ProcessStartInfo("cmd.exe")
        {
            Arguments              = $"/c {command}",
            WorkingDirectory       = workDir,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding  = Encoding.UTF8,
        };

        // Hint that color output is welcome (honoured by some CLI tools)
        psi.Environment["TERM"]        = "xterm-256color";
        psi.Environment["FORCE_COLOR"] = "1";
        psi.Environment["NO_COLOR"]    = "";   // unset in case it was set globally

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: {command}");

        var session = new AiTerminalSession(process);
        session.BeginReading();
        return session;
    }

    private AiTerminalSession(Process process) => _process = process;

    // ── I/O ───────────────────────────────────────────────────────────────────

    private void BeginReading()
    {
        var ct = _cts.Token;
        _ = ReadStreamAsync(_process.StandardOutput, ct);
        _ = ReadStreamAsync(_process.StandardError,  ct);
    }

    private async Task ReadStreamAsync(StreamReader reader, CancellationToken ct)
    {
        try
        {
            var buffer = new char[1024];
            while (!ct.IsCancellationRequested)
            {
                int n = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                if (n == 0) break; // end of stream
                OutputReceived?.Invoke(new string(buffer, 0, n));
            }
        }
        catch (OperationCanceledException) { }
        catch { /* stream closed by process exit — expected */ }
    }

    /// <summary>Sends a line of text to the process stdin.</summary>
    public void SendLine(string line)
    {
        if (_process.HasExited) return;
        try { _process.StandardInput.WriteLine(line); }
        catch { /* process may have exited between the check and the write */ }
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (!_process.HasExited)
        {
            try { _process.Kill(entireProcessTree: true); }
            catch { }

            try { await _process.WaitForExitAsync(CancellationToken.None); }
            catch { }
        }
        _process.Dispose();
        _cts.Dispose();
    }
}
