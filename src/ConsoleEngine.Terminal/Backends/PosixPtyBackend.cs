using System.Runtime.InteropServices;
using ConsoleEngine.Terminal.Interop;
using static ConsoleEngine.Terminal.Interop.NativeMethodsPosix;

namespace ConsoleEngine.Terminal.Backends;

/// <summary>
/// <see cref="ITerminalBackend"/> backed by a POSIX pseudo-terminal (Linux, macOS, ARM/RPi).
/// Master fd via <c>posix_openpt</c>; child launched with <c>posix_spawn</c> using
/// <c>POSIX_SPAWN_SETSID</c> + an <c>addopen</c> of the slave pts so it becomes the child's
/// controlling terminal without running any in-child code.
/// </summary>
internal sealed class PosixPtyBackend : ITerminalBackend
{
    private readonly int  _master;
    private readonly int  _pid;
    private readonly Thread _readThread;
    private volatile bool _running = true;
    private int  _exitCode;
    private bool _disposed;

    private readonly BufferedOutput _output = new();

    // Buffered so a subscriber attaching after Start() still receives the child's
    // initial output (e.g. the shell prompt) instead of racing the read loop.
    public event Action<ReadOnlyMemory<byte>>? Output
    {
        add    { if (value is not null) _output.Subscribe(value); }
        remove { if (value is not null) _output.Unsubscribe(value); }
    }

    public event Action<int>? Exited;

    public bool IsRunning => _running;
    public int  ExitCode  => _exitCode;

    public PosixPtyBackend(TerminalOptions options)
    {
        // 1. Open PTY master and unlock the slave.
        _master = posix_openpt(O_RDWR | O_NOCTTY);
        if (_master < 0)
            throw new InvalidOperationException($"posix_openpt failed: errno {Marshal.GetLastWin32Error()}");
        if (grantpt(_master) != 0)
            throw new InvalidOperationException($"grantpt failed: errno {Marshal.GetLastWin32Error()}");
        if (unlockpt(_master) != 0)
            throw new InvalidOperationException($"unlockpt failed: errno {Marshal.GetLastWin32Error()}");

        IntPtr namePtr = ptsname(_master);
        string slavePath = Marshal.PtrToStringAnsi(namePtr)
            ?? throw new InvalidOperationException("ptsname returned null.");

        // 2. Set initial window size on the master.
        var ws = new WinSize { ws_col = (ushort)options.Cols, ws_row = (ushort)options.Rows };
        _ = ioctl(_master, TIOCSWINSZ, ref ws);

        // 3. Build file actions + attributes (over-allocated, zeroed buffers).
        IntPtr fa   = Marshal.AllocHGlobal(FileActionsBufSize);
        IntPtr attr = Marshal.AllocHGlobal(SpawnAttrBufSize);
        Zero(fa, FileActionsBufSize);
        Zero(attr, SpawnAttrBufSize);

        IntPtr argv = IntPtr.Zero, envp = IntPtr.Zero;
        var toFree = new List<IntPtr>();
        try
        {
            if (posix_spawn_file_actions_init(fa) != 0)
                throw new InvalidOperationException("posix_spawn_file_actions_init failed.");
            if (posix_spawnattr_init(attr) != 0)
                throw new InvalidOperationException("posix_spawnattr_init failed.");

            // Child: stdin/out/err ← slave pts; master closed in child.
            _ = posix_spawn_file_actions_addopen(fa, 0, slavePath, O_RDWR, 0);
            _ = posix_spawn_file_actions_adddup2(fa, 0, 1);
            _ = posix_spawn_file_actions_adddup2(fa, 0, 2);
            _ = posix_spawn_file_actions_addclose(fa, _master);

            // Session leader → opening pts (without O_NOCTTY) makes it the controlling terminal.
            _ = posix_spawnattr_setflags(attr, POSIX_SPAWN_SETSID);

            argv = BuildNativeStringArray(Prepend(options.Command, options.Args), toFree);
            envp = BuildNativeStringArray(BuildEnv(options.Env), toFree);

            int rc = posix_spawnp(out _pid, options.Command, fa, attr, argv, envp);
            if (rc != 0)
                throw new InvalidOperationException($"posix_spawnp('{options.Command}') failed: rc {rc}");
        }
        finally
        {
            _ = posix_spawn_file_actions_destroy(fa);
            _ = posix_spawnattr_destroy(attr);
            Marshal.FreeHGlobal(fa);
            Marshal.FreeHGlobal(attr);
            foreach (IntPtr p in toFree) Marshal.FreeCoTaskMem(p);
            if (argv != IntPtr.Zero) Marshal.FreeHGlobal(argv);
            if (envp != IntPtr.Zero) Marshal.FreeHGlobal(envp);
        }

        // 4. Background readers/waiters.
        _readThread = new Thread(ReadLoop) { IsBackground = true, Name = "pty-read" };
        _readThread.Start();
        new Thread(WaitForExit) { IsBackground = true, Name = "pty-wait" }.Start();
    }

    public void Write(ReadOnlySpan<byte> data)
    {
        if (_disposed || !_running) return;
        byte[] buf = data.ToArray();
        nint remaining = buf.Length, offset = 0;
        while (remaining > 0)
        {
            nint n = WriteAt(buf, offset, remaining);
            if (n <= 0) break;
            offset += n;
            remaining -= n;
        }
    }

    public void Resize(int cols, int rows)
    {
        if (_disposed || !_running) return;
        var ws = new WinSize { ws_col = (ushort)cols, ws_row = (ushort)rows };
        _ = ioctl(_master, TIOCSWINSZ, ref ws);
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private nint WriteAt(byte[] buf, nint offset, nint count)
    {
        if (offset == 0) return write(_master, buf, count);
        // write() from an offset: slice into a temp buffer (writes are small — keystrokes).
        var slice = new byte[count];
        Array.Copy(buf, offset, slice, 0, count);
        return write(_master, slice, count);
    }

    private void ReadLoop()
    {
        var buffer = new byte[4096];
        while (!_disposed)
        {
            nint n = read(_master, buffer, buffer.Length);
            if (n > 0)
            {
                _output.Emit(new ReadOnlyMemory<byte>(buffer, 0, (int)n));
            }
            else
            {
                // 0 = EOF; -1 = error (EIO on Linux when the slave side closes) → treat as end.
                break;
            }
        }
    }

    private void WaitForExit()
    {
        _ = waitpid(_pid, out int status, 0);
        _exitCode = ExitStatus(status);
        _running = false;
        Exited?.Invoke(_exitCode);
    }

    private static string[] Prepend(string first, string[] rest)
    {
        var result = new string[rest.Length + 1];
        result[0] = first;
        Array.Copy(rest, 0, result, 1, rest.Length);
        return result;
    }

    private static string[] BuildEnv(IReadOnlyDictionary<string, string>? extra)
    {
        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            merged[(string)e.Key] = (string)(e.Value ?? string.Empty);
        if (extra is not null)
            foreach (var kv in extra) merged[kv.Key] = kv.Value;

        return merged.Select(kv => $"{kv.Key}={kv.Value}").ToArray();
    }

    /// <summary>
    /// Marshals <paramref name="values"/> into a NULL-terminated native <c>char*[]</c>.
    /// Each string and the array block are tracked in <paramref name="toFree"/> for cleanup.
    /// </summary>
    private static IntPtr BuildNativeStringArray(string[] values, List<IntPtr> toFree)
    {
        IntPtr block = Marshal.AllocHGlobal(IntPtr.Size * (values.Length + 1));
        for (int i = 0; i < values.Length; i++)
        {
            IntPtr s = Marshal.StringToCoTaskMemUTF8(values[i]);
            toFree.Add(s);
            Marshal.WriteIntPtr(block, i * IntPtr.Size, s);
        }
        Marshal.WriteIntPtr(block, values.Length * IntPtr.Size, IntPtr.Zero); // NULL terminator
        return block;
    }

    private static void Zero(IntPtr ptr, int bytes)
    {
        for (int i = 0; i < bytes; i++) Marshal.WriteByte(ptr, i, 0);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_running)
        {
            _ = kill(_pid, SIGTERM);
            // Give it a moment, then force-kill if still alive.
            await Task.Delay(100).ConfigureAwait(false);
            if (_running) _ = kill(_pid, SIGKILL);
        }

        _ = close(_master); // unblocks ReadLoop
        await Task.CompletedTask;
    }
}
