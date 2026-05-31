using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace ConsoleEngine.Terminal.Interop;

/// <summary>
/// Win32 ConPTY interop surface. Pure declarations — no logic.
/// See: https://learn.microsoft.com/windows/console/creating-a-pseudoconsole-session
/// </summary>
[SupportedOSPlatform("windows")]
internal static class NativeMethodsWindows
{
    internal const int  STARTF_USESTDHANDLES               = 0x00000100;
    internal const uint EXTENDED_STARTUPINFO_PRESENT       = 0x00080000;
    internal const uint CREATE_UNICODE_ENVIRONMENT         = 0x00000400;
    internal const int  PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;

    [StructLayout(LayoutKind.Sequential)]
    internal struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int    dwProcessId;
        public int    dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STARTUPINFO
    {
        public int    cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int    dwX;
        public int    dwY;
        public int    dwXSize;
        public int    dwYSize;
        public int    dwXCountChars;
        public int    dwYCountChars;
        public int    dwFillAttribute;
        public int    dwFlags;
        public short  wShowWindow;
        public short  cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr      lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int    nLength;
        public IntPtr lpSecurityDescriptor;
        public int    bInheritHandle;
    }

    // ── Pipes ──────────────────────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreatePipe(
        out SafeFileHandle hReadPipe,
        out SafeFileHandle hWritePipe,
        IntPtr lpPipeAttributes,
        int nSize);

    // ── Pseudo Console ─────────────────────────────────────────────────────────

    // COORD is a single packed 32-bit value (X = low word, Y = high word) passed by value. We pass
    // it as a uint via PackSize() — wire-equivalent to the COORD struct used by the canonical ConPTY
    // samples, but unambiguous across runtimes and marshallers.
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int CreatePseudoConsole(
        uint size,
        SafeFileHandle hInput,
        SafeFileHandle hOutput,
        uint dwFlags,
        out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int ResizePseudoConsole(IntPtr hPC, uint size);

    /// <summary>Packs columns/rows into the Win32 COORD wire format (X = low word, Y = high word).</summary>
    internal static uint PackSize(int cols, int rows)
        => (ushort)cols | ((uint)(ushort)rows << 16);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern void ClosePseudoConsole(IntPtr hPC);

    // ── Proc thread attribute list ─────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool InitializeProcThreadAttributeList(
        IntPtr lpAttributeList,
        int dwAttributeCount,
        int dwFlags,
        ref nint lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UpdateProcThreadAttribute(
        IntPtr lpAttributeList,
        uint dwFlags,
        IntPtr Attribute,
        IntPtr lpValue,
        nint cbSize,
        IntPtr lpPreviousValue,
        IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    // ── Process ────────────────────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcess(
        string? lpApplicationName,
        string  lpCommandLine,
        IntPtr  lpProcessAttributes,
        IntPtr  lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint    dwCreationFlags,
        IntPtr  lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFOEX lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    // ── Heap (for the attribute list buffer) ───────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GetProcessHeap();

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, nint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

    internal const uint INFINITE      = 0xFFFFFFFF;
    internal const uint WAIT_OBJECT_0 = 0x00000000;
}
