using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ConsoleEngine.Persistence;

namespace ConsoleEngine.Benchmarks;

/// <summary>
/// Measures SaveRepository I/O throughput, especially LoadMostRecent() which calls
/// File.GetLastWriteTimeUtc per file-comparison in the current implementation.
/// Run with: dotnet run -c Release -- --filter "*SaveRepository*"
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class SaveRepositoryBenchmarks
{
    [Params(1, 5, 20, 50)]
    public int SaveFileCount { get; set; }

    private string                      _saveDir = null!;
    private SaveRepository<SavePayload> _repo    = null!;
    private SavePayload                 _payload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _saveDir = Path.Combine(Path.GetTempPath(), $"ce_bench_saves_{SaveFileCount}_{Guid.NewGuid():N}");
        _repo    = new SaveRepository<SavePayload>(_saveDir, p => p.SlotId);
        _payload = new SavePayload { SlotId = "bench_slot", Data = new string('x', 1024) };

        // Pre-create N save files with distinct timestamps
        for (int i = 0; i < SaveFileCount; i++)
        {
            var slot = new SavePayload { SlotId = $"slot_{i:D4}", Data = new string((char)('a' + i % 26), 256) };
            _repo.Save(slot);
            Thread.Sleep(1); // ensure distinct LastWriteTimeUtc per file on Windows
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_saveDir))
            Directory.Delete(_saveDir, recursive: true);
    }

    [Benchmark(Baseline = true)]
    public SavePayload LoadMostRecent() => _repo.LoadMostRecent();

    [Benchmark]
    public void Save_Single() => _repo.Save(_payload);

    [Benchmark]
    public void Save_Then_Load_Roundtrip()
    {
        _repo.Save(_payload);
        _repo.LoadMostRecent();
    }

    [Benchmark]
    public bool HasAnySave() => _repo.HasAnySave();

    // ── Payload ───────────────────────────────────────────────────────────────

    public sealed class SavePayload
    {
        public string SlotId { get; set; } = string.Empty;
        public string Data   { get; set; } = string.Empty;
    }
}
