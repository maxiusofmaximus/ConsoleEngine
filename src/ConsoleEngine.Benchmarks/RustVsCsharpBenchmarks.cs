using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ConsoleEngine.Rendering;

namespace ConsoleEngine.Benchmarks;

/// <summary>
/// L2: the integrated <see cref="PixelArtRenderer"/> API — managed <c>ToAnsiString</c> (baseline)
/// vs the native-accelerated <c>ToAnsiBytes</c> and the managed-fallback <c>ToAnsiBytes</c>.
/// Run with: dotnet run -c Release -- --filter "*RustVsCsharp*"
/// </summary>
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
public class RustVsCsharpBenchmarks
{
    [Params(512, 1024)]
    public int SpriteSize { get; set; }

    private PixelArtRenderer.Rgb[,] _pixels = null!;
    private byte[]                  _buf = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows    = Enumerable.Range(0, SpriteSize).Select(_ => new string('R', SpriteSize)).ToArray();
        var palette = new Dictionary<char, PixelArtRenderer.Rgb> { ['R'] = new(200, 50, 50) };
        _pixels = PixelArtRenderer.BuildSprite(rows, palette);
        _buf    = new byte[PixelArtRenderer.MaxAnsiByteCount(SpriteSize, SpriteSize)];

        // Correctness gate: the native path must equal the managed string when present.
        if (PixelArtRenderer.IsNativeAccelerationAvailable)
        {
            PixelArtRenderer.NativeAcceleration = true;
            int n = PixelArtRenderer.ToAnsiBytes(_pixels, _buf);
            if (Encoding.UTF8.GetString(_buf, 0, n) != PixelArtRenderer.ToAnsiString(_pixels))
                throw new InvalidOperationException("native ToAnsiBytes != managed ToAnsiString");
        }
    }

    [Benchmark(Baseline = true)]
    public string Csharp_ToAnsiString() => PixelArtRenderer.ToAnsiString(_pixels);

    [Benchmark]
    public int Native_ToAnsiBytes()
    {
        PixelArtRenderer.NativeAcceleration = true;
        return PixelArtRenderer.ToAnsiBytes(_pixels, _buf);
    }

    [Benchmark]
    public int Managed_ToAnsiBytes()
    {
        PixelArtRenderer.NativeAcceleration = false;
        return PixelArtRenderer.ToAnsiBytes(_pixels, _buf);
    }
}
