using System.Runtime.InteropServices;
using System.Text;
using ConsoleEngine.Terminal;

// Standalone ConPTY probe — runs the real PTY backend OUTSIDE of vstest, in a normal
// interactive console process, to determine whether ConPTY renders child output here.
// If "CONTAINS=True" shows up below, the backend is correct and only the test host
// environment (vstest, headless) was suppressing rendering.
// Marker token is "ce-pty-ok" — kept consistent with the test suite.

bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
string cwd = Environment.CurrentDirectory;

Console.WriteLine($"=== ConPtyProbe — OS={RuntimeInformation.OSDescription} ===");
Console.WriteLine();

// 1) echo, fast exit
await RunScenario(
    "echo-fast",
    isWindows
        ? new TerminalOptions("cmd.exe", ["/c", "echo ce-pty-ok"], cwd, 80, 24, TerminalOptions.DefaultEnv)
        : new TerminalOptions("/bin/sh", ["-c", "echo ce-pty-ok"], cwd, 80, 24, TerminalOptions.DefaultEnv),
    expect: "ce-pty-ok",
    writeInput: null,
    waitMs: 2500);

// 2) echo, kept alive briefly
await RunScenario(
    "echo-alive",
    isWindows
        ? new TerminalOptions("cmd.exe", ["/c", "echo ce-pty-ok & ping -n 3 127.0.0.1"], cwd, 80, 24, TerminalOptions.DefaultEnv)
        : new TerminalOptions("/bin/sh", ["-c", "echo ce-pty-ok; sleep 1"], cwd, 80, 24, TerminalOptions.DefaultEnv),
    expect: "ce-pty-ok",
    writeInput: null,
    waitMs: 3500);

// 3) write to child stdin, read it back
await RunScenario(
    "write-stdin",
    isWindows
        ? new TerminalOptions("cmd.exe", ["/v:on", "/c", "set /p x=  & echo RESULT=!x!"], cwd, 80, 24, TerminalOptions.DefaultEnv)
        : new TerminalOptions("/bin/sh", ["-c", "read x; echo RESULT=$x"], cwd, 80, 24, TerminalOptions.DefaultEnv),
    expect: "RESULT=marker-123",
    writeInput: "marker-123\r\n",
    waitMs: 3000);

Console.WriteLine("=== done ===");
return;

static async Task RunScenario(string name, TerminalOptions opts, string expect, string? writeInput, int waitMs)
{
    var sb   = new StringBuilder();
    var gate = new object();
    int exitCode = int.MinValue;

    await using var backend = TerminalBackendFactory.Start(opts);
    backend.Output += mem => { lock (gate) sb.Append(Encoding.UTF8.GetString(mem.Span)); };
    backend.Exited += code => exitCode = code;

    if (writeInput is not null)
    {
        await Task.Delay(400);
        backend.Write(Encoding.UTF8.GetBytes(writeInput));
    }

    await Task.Delay(waitMs);

    string s;
    lock (gate) s = sb.ToString();

    Console.WriteLine($"--- {name} ---");
    Console.WriteLine($"  bytes    = {s.Length}");
    Console.WriteLine($"  exitCode = {(exitCode == int.MinValue ? "(still running)" : exitCode.ToString())}");
    Console.WriteLine($"  CONTAINS \"{expect}\" = {s.Contains(expect, StringComparison.Ordinal)}");
    Console.WriteLine($"  dump     = [{Escape(s)}]");
    Console.WriteLine();
}

static string Escape(string s)
{
    var sb = new StringBuilder(s.Length * 2);
    foreach (char c in s)
        sb.Append(c switch
        {
            '' => "<ESC>",
            '\r'     => "<CR>",
            '\n'     => "<LF>",
            '\t'     => "<TAB>",
            < ' '    => $"<{(int)c:X2}>",
            _        => c.ToString()
        });
    return sb.ToString();
}
