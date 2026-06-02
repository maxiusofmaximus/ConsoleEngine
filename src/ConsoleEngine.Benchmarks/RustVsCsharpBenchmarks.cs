using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ConsoleEngine.Rendering;

namespace ConsoleEngine.Benchmarks;

/// <summary>
/// L1: C# vs Rust <c>ToAnsiString</c>. Compares the managed baseline against the Rust cdylib
/// (drop-in string) and the zero-allocation buffer path.
/// Run with: dotnet run -c Release -- --filter "*RustVsCsharp*"
/// </summary>
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
public class RustVsCsharpBenchmarks
{
    [Params(512, 1024)]
    public int SpriteSize { get; set; }

    private PixelArtRenderer.Rgb[,] _pixels = null!;
    private PixelArtRenderer.Rgb    _transparent;
    private byte[]                  _reuse = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows    = Enumerable.Range(0, SpriteSize).Select(_ => new string('R', SpriteSize)).ToArray();
        var palette = new Dictionary<char, PixelArtRenderer.Rgb> { ['R'] = new(200, 50, 50) };
        _pixels      = PixelArtRenderer.BuildSprite(rows, palette);
        _transparent = default;
        _reuse       = new byte[RustRender.MaxBufferSize(SpriteSize, SpriteSize)];

        // Correctness gate — the comparison is meaningless unless Rust output == C# output.
        AssertEqual(_pixels, _transparent, "all-solid");

        // Exercise the transparent / top-only / bottom-only / empty branches + an odd height.
        var mixed = new PixelArtRenderer.Rgb[9, 7];
        for (int y = 0; y < 9; y++)
        for (int x = 0; x < 7; x++)
            mixed[y, x] = ((x + y) % 3 == 0) ? default : new PixelArtRenderer.Rgb((byte)(x * 30), (byte)(y * 20), 7);
        AssertEqual(mixed, default, "mixed-9x7");
    }

    private static void AssertEqual(PixelArtRenderer.Rgb[,] px, PixelArtRenderer.Rgb tr, string label)
    {
        string cs = PixelArtRenderer.ToAnsiString(px, tr);
        string rs = RustRender.ToAnsiString(px, tr);
        if (!string.Equals(cs, rs, StringComparison.Ordinal))
            throw new InvalidOperationException($"Rust output differs from C# ({label}).");
    }

    [Benchmark(Baseline = true)]
    public string Csharp_ToAnsiString() => PixelArtRenderer.ToAnsiString(_pixels, _transparent);

    [Benchmark]
    public string Rust_ToAnsiString() => RustRender.ToAnsiString(_pixels, _transparent);

    /// <summary>Zero-allocation buffer path (reuses one buffer) — for stream/console consumers.</summary>
    [Benchmark]
    public int Rust_ToAnsiBytes_ReuseBuffer() => RustRender.ToAnsiBytesInto(_pixels, _transparent, _reuse);
}
