using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TerminalBench;

/// <summary>
/// Environment fingerprint captured once per harness invocation and embedded into every
/// JSONL record, so each line is self-contained and comparable across machines and dates
/// without re-joining against an external header.
/// </summary>
public sealed record RunMetadata(
    string RunId,
    string TimestampUtc,
    string BuildConfig,
    string Framework,
    string OsDescription,
    string OsPlatform,
    string OsArchitecture,
    string ProcessArchitecture,
    int    LogicalCores,
    string MachineName,
    string GitCommit)
{
    public static RunMetadata Capture()
    {
        string buildConfig =
#if DEBUG
            "Debug";
#else
            "Release";
#endif
        string runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}";

        return new RunMetadata(
            RunId:                runId,
            TimestampUtc:         DateTime.UtcNow.ToString("o"),
            BuildConfig:          buildConfig,
            Framework:            RuntimeInformation.FrameworkDescription,
            OsDescription:        RuntimeInformation.OSDescription,
            OsPlatform:           PlatformName(),
            OsArchitecture:       RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture:  RuntimeInformation.ProcessArchitecture.ToString(),
            LogicalCores:         Environment.ProcessorCount,
            MachineName:          Environment.MachineName,
            GitCommit:            ResolveGitCommit());
    }

    private static string PlatformName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))   return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))     return "macOS";
        return "Unknown";
    }

    private static string ResolveGitCommit()
    {
        try
        {
            var psi = new ProcessStartInfo("git", "rev-parse --short HEAD")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
            };
            using var p = Process.Start(psi);
            if (p is null) return "unknown";
            string outp = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(2000);
            return string.IsNullOrEmpty(outp) ? "unknown" : outp;
        }
        catch { return "unknown"; }
    }
}
