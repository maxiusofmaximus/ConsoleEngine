using System.Runtime.InteropServices;
using System.Text;
using ConsoleEngine.Rendering;

namespace ConsoleEngine.Benchmarks;

/// <summary>
/// L1 PoC interop to the Rust <c>ce_render</c> cdylib for <c>ToAnsiString</c>.
/// Resolves the native lib by absolute path (robust under the BenchmarkDotNet child process).
/// </summary>
internal static unsafe class RustRender
{
    private const string Lib = "ce_render";
    private static readonly string DllPath =
        @"C:\Users\maxli\ConsoleEngine\native\ce_render\target\release\ce_render.dll";

    static RustRender()
    {
        NativeLibrary.SetDllImportResolver(typeof(RustRender).Assembly,
            (name, _, _) => name == Lib ? NativeLibrary.Load(DllPath) : IntPtr.Zero);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ce_pixels_to_ansi(
        byte* pixels, int width, int height,
        byte trR, byte trG, byte trB,
        int crlf,
        byte* outBuf, nuint outCap, nuint* outLen);

    private static int Crlf => Environment.NewLine == "\r\n" ? 1 : 0;

    /// <summary>Safe upper bound for the output byte buffer (matches the Rust crate's bound).</summary>
    public static int MaxBufferSize(int w, int h)
    {
        int termRows = (h + 1) / 2;
        int nl = Crlf != 0 ? 2 : 1;
        return termRows * (w * 41 + 4 + nl);
    }

    /// <summary>Zero managed allocation: writes UTF-8 ANSI bytes into <paramref name="buffer"/>; returns length.</summary>
    public static int ToAnsiBytesInto(PixelArtRenderer.Rgb[,] pixels, PixelArtRenderer.Rgb transparent, byte[] buffer)
    {
        int h = pixels.GetLength(0), w = pixels.GetLength(1);
        fixed (PixelArtRenderer.Rgb* p = pixels)
        fixed (byte* outp = buffer)
        {
            nuint len;
            int rc = ce_pixels_to_ansi((byte*)p, w, h, transparent.R, transparent.G, transparent.B,
                                       Crlf, outp, (nuint)buffer.Length, &len);
            if (rc != 0) throw new InvalidOperationException($"ce_pixels_to_ansi rc={rc} (need {(int)len} bytes)");
            return (int)len;
        }
    }

    /// <summary>Drop-in replacement: one final string allocation, no per-pixel temporaries.</summary>
    public static string ToAnsiString(PixelArtRenderer.Rgb[,] pixels, PixelArtRenderer.Rgb transparent = default)
    {
        int h = pixels.GetLength(0), w = pixels.GetLength(1);
        var buf = new byte[MaxBufferSize(w, h)];
        int len = ToAnsiBytesInto(pixels, transparent, buf);
        return Encoding.UTF8.GetString(buf, 0, len);
    }
}
