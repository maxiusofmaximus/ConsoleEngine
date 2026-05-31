using System.Runtime.InteropServices;
using System.Text;
using ConsoleEngine.Terminal;
using Xunit;

namespace ConsoleEngine.Tests;

/// <summary>
/// Cross-platform PTY backend tests, split into two tiers:
/// <list type="bullet">
/// <item><description><b>Environment-independent</b> (<see cref="FactAttribute"/>) — spawn, exit
///   code, lifecycle, resize, factory, and "bytes flow through the reader". These hold on every
///   host and are the CI regression guard.</description></item>
/// <item><description><b>Content / round-trip</b> (<see cref="PtyRenderFactAttribute"/>) — assert
///   the child's rendered output. Reliable on POSIX (raw pty) and on a console-capable Windows
///   host, but ConPTY does not render content inside the headless Windows vstest host, so they are
///   skipped there unless <c>CONSOLEENGINE_PTY_RENDER_TESTS=1</c>. <c>tools/ConPtyProbe</c> covers
///   them interactively.</description></item>
/// </list>
/// </summary>
[Trait("Category", "Terminal")]
public sealed class TerminalBackendTests
{
    private const string Marker = "ce-pty-ok";

    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // Spawns a shell that runs one command and exits.
    private static TerminalOptions Shell(string posixCmd, string winCmd, int cols = 80, int rows = 24)
        => IsWindows
            ? new TerminalOptions("cmd.exe", ["/c", winCmd], Environment.CurrentDirectory, cols, rows,
                                  TerminalOptions.DefaultEnv)
            : new TerminalOptions("/bin/sh", ["-c", posixCmd], Environment.CurrentDirectory, cols, rows,
                                  TerminalOptions.DefaultEnv);

    // Collects output until the backend exits or a timeout elapses.
    private static async Task<(string output, int exit)> RunToEndAsync(TerminalOptions opts, TimeSpan timeout)
    {
        var sb     = new StringBuilder();
        var exited = new TaskCompletionSource<int>();
        await using var backend = TerminalBackendFactory.Start(opts);

        backend.Output += mem => { lock (sb) sb.Append(Encoding.UTF8.GetString(mem.Span)); };
        backend.Exited += code => exited.TrySetResult(code);

        await Task.WhenAny(exited.Task, Task.Delay(timeout));
        await Task.Delay(80); // let the reader flush trailing output after exit
        int exit = exited.Task.IsCompleted ? exited.Task.Result : -999;
        lock (sb) return (sb.ToString(), exit);
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

    // ── Environment-independent (always run) ─────────────────────────────────────

    [Fact]
    public void Start_NullOptions_Throws()
        => Assert.Throws<ArgumentNullException>(() => TerminalBackendFactory.Start(null!));

    [Fact]
    public async Task Exited_FiresWithExitCode()
    {
        var (_, exit) = await RunToEndAsync(Shell("exit 7", "exit 7"), TimeSpan.FromSeconds(10));
        Assert.Equal(7, exit);
    }

    [Fact]
    public async Task Backend_ProducesOutput_DoesNotDeadlock()
    {
        // Proves the output pipe + read loop work end to end: bytes flow and the session completes
        // without hanging. This is deliberately NOT a content assertion — on the Windows vstest host
        // the bytes are ConPTY's handshake VT; on POSIX they are the child's raw output. Either way
        // "non-empty + completed" means the reader is alive and not deadlocked.
        var opts = IsWindows
            ? Shell("", "for /L %i in (1,1,500) do @echo line-%i")
            : Shell("for i in $(seq 1 500); do echo line-$i; done", "");

        var (output, exit) = await RunToEndAsync(opts, TimeSpan.FromSeconds(30));

        Assert.NotEqual(-999, exit);                // completed within timeout = no deadlock
        Assert.False(string.IsNullOrEmpty(output)); // data flowed through the reader
    }

    [Fact]
    public async Task Resize_DoesNotThrow()
    {
        var opts = IsWindows
            ? new TerminalOptions("cmd.exe", [], Environment.CurrentDirectory, 80, 24)
            : new TerminalOptions("/bin/sh", [], Environment.CurrentDirectory, 80, 24);

        await using var backend = TerminalBackendFactory.Start(opts);
        backend.Resize(120, 40);
        backend.Resize(80, 24);
    }

    [Fact]
    public async Task Dispose_KillsChild_NoOrphan()
    {
        var opts = Shell("sleep 30", "ping -n 30 127.0.0.1 > nul");
        var backend = TerminalBackendFactory.Start(opts);
        Assert.True(backend.IsRunning);

        await backend.DisposeAsync();
        await Task.Delay(250); // allow SIGTERM/TerminateProcess + waitpid to settle

        Assert.False(backend.IsRunning);
    }

    // ── Content / round-trip (skipped on the Windows vstest host) ────────────────

    [PtyRenderFact]
    public async Task Start_EchoCommand_ProducesContent()
    {
        var (output, _) = await RunToEndAsync(Shell($"echo {Marker}", $"echo {Marker}"),
                                              TimeSpan.FromSeconds(15));
        Assert.Contains(Marker, output, StringComparison.Ordinal);
    }

    [PtyRenderFact]
    public async Task Write_ReachesChildStdin_AndIsEchoed()
    {
        // Child reads ONE line from stdin and echoes it back transformed.
        // Windows uses delayed expansion (!x!): %x% would be expanded at parse time, before
        // set /p runs, yielding an empty value.
        var opts = IsWindows
            ? new TerminalOptions("cmd.exe", ["/v:on", "/c", "set /p x=  & echo RESULT=!x!"],
                                  Environment.CurrentDirectory, 80, 24, TerminalOptions.DefaultEnv)
            : new TerminalOptions("/bin/sh", ["-c", "read x; echo RESULT=$x"],
                                  Environment.CurrentDirectory, 80, 24, TerminalOptions.DefaultEnv);

        var sb = new StringBuilder();
        await using var backend = TerminalBackendFactory.Start(opts);
        backend.Output += mem => { lock (sb) sb.Append(Encoding.UTF8.GetString(mem.Span)); };

        await Task.Delay(300); // let the child reach its read()
        backend.Write(Encoding.UTF8.GetBytes($"{Marker}\r\n"));

        bool found = await WaitForAsync(
            () => { lock (sb) return sb.ToString().Contains($"RESULT={Marker}", StringComparison.Ordinal); },
            TimeSpan.FromSeconds(15));

        string dump; lock (sb) dump = sb.ToString();
        Assert.True(found, $"'RESULT={Marker}' not seen. Output was:\n{dump}");
    }

    [PtyRenderFact]
    [Trait("Platform", "POSIX")]
    public async Task Resize_ChildSeesNewColumns_Posix()
    {
        if (IsWindows) return; // POSIX-only; tput is not available on cmd

        var opts = new TerminalOptions("/bin/sh", ["-c", "sleep 0.3; tput cols"],
            Environment.CurrentDirectory, 80, 24, TerminalOptions.DefaultEnv);

        var sb     = new StringBuilder();
        var exited = new TaskCompletionSource<int>();
        await using var backend = TerminalBackendFactory.Start(opts);
        backend.Output += mem => { lock (sb) sb.Append(Encoding.UTF8.GetString(mem.Span)); };
        backend.Exited += code => exited.TrySetResult(code);

        backend.Resize(132, 40);
        await Task.WhenAny(exited.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        await Task.Delay(80);

        string s; lock (sb) s = sb.ToString();
        Assert.Contains("132", s, StringComparison.Ordinal);
    }
}
