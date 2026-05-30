using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ConsoleEngine.Core;

namespace ConsoleEngine.Benchmarks;

/// <summary>
/// Measures FlagStore serialization throughput at 10 → 10 000 flags.
/// Run with: dotnet run -c Release -- --filter "*FlagStore*"
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class FlagStoreBenchmarks
{
    [Params(10, 100, 1000, 10_000)]
    public int FlagCount { get; set; }

    private FlagStore _store     = null!;
    private string    _storeJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        _store = new FlagStore();
        for (int i = 0; i < FlagCount; i++)
        {
            // Mix of types to exercise the generic serializer path
            switch (i % 3)
            {
                case 0: _store.Set($"flag_str_{i}", $"value_{i}");  break;
                case 1: _store.Set($"flag_int_{i}", i);              break;
                case 2: _store.Set($"flag_bool_{i}", i % 2 == 0);   break;
            }
        }
        _storeJson = _store.ToJson();
    }

    [Benchmark(Baseline = true)]
    public string ToJson() => _store.ToJson();

    [Benchmark]
    public FlagStore FromJson() => FlagStore.FromJson(_storeJson);

    [Benchmark]
    public void Get_AllFlags()
    {
        for (int i = 0; i < FlagCount; i++)
        {
            switch (i % 3)
            {
                case 0: _store.Get<string>($"flag_str_{i}");  break;
                case 1: _store.Get<int>($"flag_int_{i}");     break;
                case 2: _store.Get<bool>($"flag_bool_{i}");   break;
            }
        }
    }

    [Benchmark]
    public void Set_AllFlags()
    {
        var tmp = new FlagStore();
        for (int i = 0; i < FlagCount; i++)
            tmp.Set($"key_{i}", i);
    }
}
