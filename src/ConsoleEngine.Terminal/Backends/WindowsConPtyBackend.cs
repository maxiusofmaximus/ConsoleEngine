using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using ConsoleEngine.Terminal.Interop;
using Microsoft.Win32.SafeHandles;
using static ConsoleEngine.Terminal.Interop.NativeMethodsWindows;

namespace ConsoleEngine.Terminal.Backends;

/// <summary>
/// <see cref="ITerminalBackend"/> backed by the Windows Pseudo Console (ConPTY) API.
/// </summary>
[SupportedOSPlatform("windows10.0.17763.0")]
internal sealed class WindowsConPtyBackend : ITerminalBackend
{
    private readonly IntPtr         _hPC;
    private readonly IntPtr         _attrList;
    private readonly FileStream     _inStream;   // we write → child stdin
    private readonly FileStream     _outStream;  // we read  ← child stdout/stderr
    private readonly IntPtr         _hProcess;
    private readonly IntPtr         _hThread;
    private readonly CancellationTokenSource _cts = new();

    private int  _exitCode;
    private bool _running = true;
    private bool _disposed;

    public event Action<ReadOnlyMemory<byte>>? Output;
    public event Action<int>?                  Exited;

    public bool IsRunning => _running;
    public int  ExitCode  => _exitCode;

    public WindowsConPtyBackend(TerminalOptions options)
    {
        // 1. Two pipes: input (we write) and output (we read).
        if (!CreatePipe(out SafeFileHandle inputRead,  out SafeFileHandle inputWrite,  IntPtr.Zero, 0))
            throw new InvalidOperationException($"CreatePipe (input) failed: {Marshal.GetLastWin32Error()}");
        if (!CreatePipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite, IntPtr.Zero, 0))
            throw new InvalidOperationException($"CreatePipe (output) failed: {Marshal.GetLastWin32Error()}");

        // 2. Create the pseudo console wired to the child-facing pipe ends.
        var size = new COORD { X = (short)options.Cols, Y = (short)options.Rows };
        int hr = CreatePseudoConsole(size, inputRead, outputWrite, 0, out _hPC);
        if (hr != 0)
            throw new InvalidOperationException($"CreatePseudoConsole failed: HRESULT 0x{hr:X8}");

        // The PC duplicated the child-facing ends; we can release our copies.
        inputRead.Dispose();
        outputWrite.Dispose();

        // 3. STARTUPINFOEX carrying the HPCON as a thread attribute.
        _attrList = BuildAttributeList(_hPC, out STARTUPINFOEX startupEx);

        // 4. Launch the child.
        string cmdLine = BuildCommandLine(options.Command, options.Args);
        IntPtr envBlock = BuildEnvironmentBlock(options.Env);
        try
        {
            // A Unicode environment block REQUIRES CREATE_UNICODE_ENVIRONMENT, otherwise
            // CreateProcess treats it as ANSI and fails with ERROR_INVALID_PARAMETER (87).
            uint flags = EXTENDED_STARTUPINFO_PRESENT;
            if (envBlock != IntPtr.Zero) flags |= CREATE_UNICODE_ENVIRONMENT;

            bool ok = CreateProcess(
                lpApplicationName: null,
                lpCommandLine:     cmdLine,
                lpProcessAttributes: IntPtr.Zero,
                lpThreadAttributes:  IntPtr.Zero,
                bInheritHandles:   false,
                dwCreationFlags:   flags,
                lpEnvironment:     envBlock,
                lpCurrentDirectory: string.IsNullOrEmpty(options.WorkingDirectory) ? null : options.WorkingDirectory,
                lpStartupInfo:     ref startupEx,
                lpProcessInformation: out PROCESS_INFORMATION pi);

            if (!ok)
                throw new InvalidOperationException($"CreateProcess failed: {Marshal.GetLastWin32Error()}");

            _hProcess = pi.hProcess;
            _hThread  = pi.hThread;
        }
        finally
        {
            if (envBlock != IntPtr.Zero) Marshal.FreeHGlobal(envBlock);
        }

        // 5. Our pipe ends as streams.
        _inStream  = new FileStream(inputWrite, FileAccess.Write);
        _outStream = new FileStream(outputRead, FileAccess.Read);

        // 6. Background readers/waiters.
        _ = Task.Run(ReadLoopAsync);
        _ = Task.Run(WaitForExit);
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (_disposed || !_running) return;
        try
        {
            _inStream.Write(data);
            _inStream.Flush();
        }
        catch { /* pipe broken — child likely exited */ }
    }

    public void Resize(int cols, int rows)
    {
        if (_disposed || !_running) return;
        _ = ResizePseudoConsole(_hPC, new COORD { X = (short)cols, Y = (short)rows });
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private async Task ReadLoopAsync()
    {
        var buffer = new byte[4096];
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                int n = await _outStream.ReadAsync(buffer, _cts.Token).ConfigureAwait(false);
                if (n == 0) break; // pipe closed
                Output?.Invoke(new ReadOnlyMemory<byte>(buffer, 0, n));
            }
        }
        catch (OperationCanceledException) { }
        catch { /* pipe closed by child exit — expected */ }
    }

    private void WaitForExit()
    {
        _ = WaitForSingleObject(_hProcess, INFINITE);
        _exitCode = GetExitCodeProcess(_hProcess, out uint code) ? (int)code : -1;
        _running = false;
        Exited?.Invoke(_exitCode);
    }

    private static IntPtr BuildAttributeList(IntPtr hPC, out STARTUPINFOEX startupEx)
    {
        startupEx = default;
        startupEx.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

        nint size = 0;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);

        IntPtr heap = GetProcessHeap();
        IntPtr list = HeapAlloc(heap, 0, size);
        if (list == IntPtr.Zero)
            throw new InvalidOperationException("HeapAlloc for the proc-thread attribute list failed.");

        if (!InitializeProcThreadAttributeList(list, 1, 0, ref size))
            throw new InvalidOperationException($"InitializeProcThreadAttributeList failed: {Marshal.GetLastWin32Error()}");

        if (!UpdateProcThreadAttribute(
                list, 0, (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                hPC, IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
            throw new InvalidOperationException($"UpdateProcThreadAttribute failed: {Marshal.GetLastWin32Error()}");

        startupEx.lpAttributeList = list;
        return list;
    }

    private static string BuildCommandLine(string command, string[] args)
    {
        var sb = new StringBuilder();
        AppendQuoted(sb, command);
        foreach (string a in args)
        {
            sb.Append(' ');
            AppendQuoted(sb, a);
        }
        return sb.ToString();

        static void AppendQuoted(StringBuilder sb, string s)
        {
            if (s.Length > 0 && !s.Contains(' ') && !s.Contains('"')) { sb.Append(s); return; }
            sb.Append('"');
            foreach (char c in s)
            {
                if (c == '"') sb.Append('\\');
                sb.Append(c);
            }
            sb.Append('"');
        }
    }

    private static IntPtr BuildEnvironmentBlock(IReadOnlyDictionary<string, string>? extra)
    {
        if (extra is null || extra.Count == 0) return IntPtr.Zero;

        // Merge parent env with the overrides (case-insensitive keys on Windows).
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            merged[(string)e.Key] = (string)(e.Value ?? string.Empty);
        foreach (var kv in extra) merged[kv.Key] = kv.Value;

        // Block format: key=value\0 ... \0\0  (UTF-16, sorted by key).
        var sb = new StringBuilder();
        foreach (var kv in merged.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\0');
        sb.Append('\0');

        return Marshal.StringToHGlobalUni(sb.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();

        if (_running)
        {
            try { _ = TerminateProcess(_hProcess, 0); } catch { }
        }

        // Closing the PC releases the child-facing pipe ends and unblocks the read loop.
        try { ClosePseudoConsole(_hPC); } catch { }

        try { _inStream.Dispose();  } catch { }
        try { _outStream.Dispose(); } catch { }

        if (_attrList != IntPtr.Zero)
        {
            DeleteProcThreadAttributeList(_attrList);
            _ = HeapFree(GetProcessHeap(), 0, _attrList);
        }

        if (_hThread  != IntPtr.Zero) _ = CloseHandle(_hThread);
        if (_hProcess != IntPtr.Zero) _ = CloseHandle(_hProcess);

        _cts.Dispose();
        await Task.CompletedTask;
    }
}
