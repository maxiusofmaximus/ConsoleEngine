using TerminalBench;

// ── ConsoleEngine.Terminal — PTY backend benchmark & stress harness ──────────────────
//
// Evidence-first: measures the real backend through a real console host (so ConPTY content
// flows on Windows), writes structured JSONL + a Markdown summary to artifacts/benchmarks/
// terminal/, and prints where the log landed. No optimization decisions live here — this only
// produces the baseline data that decisions are made against.
//
// Usage:
//   dotnet run -c Release --project tools/TerminalBench [-- quick|full]
//     quick : smaller iteration counts for a fast smoke of the harness
//     full  : default; the publishable baseline

string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "full";
bool quick = mode is "quick" or "q";

var env = RunMetadata.Capture();

// Resolve artifacts dir relative to the repo root (two levels up from tools/TerminalBench).
string repoRoot = FindRepoRoot(Environment.CurrentDirectory) ?? Environment.CurrentDirectory;
string outDir = Path.Combine(repoRoot, "artifacts", "benchmarks", "terminal");

using var logger = new BenchLogger(outDir, env);
var scenarios = new Scenarios(logger);

Console.WriteLine($"== ConsoleEngine.Terminal benchmark ==");
Console.WriteLine($"   run     : {env.RunId}  ({mode})");
Console.WriteLine($"   platform: {env.OsPlatform} {env.OsArchitecture} / proc {env.ProcessArchitecture}");
Console.WriteLine($"   build   : {env.BuildConfig}  {env.Framework}");
Console.WriteLine($"   git     : {env.GitCommit}");
Console.WriteLine($"   log     : {logger.LogPath}");
Console.WriteLine();

// Iteration plan (kept modest so a full run finishes in a couple of minutes).
int spawnIters   = quick ? 10  : 50;
int rttIters     = quick ? 10  : 40;
int resizeIters  = quick ? 100 : 1000;
int disposeIters = quick ? 5   : 15;
int tputLines    = quick ? 2000 : 20000;
int burstLines   = quick ? 2000 : 20000;
int stressN      = quick ? 4   : 12;
int stressLines  = quick ? 500 : 2000;

await Step("startup_latency",   () => scenarios.StartupLatency(spawnIters, warmup: quick ? 2 : 5));
await Step("throughput",        () => scenarios.Throughput(tputLines));
await Step("burst_throughput",  () => scenarios.BurstThroughput(burstLines));
await Step("write_roundtrip",   () => scenarios.WriteRoundTrip(rttIters, warmup: 3));
await Step("resize_latency",    () => scenarios.ResizeLatency(resizeIters, warmup: 50));
await Step("dispose_latency",   () => scenarios.DisposeLatency(disposeIters, warmup: 2));
await Step("concurrent_stress", () => scenarios.ConcurrentStress(stressN, stressLines));

string md = logger.WriteMarkdownSummary();
Console.WriteLine();
Console.WriteLine("== done ==");
Console.WriteLine($"   JSONL   : {logger.LogPath}");
Console.WriteLine($"   summary : {md}");
return 0;

static async Task Step(string name, Func<Task> run)
{
    Console.Write($"  • {name} ... ");
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await run();
    sw.Stop();
    Console.WriteLine($"done ({sw.Elapsed.TotalSeconds:F1}s)");
}

static string? FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "ConsoleEngine.sln"))) return dir.FullName;
        dir = dir.Parent;
    }
    return null;
}
