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
    private readonly Thread         _readThread;
    private readonly BufferedOutput _output = new();

    private int  _exitCode;
    private int  _pcClosed;       // 0 → open, 1 → ClosePseudoConsole already called
    private bool _running = true;
    private bool _disposed;

    // Exit is one-shot. Track it so a handler subscribing AFTER a fast child has already exited
    // still receives the code (mirrors BufferedOutput's late-subscriber guarantee for Output).
    private readonly object _exitLock = new();
    private Action<int>? _exitedHandlers;
    private bool _hasExited;

    // Buffered so a subscriber attaching after Start() still receives the child's
    // initial output (e.g. the shell prompt) instead of racing the read loop.
    public event Action<ReadOnlyMemory<byte>>? Output
    {
        add    { if (value is not null) _output.Subscribe(value); }
        remove { if (value is not null) _output.Unsubscribe(value); }
    }

    public event Action<int>? Exited
    {
        add
        {
            if (value is null) return;
            bool fireNow;
            lock (_exitLock) { _exitedHandlers += value; fireNow = _hasExited; }
            if (fireNow) value(_exitCode); // already exited → replay to this late subscriber
        }
        remove { if (value is not null) lock (_exitLock) _exitedHandlers -= value; }
    }

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
        int hr = CreatePseudoConsole(PackSize(options.Cols, options.Rows), inputRead, outputWrite, 0, out _hPC);
        if (hr != 0)
            throw new InvalidOperationException($"CreatePseudoConsole failed: HRESULT 0x{hr:X8}");

        // The PC duplicated the child-facing ends; we can release our copies.
        inputRead.Dispose();
        outputWrite.Dispose();

        // 3. STARTUPINFOEX carrying the HPCON as a thread attribute.
        _attrList = BuildAttributeList(_hPC, out STARTUPINFOEX startupEx);

        // 4. Launch the child.
        string cmdLine = BuildCommandLine(options.Command, options.Args);
        IntPtr envBlock = IntPtr.Zero; // DIAG: inherit parent env to isolate env-block as the cause
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

        // 6. Background readers/waiters. A dedicated blocking-read thread (mirrors the POSIX
        // backend) avoids the per-read Task allocation and thread-pool hop that ReadAsync incurs
        // on a synchronous pipe FileStream — the dominant allocation measured in the throughput
        // benchmark (2.37 B/byte). See docs/perf-audit/terminal/baseline.md.
        _readThread = new Thread(ReadLoop) { IsBackground = true, Name = "conpty-read" };
        _readThread.Start();
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
        _ = ResizePseudoConsole(_hPC, PackSize(cols, rows));
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void ReadLoop()
    {
        var buffer = new byte[4096];
        try
        {
            while (!_disposed)
            {
                int n = _outStream.Read(buffer, 0, buffer.Length);
                if (n <= 0) break; // EOF: ConPTY's write end released on ClosePseudoConsole
                _output.Emit(new ReadOnlyMemory<byte>(buffer, 0, n));
            }
        }
        catch { /* pipe closed by child exit / dispose — expected */ }
    }

    private void WaitForExit()
    {
        _ = WaitForSingleObject(_hProcess, INFINITE);
        _exitCode = GetExitCodeProcess(_hProcess, out uint code) ? (int)code : -1;

        // The child has exited, but ConPTY may still hold its final rendered frame in the
        // screen buffer. Closing the pseudoconsole flushes that frame to the output pipe and
        // then signals EOF; ClosePseudoConsole blocks until the read loop drains the pipe, so
        // once it returns all output has been delivered. Without this, the output of one-shot
        // commands (e.g. `echo`) is lost entirely — only ConPTY's startup sequence is seen.
        ClosePseudoConsoleOnce();

        _running = false;

        Action<int>? handlers;
        lock (_exitLock) { _hasExited = true; handlers = _exitedHandlers; }
        handlers?.Invoke(_exitCode);
    }

    private void ClosePseudoConsoleOnce()
    {
        if (Interlocked.Exchange(ref _pcClosed, 1) == 0)
        {
            try { ClosePseudoConsole(_hPC); } catch { }
        }
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

        if (_running)
        {
            try { _ = TerminateProcess(_hProcess, 0); } catch { }
        }

        // Flush + close the PC FIRST: this drains the read loop and signals EOF. Closing before
        // draining would deadlock (ClosePseudoConsole waits for the output pipe to be drained) and
        // drop final output. EOF then unblocks the read thread's blocking Read.
        ClosePseudoConsoleOnce();

        try { _inStream.Dispose();  } catch { }
        try { _outStream.Dispose(); } catch { }

        if (_attrList != IntPtr.Zero)
        {
            DeleteProcThreadAttributeList(_attrList);
            _ = HeapFree(GetProcessHeap(), 0, _attrList);
        }

        if (_hThread  != IntPtr.Zero) _ = CloseHandle(_hThread);
        if (_hProcess != IntPtr.Zero) _ = CloseHandle(_hProcess);

        await Task.CompletedTask;
    }
}
