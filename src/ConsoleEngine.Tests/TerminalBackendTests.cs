using System.Runtime.InteropServices;
using System.Text;
using ConsoleEngine.Terminal;
using Xunit;

namespace ConsoleEngine.Tests;

/// <summary>
/// Cross-platform PTY backend tests. Each test is OS-gated and skips where the shell
/// it drives is not present, so the same suite is valid on the Windows/Linux/macOS CI matrix.
/// </summary>
[Trait("Category", "Terminal")]
public sealed class TerminalBackendTests
{
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // Spawns a shell that runs one command and exits.
    private static TerminalOptions ShellRunning(string posixCmd, string winCmd, int cols = 80, int rows = 24)
        => IsWindows
            ? new TerminalOptions("cmd.exe", ["/c", winCmd], Environment.CurrentDirectory, cols, rows,
                                  TerminalOptions.DefaultEnv)
            : new TerminalOptions("/bin/sh", ["-c", posixCmd], Environment.CurrentDirectory, cols, rows,
                                  TerminalOptions.DefaultEnv);

    // Collects output until the backend exits or a timeout elapses.
    private static async Task<(string output, int exit)> RunToEndAsync(
        TerminalOptions opts, TimeSpan timeout)
    {
        var sb       = new StringBuilder();
        var exited   = new TaskCompletionSource<int>();
        await using var backend = TerminalBackendFactory.Start(opts);

        backend.Output += mem => sb.Append(Encoding.UTF8.GetString(mem.Span));
        backend.Exited += code => exited.TrySetResult(code);

        var done = await Task.WhenAny(exited.Task, Task.Delay(timeout));
        // Give the read loop a beat to flush trailing output after exit.
        await Task.Delay(80);
        int exit = exited.Task.IsCompleted ? exited.Task.Result : -999;
        return (sb.ToString(), exit);
    }

    // ── Output ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Start_EchoCommand_ProducesOutput()
    {
        var opts = ShellRunning("echo hello-pty", "echo hello-pty");
        var (output, _) = await RunToEndAsync(opts, TimeSpan.FromSeconds(10));
        Assert.Contains("hello-pty", output, StringComparison.Ordinal);
    }

    // ── Exit code ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exited_FiresWithExitCode()
    {
        var opts = ShellRunning("exit 7", "exit 7");
        var (_, exit) = await RunToEndAsync(opts, TimeSpan.FromSeconds(10));
        Assert.Equal(7, exit);
    }

    // ── Write ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Write_ToInteractiveShell_IsProcessed()
    {
        // Start an interactive shell, type a command, and poll for the marker (no fixed sleeps —
        // interactive PTY timing varies, so we wait on the output condition with a timeout).
        var opts = IsWindows
            ? new TerminalOptions("cmd.exe", [], Environment.CurrentDirectory, 80, 24, TerminalOptions.DefaultEnv)
            : new TerminalOptions("/bin/sh", [], Environment.CurrentDirectory, 80, 24, TerminalOptions.DefaultEnv);

        var sb = new StringBuilder();
        var gate = new object();
        await using var backend = TerminalBackendFactory.Start(opts);
        backend.Output += mem => { lock (gate) sb.Append(Encoding.UTF8.GetString(mem.Span)); };

        await Task.Delay(600); // shell banner/prompt
        backend.Write(Encoding.UTF8.GetBytes("echo marker-123\r\n"));

        bool found = await WaitForAsync(
            () => { lock (gate) return sb.ToString().Contains("marker-123", StringComparison.Ordinal); },
            TimeSpan.FromSeconds(10));

        backend.Write(Encoding.UTF8.GetBytes("exit\r\n"));
        Assert.True(found, $"'marker-123' not seen. Output was:\n{sb}");
    }

    private static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition()) return true;
            await Task.Delay(50);
        }
        return condition();
    }

    // ── Resize ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Resize_DoesNotThrow()
    {
        var opts = IsWindows
            ? new TerminalOptions("cmd.exe", [], Environment.CurrentDirectory, 80, 24)
            : new TerminalOptions("/bin/sh", [], Environment.CurrentDirectory, 80, 24);

        var backend = TerminalBackendFactory.Start(opts);
        try
        {
            backend.Resize(120, 40);
            backend.Resize(80, 24);
        }
        finally
        {
            backend.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    [Fact]
    [Trait("Platform", "POSIX")]
    public async Task Resize_ChildSeesNewColumns_Posix()
    {
        if (IsWindows) return; // tput is POSIX-only; covered by Resize_DoesNotThrow on Windows

        // Resize to 132 cols, then ask the shell how wide it thinks it is.
        var opts = new TerminalOptions("/bin/sh", ["-c", "sleep 0.3; tput cols"],
            Environment.CurrentDirectory, 80, 24, TerminalOptions.DefaultEnv);

        var sb     = new StringBuilder();
        var exited = new TaskCompletionSource<int>();
        await using var backend = TerminalBackendFactory.Start(opts);
        backend.Output += mem => sb.Append(Encoding.UTF8.GetString(mem.Span));
        backend.Exited += code => exited.TrySetResult(code);

        backend.Resize(132, 40);
        await Task.WhenAny(exited.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        await Task.Delay(80);

        Assert.Contains("132", sb.ToString(), StringComparison.Ordinal);
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dispose_KillsChild_NoOrphan()
    {
        // Long-running child; dispose should terminate it and flip IsRunning.
        var opts = ShellRunning("sleep 30", "ping -n 30 127.0.0.1 > nul");
        var backend = TerminalBackendFactory.Start(opts);
        Assert.True(backend.IsRunning);

        await backend.DisposeAsync();
        await Task.Delay(250); // allow SIGTERM/TerminateProcess + waitpid to settle

        Assert.False(backend.IsRunning);
    }

    [Fact]
    public async Task HighThroughput_CompletesWithoutDeadlock()
    {
        // A child emitting thousands of lines must complete without hanging the reader.
        // NOTE: we assert *completion + non-trivial output*, not an exact byte count.
        // ConPTY delivers rendered screen output (VT repaints), so on Windows the byte
        // total is not a simple concatenation of stdout — only POSIX gives raw bytes.
        var opts = IsWindows
            ? ShellRunning("", "for /L %i in (1,1,2000) do @echo 0123456789012345678901234567890123456789")
            : ShellRunning("for i in $(seq 1 2000); do echo 0123456789012345678901234567890123456789; done", "");

        var (output, exit) = await RunToEndAsync(opts, TimeSpan.FromSeconds(30));

        Assert.NotEqual(-999, exit);                 // completed within timeout = no deadlock
        Assert.False(string.IsNullOrEmpty(output));  // data actually flowed through the reader
    }

    // ── Factory ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TerminalBackendFactory.Start(null!));
    }
}
