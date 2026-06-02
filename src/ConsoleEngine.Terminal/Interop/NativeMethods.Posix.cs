using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ConsoleEngine.Terminal.Interop;

/// <summary>
/// POSIX PTY interop for Linux and macOS (incl. ARM/Raspberry Pi).
/// Uses <c>posix_openpt</c> + <c>posix_spawn</c> — never <c>fork()</c>, which is unsafe in a
/// managed multithreaded runtime (mutexes held by other threads stay locked after fork).
/// </summary>
internal static class NativeMethodsPosix
{
    private const string Libc = "libc";

    // ── open flags ─────────────────────────────────────────────────────────────
    internal const int O_RDWR = 0x0002;
    // O_NOCTTY differs per platform.
    internal static int O_NOCTTY =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 0x20000 : 0x100;

    // POSIX_SPAWN_SETSID — make the child a session leader so opening the pts (without O_NOCTTY)
    // acquires it as the controlling terminal. Value differs per platform.
    internal static short POSIX_SPAWN_SETSID =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? (short)0x0400 : (short)0x0080;

    // ioctl TIOCSWINSZ — set window size.
    internal static nuint TIOCSWINSZ =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 0x80087467u : 0x5414u;

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinSize
    {
        public ushort ws_row;
        public ushort ws_col;
        public ushort ws_xpixel;
        public ushort ws_ypixel;
    }

    // ── PTY master setup ─────────────────────────────────────────────────────────

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_openpt(int flags);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int grantpt(int fd);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int unlockpt(int fd);

    [DllImport(Libc, SetLastError = true)]
    internal static extern IntPtr ptsname(int fd);   // returns char* to a static buffer; copy immediately

    [DllImport(Libc, SetLastError = true)]
    [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments",
        Justification = "LPUTF8Str is the correct marshaling for POSIX libc, which expects UTF-8 char*; CA2101's Unicode preference is Windows-specific.")]
    internal static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int close(int fd);

    [DllImport(Libc, SetLastError = true)]
    internal static extern nint read(int fd, byte[] buf, nint count);

    [DllImport(Libc, SetLastError = true)]
    internal static extern unsafe nint write(int fd, byte* buf, nint count);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int ioctl(int fd, nuint request, ref WinSize size);

    // ioctl is variadic: int ioctl(int, unsigned long, ...). On Darwin ARM64 (Apple Silicon)
    // ALL variadic arguments are passed on the STACK while the named args (fd, request) use
    // x0/x1. A normal fixed-signature P/Invoke puts the winsize pointer in a register the callee
    // never reads, corrupting the call (it sets a garbage window size). Filling x2..x7 with six
    // padding args forces the winsize pointer onto the first stack slot, where ioctl's va_arg
    // reads it. Used only on Darwin ARM64; every other platform uses the plain ioctl above.
    [DllImport(Libc, SetLastError = true, EntryPoint = "ioctl")]
    internal static extern unsafe int ioctl_darwin_varargs(
        int fd, nuint request,
        nint pad2, nint pad3, nint pad4, nint pad5, nint pad6, nint pad7,
        WinSize* size);

    private static readonly bool IsDarwinArm64 =
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

    /// <summary>Sets the terminal window size, routing around the Darwin ARM64 variadic ABI.</summary>
    internal static unsafe int SetWinSize(int fd, ref WinSize ws)
    {
        if (IsDarwinArm64)
        {
            fixed (WinSize* p = &ws)
                return ioctl_darwin_varargs(fd, TIOCSWINSZ, 0, 0, 0, 0, 0, 0, p);
        }
        return ioctl(fd, TIOCSWINSZ, ref ws);
    }

    // ── posix_spawn ───────────────────────────────────────────────────────────────
    // file_actions and attr are opaque, platform-sized structs. We over-allocate a zeroed
    // buffer (passed by IntPtr) large enough for both glibc and macOS layouts.

    internal const int FileActionsBufSize = 256;
    internal const int SpawnAttrBufSize   = 512;

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_spawn_file_actions_init(IntPtr fileActions);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_spawn_file_actions_destroy(IntPtr fileActions);

    [DllImport(Libc, SetLastError = true)]
    [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments",
        Justification = "LPUTF8Str is the correct marshaling for POSIX libc, which expects UTF-8 char*; CA2101's Unicode preference is Windows-specific.")]
    internal static extern int posix_spawn_file_actions_addopen(
        IntPtr fileActions, int fd, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int oflag, uint mode);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_spawn_file_actions_adddup2(IntPtr fileActions, int fd, int newfd);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_spawn_file_actions_addclose(IntPtr fileActions, int fd);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_spawnattr_init(IntPtr attr);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_spawnattr_destroy(IntPtr attr);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int posix_spawnattr_setflags(IntPtr attr, short flags);

    [DllImport(Libc, SetLastError = true)]
    [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments",
        Justification = "LPUTF8Str is the correct marshaling for POSIX libc, which expects UTF-8 char*; CA2101's Unicode preference is Windows-specific.")]
    internal static extern int posix_spawnp(
        out int pid,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string file,
        IntPtr fileActions,
        IntPtr attr,
        IntPtr argv,   // char* const[]
        IntPtr envp);  // char* const[]

    // ── process wait / signal ─────────────────────────────────────────────────────

    [DllImport(Libc, SetLastError = true)]
    internal static extern int waitpid(int pid, out int status, int options);

    [DllImport(Libc, SetLastError = true)]
    internal static extern int kill(int pid, int sig);

    internal const int SIGTERM = 15;
    internal const int SIGKILL = 9;

    // errno: a blocking syscall interrupted by a signal. The .NET runtime delivers signals for
    // GC/thread coordination, so read()/waitpid() can return EINTR and must be retried rather
    // than mistaken for EOF. EINTR is 4 on both Linux and macOS.
    internal const int EINTR = 4;

    /// <summary>Extracts the exit code from a <c>waitpid</c> status (WEXITSTATUS).</summary>
    internal static int ExitStatus(int status) => (status >> 8) & 0xFF;
}
