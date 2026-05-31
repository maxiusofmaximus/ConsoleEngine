using System.Diagnostics;

namespace TerminalBench;

/// <summary>Latency distribution over repeated iterations of a scenario (all values in ms).</summary>
public sealed record Distribution(double Min, double Median, double Mean, double P95, double Max)
{
    public static Distribution From(IReadOnlyList<double> samples)
    {
        if (samples.Count == 0) return new Distribution(0, 0, 0, 0, 0);
        var sorted = samples.OrderBy(x => x).ToArray();
        return new Distribution(
            Min:    sorted[0],
            Median: Percentile(sorted, 50),
            Mean:   sorted.Average(),
            P95:    Percentile(sorted, 95),
            Max:    sorted[^1]);
    }

    private static double Percentile(double[] sorted, double p)
    {
        if (sorted.Length == 1) return sorted[0];
        double rank = (p / 100.0) * (sorted.Length - 1);
        int lo = (int)Math.Floor(rank);
        int hi = (int)Math.Ceiling(rank);
        double frac = rank - lo;
        return sorted[lo] + (sorted[hi] - sorted[lo]) * frac;
    }
}

/// <summary>
/// Samples managed allocations, GC collection counts, and process CPU time across a block of
/// work. Allocation delta uses the total-allocated counter (monotonic), so it captures work on
/// the backend's background read/wait threads too — not just this thread.
/// </summary>
public sealed class ResourceSampler
{
    private long _allocStart;
    private int  _g0, _g1, _g2;
    private TimeSpan _cpuStart, _cpuUserStart;

    public void Start()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        _allocStart = GC.GetTotalAllocatedBytes(precise: false);
        _g0 = GC.CollectionCount(0);
        _g1 = GC.CollectionCount(1);
        _g2 = GC.CollectionCount(2);
        var proc = Process.GetCurrentProcess();
        _cpuStart     = proc.TotalProcessorTime;
        _cpuUserStart = proc.UserProcessorTime;
    }

    public ResourceUsage Stop()
    {
        long alloc = GC.GetTotalAllocatedBytes(precise: false) - _allocStart;
        var proc = Process.GetCurrentProcess();
        return new ResourceUsage(
            AllocatedBytes: Math.Max(0, alloc),
            Gen0:  GC.CollectionCount(0) - _g0,
            Gen1:  GC.CollectionCount(1) - _g1,
            Gen2:  GC.CollectionCount(2) - _g2,
            CpuMsTotal: (proc.TotalProcessorTime - _cpuStart).TotalMilliseconds,
            CpuMsUser:  (proc.UserProcessorTime  - _cpuUserStart).TotalMilliseconds);
    }
}

public sealed record ResourceUsage(
    long   AllocatedBytes,
    int    Gen0,
    int    Gen1,
    int    Gen2,
    double CpuMsTotal,
    double CpuMsUser);
