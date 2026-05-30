using System.Runtime.InteropServices;
using ConsoleEngine.Terminal.Backends;

namespace ConsoleEngine.Terminal;

/// <summary>
/// Creates the platform-appropriate <see cref="ITerminalBackend"/>:
/// ConPTY on Windows, POSIX PTY on Linux/macOS (incl. ARM/Raspberry Pi).
/// </summary>
public static class TerminalBackendFactory
{
    /// <summary>Spawns a pseudo-terminal session for the given <paramref name="options"/>.</summary>
    /// <exception cref="PlatformNotSupportedException">The current OS has no PTY backend.</exception>
    public static ITerminalBackend Start(TerminalOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return CreateWindows(options);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException(
                "ConPTY requires Windows 10 1809 (build 17763) or later.");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new PosixPtyBackend(options);

        throw new PlatformNotSupportedException(
            $"No terminal backend for {RuntimeInformation.OSDescription}.");
    }

    // Isolated so the Windows-only type is never JIT-touched on non-Windows platforms.
    [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.17763.0")]
    private static WindowsConPtyBackend CreateWindows(TerminalOptions options)
        => new(options);
}
