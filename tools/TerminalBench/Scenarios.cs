using System.Diagnostics;
using ConsoleEngine.Terminal;

namespace TerminalBench;

/// <summary>
/// The measured scenarios. Each is evidence-first: it records raw numbers to the log and makes
/// no judgement — interpretation happens later against the JSONL history.
/// </summary>
public sealed class Scenarios
{
    private readonly BenchLogger _log;
    private static readonly Dictionary<string, object> Empty = new();

    public Scenarios(BenchLogger log) => _log = log;

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static async Task<int> WaitForExitAsync(ITerminalBackend backend, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnExit(int code) => tcs.TrySetResult(code);
        backend.Exited += OnExit;
        try
        {
            // Cover the race where it exited before we subscribed.
            if (!backend.IsRunning) return backend.ExitCode;
            var done = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
            return done == tcs.Task ? tcs.Task.Result : throw new TimeoutException($"child did not exit in {timeoutMs}ms");
        }
        finally { backend.Exited -= OnExit; }
    }

    private static Dictionary<string, object> DistMetrics(string prefix, Distribution d) => new()
    {
        [$"{prefix}_min_ms"]    = Round(d.Min),
        [$"{prefix}_median_ms"] = Round(d.Median),
        [$"{prefix}_mean_ms"]   = Round(d.Mean),
        [$"{prefix}_p95_ms"]    = Round(d.P95),
        [$"{prefix}_max_ms"]    = Round(d.Max),
    };

    private static void AddResource(Dictionary<string, object> m, ResourceUsage r)
    {
        m["alloc_bytes"]   = r.AllocatedBytes;
        m["alloc_kb"]      = Round(r.AllocatedBytes / 1024.0);
        m["gc_gen0"]       = r.Gen0;
        m["gc_gen1"]       = r.Gen1;
        m["gc_gen2"]       = r.Gen2;
        m["cpu_ms_total"]  = Round(r.CpuMsTotal);
        m["cpu_ms_user"]   = Round(r.CpuMsUser);
    }

    private static double Round(double v) => Math.Round(v, 4);
    private static double Round(double v, int digits) => Math.Round(v, digits);

    // ── scenarios ──────────────────────────────────────────────────────────────────

    /// <summary>Spawn → exit latency across N iterations (process lifecycle overhead).</summary>
    public async Task StartupLatency(int iterations, int warmup)
    {
        var samples = new List<double>(iterations);
        try
        {
            for (int i = 0; i < warmup; i++)
            {
                var b = TerminalBackendFactory.Start(PlatformCommands.QuickExit());
                await WaitForExitAsync(b, 10_000);
                await b.DisposeAsync();
            }

            var res = new ResourceSampler();
            res.Start();
            for (int i = 0; i < iterations; i++)
            {
                var sw = Stopwatch.StartNew();
                var b = TerminalBackendFactory.Start(PlatformCommands.QuickExit());
                await WaitForExitAsync(b, 10_000);
                sw.Stop();
                await b.DisposeAsync();
                samples.Add(sw.Elapsed.TotalMilliseconds);
            }
            var usage = res.Stop();

            var m = DistMetrics("latency", Distribution.From(samples));
            m["iterations"] = iterations;
            AddResource(m, usage);
            m["alloc_per_spawn_bytes"] = iterations > 0 ? usage.AllocatedBytes / iterations : 0;
            _log.Record("startup_latency", "ok", null,
                new Dictionary<string, object> { ["iterations"] = iterations, ["warmup"] = warmup }, m);
        }
        catch (Exception ex)
        {
            _log.Record("startup_latency", "error", ex.Message,
                new Dictionary<string, object> { ["iterations"] = iterations }, Empty);
        }
    }

    /// <summary>Sustained read throughput: child emits N lines; measure MB/s and alloc/byte.</summary>
    public async Task Throughput(int lines)
    {
        try
        {
            long totalBytes = 0;
            int chunks = 0;
            var firstByte = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var backend = TerminalBackendFactory.Start(PlatformCommands.BulkOutput(lines));
            var res = new ResourceSampler();
            res.Start();
            var sw = Stopwatch.StartNew();

            backend.Output += chunk =>
            {
                if (Interlocked.Increment(ref chunks) == 1) firstByte.TrySetResult();
                Interlocked.Add(ref totalBytes, chunk.Length);
            };

            await WaitForExitAsync(backend, 120_000);
            // Allow ConPTY's final-frame flush (ClosePseudoConsole on exit) to drain.
            await Task.Delay(50);
            sw.Stop();
            var usage = res.Stop();
            await backend.DisposeAsync();

            double seconds = sw.Elapsed.TotalSeconds;
            double mb = totalBytes / (1024.0 * 1024.0);
            var m = new Dictionary<string, object>
            {
                ["lines"]            = lines,
                ["total_bytes"]      = totalBytes,
                ["total_mb"]         = Round(mb),
                ["chunks"]           = chunks,
                ["avg_chunk_bytes"]  = chunks > 0 ? totalBytes / chunks : 0,
                ["elapsed_ms"]       = Round(sw.Elapsed.TotalMilliseconds),
                ["throughput_mb_s"]  = seconds > 0 ? Round(mb / seconds) : 0,
                ["alloc_per_byte"]   = totalBytes > 0 ? Round((double)usage.AllocatedBytes / totalBytes, 4) : 0,
            };
            AddResource(m, usage);
            // 0 bytes means content never flowed (e.g. ConPTY in a non-console host) — flag it so
            // the record isn't mistaken for a real measurement when comparing history.
            _log.Record("throughput", totalBytes > 0 ? "ok" : "no_output", null,
                new Dictionary<string, object> { ["lines"] = lines }, m);
        }
        catch (Exception ex)
        {
            _log.Record("throughput", "error", ex.Message,
                new Dictionary<string, object> { ["lines"] = lines }, Empty);
        }
    }

    /// <summary>Write→echo round-trip latency: write a marker, time until it appears in output.</summary>
    public async Task WriteRoundTrip(int iterations, int warmup)
    {
        var samples = new List<double>(iterations);
        ITerminalBackend? backend = null;
        try
        {
            backend = TerminalBackendFactory.Start(PlatformCommands.InteractiveShell());

            // Per-iteration marker matched against the rolling output buffer.
            string? wanted = null;
            var hit = new object();
            TaskCompletionSource? pending = null;
            var tail = new System.Text.StringBuilder();

            backend.Output += chunk =>
            {
                string s = System.Text.Encoding.UTF8.GetString(chunk.Span);
                lock (hit)
                {
                    tail.Append(s);
                    if (tail.Length > 8192) tail.Remove(0, tail.Length - 8192);
                    if (wanted is not null && tail.ToString().Contains(wanted))
                        pending?.TrySetResult();
                }
            };

            // Let the shell settle (initial prompt).
            await Task.Delay(400);

            async Task<bool> OneRoundTrip(string marker, int timeoutMs, List<double>? into)
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (hit) { wanted = marker; pending = tcs; tail.Clear(); }
                var sw = Stopwatch.StartNew();
                backend!.Write(PlatformCommands.WriteCommand(marker));
                var done = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
                sw.Stop();
                if (done != tcs.Task) return false;
                into?.Add(sw.Elapsed.TotalMilliseconds);
                return true;
            }

            // Prime: retry (cheaply) until the shell first responds, so cold-start init doesn't
            // pollute the measured samples or cost a 10s timeout per cold miss.
            bool primed = false;
            for (int i = 0; i < 25 && !primed; i++)
                primed = await OneRoundTrip($"CE_PRIME_{i}_OK", 500, null);

            for (int i = 0; i < warmup; i++)
                await OneRoundTrip($"CE_WARM_{i}_OK", 3_000, null);

            bool runningAtLoop = backend.IsRunning;
            int ok = 0;
            for (int i = 0; i < iterations; i++)
                if (await OneRoundTrip($"CE_RT_{i}_OK", 3_000, samples)) ok++;

            string sampleTail;
            lock (hit) sampleTail = tail.ToString();
            sampleTail = sampleTail.Length > 300 ? sampleTail[^300..] : sampleTail;

            var m = DistMetrics("rtt", Distribution.From(samples));
            m["iterations"]      = iterations;
            m["succeeded"]       = ok;
            m["running_at_loop"] = runningAtLoop;
            m["running_at_end"]  = backend.IsRunning;
            m["sample_tail"]     = sampleTail.Replace("", "<ESC>").Replace("\r", "\\r").Replace("\n", "\\n");
            _log.Record("write_roundtrip", ok == iterations ? "ok" : "partial", null,
                new Dictionary<string, object> { ["iterations"] = iterations, ["warmup"] = warmup }, m);
        }
        catch (Exception ex)
        {
            _log.Record("write_roundtrip", "error", ex.Message,
                new Dictionary<string, object> { ["iterations"] = iterations }, Empty);
        }
        finally
        {
            if (backend is not null) await backend.DisposeAsync();
        }
    }

    /// <summary>Resize-call throughput/latency on a live shell (syscall cost, no child re-layout wait).</summary>
    public async Task ResizeLatency(int iterations, int warmup)
    {
        ITerminalBackend? backend = null;
        var samples = new List<double>(iterations);
        try
        {
            backend = TerminalBackendFactory.Start(PlatformCommands.InteractiveShell());
            await Task.Delay(200);

            for (int i = 0; i < warmup; i++)
                backend.Resize(80 + (i % 40), 24 + (i % 10));

            var res = new ResourceSampler();
            res.Start();
            for (int i = 0; i < iterations; i++)
            {
                int cols = 80 + (i % 40), rows = 24 + (i % 10);
                var sw = Stopwatch.StartNew();
                backend.Resize(cols, rows);
                sw.Stop();
                samples.Add(sw.Elapsed.TotalMilliseconds);
            }
            var usage = res.Stop();

            var m = DistMetrics("resize", Distribution.From(samples));
            m["iterations"] = iterations;
            AddResource(m, usage);
            _log.Record("resize_latency", "ok", null,
                new Dictionary<string, object> { ["iterations"] = iterations, ["warmup"] = warmup }, m);
        }
        catch (Exception ex)
        {
            _log.Record("resize_latency", "error", ex.Message,
                new Dictionary<string, object> { ["iterations"] = iterations }, Empty);
        }
        finally
        {
            if (backend is not null) await backend.DisposeAsync();
        }
    }

    /// <summary>Dispose-kill latency: spawn a long-runner, time DisposeAsync teardown.</summary>
    public async Task DisposeLatency(int iterations, int warmup)
    {
        var samples = new List<double>(iterations);
        try
        {
            for (int i = 0; i < warmup + iterations; i++)
            {
                var backend = TerminalBackendFactory.Start(PlatformCommands.LongRunning());
                await Task.Delay(50); // ensure it's fully up
                var sw = Stopwatch.StartNew();
                await backend.DisposeAsync();
                sw.Stop();
                if (i >= warmup) samples.Add(sw.Elapsed.TotalMilliseconds);
            }

            var m = DistMetrics("dispose", Distribution.From(samples));
            m["iterations"] = iterations;
            _log.Record("dispose_latency", "ok", null,
                new Dictionary<string, object> { ["iterations"] = iterations, ["warmup"] = warmup }, m);
        }
        catch (Exception ex)
        {
            _log.Record("dispose_latency", "error", ex.Message,
                new Dictionary<string, object> { ["iterations"] = iterations }, Empty);
        }
    }

    /// <summary>Stress: many concurrent sessions each emitting output; measure aggregate + stability.</summary>
    public async Task ConcurrentStress(int sessions, int linesEach)
    {
        try
        {
            long totalBytes = 0;
            int failures = 0;
            var res = new ResourceSampler();
            res.Start();
            var sw = Stopwatch.StartNew();

            var tasks = new Task[sessions];
            for (int s = 0; s < sessions; s++)
            {
                tasks[s] = Task.Run(async () =>
                {
                    try
                    {
                        var b = TerminalBackendFactory.Start(PlatformCommands.BulkOutput(linesEach));
                        b.Output += chunk => Interlocked.Add(ref totalBytes, chunk.Length);
                        await WaitForExitAsync(b, 120_000);
                        await Task.Delay(20);
                        await b.DisposeAsync();
                    }
                    catch { Interlocked.Increment(ref failures); }
                });
            }
            await Task.WhenAll(tasks);
            sw.Stop();
            var usage = res.Stop();

            double seconds = sw.Elapsed.TotalSeconds;
            double mb = totalBytes / (1024.0 * 1024.0);
            var m = new Dictionary<string, object>
            {
                ["sessions"]        = sessions,
                ["lines_each"]      = linesEach,
                ["failures"]        = failures,
                ["total_bytes"]     = totalBytes,
                ["total_mb"]        = Round(mb),
                ["elapsed_ms"]      = Round(sw.Elapsed.TotalMilliseconds),
                ["throughput_mb_s"] = seconds > 0 ? Round(mb / seconds) : 0,
                ["ms_per_session"]  = sessions > 0 ? Round(sw.Elapsed.TotalMilliseconds / sessions) : 0,
            };
            AddResource(m, usage);
            string stressResult = failures > 0 ? "partial" : (totalBytes > 0 ? "ok" : "no_output");
            _log.Record("concurrent_stress", stressResult, null,
                new Dictionary<string, object> { ["sessions"] = sessions, ["lines_each"] = linesEach }, m);
        }
        catch (Exception ex)
        {
            _log.Record("concurrent_stress", "error", ex.Message,
                new Dictionary<string, object> { ["sessions"] = sessions }, Empty);
        }
    }
}
