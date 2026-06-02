using System.Text;
using ConsoleEngine.Rendering;
using Xunit;

namespace ConsoleEngine.Tests;

/// <summary>
/// L2a: the byte/stream ANSI API (<see cref="PixelArtRenderer.ToAnsiBytes"/> /
/// <see cref="PixelArtRenderer.WriteAnsi"/>) must be byte-identical to the managed
/// <see cref="PixelArtRenderer.ToAnsiString(PixelArtRenderer.Rgb[,], PixelArtRenderer.Rgb)"/> on
/// both the managed-fallback path (always) and the native path (when the accelerator is present).
/// </summary>
[Trait("Category", "Rendering")]
public sealed class PixelArtRendererNativeTests
{
    private static PixelArtRenderer.Rgb[,] AllSolid(int h, int w)
    {
        var g = new PixelArtRenderer.Rgb[h, w];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            g[y, x] = new PixelArtRenderer.Rgb(200, 50, 50);
        return g;
    }

    // Mixed grid with transparent (== default) pixels to exercise top-only / bottom-only / empty.
    private static PixelArtRenderer.Rgb[,] Mixed(int h, int w)
    {
        var g = new PixelArtRenderer.Rgb[h, w];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            g[y, x] = ((x + y) % 3 == 0) ? default : new PixelArtRenderer.Rgb((byte)(x * 30), (byte)(y * 20), 7);
        return g;
    }

    public static IEnumerable<object[]> Grids() => new[]
    {
        new object[] { AllSolid(16, 16) },
        new object[] { Mixed(16, 16) },
        new object[] { Mixed(9, 7) },   // odd height → last terminal row is top-only
    };

    private static byte[] ToBytesViaApi(PixelArtRenderer.Rgb[,] g)
    {
        var buf = new byte[PixelArtRenderer.MaxAnsiByteCount(g.GetLength(1), g.GetLength(0))];
        int len = PixelArtRenderer.ToAnsiBytes(g, buf);
        return buf[..len];
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void ToAnsiBytes_ManagedFallback_MatchesToAnsiString(PixelArtRenderer.Rgb[,] grid)
    {
        bool prev = PixelArtRenderer.NativeAcceleration;
        try
        {
            PixelArtRenderer.NativeAcceleration = false; // force managed
            string expected = PixelArtRenderer.ToAnsiString(grid);
            string actual   = Encoding.UTF8.GetString(ToBytesViaApi(grid));
            Assert.Equal(expected, actual);
        }
        finally { PixelArtRenderer.NativeAcceleration = prev; }
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void WriteAnsi_ManagedFallback_MatchesToAnsiString(PixelArtRenderer.Rgb[,] grid)
    {
        bool prev = PixelArtRenderer.NativeAcceleration;
        try
        {
            PixelArtRenderer.NativeAcceleration = false;
            string expected = PixelArtRenderer.ToAnsiString(grid);
            using var ms = new MemoryStream();
            PixelArtRenderer.WriteAnsi(grid, ms);
            Assert.Equal(expected, Encoding.UTF8.GetString(ms.ToArray()));
        }
        finally { PixelArtRenderer.NativeAcceleration = prev; }
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void ToAnsiBytes_NativePath_IsByteIdentical(PixelArtRenderer.Rgb[,] grid)
    {
        if (!PixelArtRenderer.IsNativeAccelerationAvailable)
            return; // native accelerator not present (e.g. CI) — managed path covered above

        bool prev = PixelArtRenderer.NativeAcceleration;
        try
        {
            PixelArtRenderer.NativeAcceleration = true; // force native
            string expected = PixelArtRenderer.ToAnsiString(grid);
            string actual   = Encoding.UTF8.GetString(ToBytesViaApi(grid));
            Assert.Equal(expected, actual);
        }
        finally { PixelArtRenderer.NativeAcceleration = prev; }
    }

    [Fact]
    public void ToAnsiBytes_DestinationTooSmall_Throws()
    {
        var grid = AllSolid(8, 8);
        var tiny = new byte[4];
        Assert.Throws<ArgumentException>(() => PixelArtRenderer.ToAnsiBytes(grid, tiny));
    }
}
