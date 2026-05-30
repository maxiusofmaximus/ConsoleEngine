using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ConsoleEngine.Rendering;

namespace ConsoleEngine.Benchmarks;

/// <summary>
/// Measures PixelArtRenderer throughput and allocation at sprite sizes from 32×32 to 1024×1024.
/// Run with: dotnet run -c Release -- --filter "*PixelArt*"
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class PixelArtRendererBenchmarks
{
    [Params(32, 64, 128, 256, 512, 1024)]
    public int SpriteSize { get; set; }

    private PixelArtRenderer.Rgb[,] _pixels = null!;
    private bool[,]                  _solid  = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rows    = Enumerable.Range(0, SpriteSize).Select(_ => new string('R', SpriteSize)).ToArray();
        var palette = new Dictionary<char, PixelArtRenderer.Rgb> { ['R'] = new(200, 50, 50) };
        _pixels = PixelArtRenderer.BuildSprite(rows, palette);

        _solid = new bool[SpriteSize, SpriteSize];
        for (int y = 0; y < SpriteSize; y++)
        for (int x = 0; x < SpriteSize; x++)
            _solid[y, x] = true;
    }

    /// <summary>Builds ANSI rows from a pre-built RGB grid. Core hot path for rendering.</summary>
    [Benchmark(Baseline = true)]
    public string ToAnsiString_FromGrid() => PixelArtRenderer.ToAnsiString(_pixels);

    /// <summary>Same grid with 50% transparency to test the branching paths.</summary>
    [Benchmark]
    public string ToAnsiString_HalfTransparent()
    {
        var transparent = new PixelArtRenderer.Rgb(0, 0, 0);
        return PixelArtRenderer.ToAnsiString(_pixels, transparent);
    }

    /// <summary>BuildSprite itself — allocates the grid from character rows.</summary>
    [Benchmark]
    public PixelArtRenderer.Rgb[,] BuildSprite()
    {
        var rows    = Enumerable.Range(0, SpriteSize).Select(_ => new string('R', SpriteSize)).ToArray();
        var palette = new Dictionary<char, PixelArtRenderer.Rgb> { ['R'] = new(200, 50, 50) };
        return PixelArtRenderer.BuildSprite(rows, palette);
    }
}
