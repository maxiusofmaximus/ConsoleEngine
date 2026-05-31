using System.Runtime.InteropServices;
using Xunit;

namespace ConsoleEngine.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> for tests that assert ConPTY-<em>rendered content</em>.
/// Such tests are reliable on POSIX (a raw pty delivers bytes regardless of host) and on a
/// console-capable Windows host, but ConPTY does not render child content inside the headless
/// Windows <c>vstest</c> test host. So on Windows this fact is skipped unless
/// <c>CONSOLEENGINE_PTY_RENDER_TESTS=1</c> is set; on POSIX it always runs. The standalone
/// <c>tools/ConPtyProbe</c> verifies the same behaviour interactively.
/// </summary>
public sealed class PtyRenderFactAttribute : FactAttribute
{
    public PtyRenderFactAttribute()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && Environment.GetEnvironmentVariable("CONSOLEENGINE_PTY_RENDER_TESTS") != "1")
        {
            Skip = "ConPTY content rendering is unavailable in the Windows vstest host. "
                 + "Set CONSOLEENGINE_PTY_RENDER_TESTS=1 on a console-capable host, or run "
                 + "tools/ConPtyProbe. (POSIX PTY rendering is covered unconditionally.)";
        }
    }
}
