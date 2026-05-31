using System.Runtime.InteropServices;
using ConsoleEngine.Terminal;

namespace TerminalBench;

/// <summary>
/// Builds <see cref="TerminalOptions"/> for equivalent child workloads on each OS, so the same
/// scenario measures the same logical work (the backend, not the child program) everywhere.
/// </summary>
public static class PlatformCommands
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private static string Cwd => Environment.CurrentDirectory;

    /// <summary>A child that exits immediately with code 0 — measures spawn/teardown overhead.</summary>
    public static TerminalOptions QuickExit() => IsWindows
        ? new("cmd.exe", ["/c", "exit", "0"], Cwd)
        : new("/bin/sh", ["-c", "exit 0"], Cwd);

    /// <summary>A child that exits with a specific code — measures exit-code propagation.</summary>
    public static TerminalOptions ExitWithCode(int code) => IsWindows
        ? new("cmd.exe", ["/c", $"exit {code}"], Cwd)
        : new("/bin/sh", ["-c", $"exit {code}"], Cwd);

    /// <summary>A child that prints <paramref name="lines"/> lines of ~70 chars then exits.</summary>
    public static TerminalOptions BulkOutput(int lines)
    {
        // ~70-char payload so each line is a realistic terminal row.
        const string payload = "0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ-abcdefghijklmnopqrstuvwxyz-##";
        return IsWindows
            ? new("cmd.exe", ["/c", $"for /L %n in (1,1,{lines}) do @echo {payload}"], Cwd)
            : new("/bin/sh", ["-c", $"i=0; while [ $i -lt {lines} ]; do echo {payload}; i=$((i+1)); done"], Cwd);
    }

    /// <summary>A long-lived interactive shell that echoes its stdin — measures write round-trip.</summary>
    public static TerminalOptions InteractiveShell() => IsWindows
        ? new("cmd.exe", [], Cwd, Env: TerminalOptions.DefaultEnv)
        : new("/bin/sh", ["-i"], Cwd, Env: TerminalOptions.DefaultEnv);

    /// <summary>A child that stays alive long enough to measure dispose-kill latency.</summary>
    public static TerminalOptions LongRunning() => IsWindows
        ? new("cmd.exe", ["/c", "ping -n 60 127.0.0.1 >nul"], Cwd)
        : new("/bin/sh", ["-c", "sleep 60"], Cwd);

    /// <summary>Bytes the child's echo command emits per <see cref="WriteCommand"/>, sans CRLF noise.</summary>
    public static byte[] WriteCommand(string marker) =>
        System.Text.Encoding.UTF8.GetBytes(IsWindows ? $"echo {marker}\r\n" : $"echo {marker}\n");
}
