using System.Reflection;
using System.Runtime.InteropServices;

namespace ConsoleEngine.Rendering;

/// <summary>
/// Optional native accelerator (Rust <c>ce_render</c> cdylib) for the half-block ANSI encoder.
/// Degrades to "unavailable" if the library cannot be loaded, so callers can fall back to the
/// managed path. Output is byte-identical to
/// <see cref="PixelArtRenderer.ToAnsiString(PixelArtRenderer.Rgb[,], PixelArtRenderer.Rgb)"/>.
/// </summary>
internal static unsafe class NativeRender
{
    private const string Lib = "ce_render";
    private static nint _handle;

    /// <summary>True if the native library loaded and the entry point is callable.</summary>
    internal static bool IsAvailable { get; }

    static NativeRender()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeRender).Assembly, Resolve);
        IsAvailable = Probe();
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ce_pixels_to_ansi(
        byte* pixels, int width, int height,
        byte trR, byte trG, byte trB,
        int crlf,
        byte* outBuf, nuint outCap, nuint* outLen);

    private static int Crlf => Environment.NewLine == "\r\n" ? 1 : 0;

    private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? path)
    {
        if (name != Lib) return IntPtr.Zero;
        if (_handle != 0) return _handle;
        if (NativeLibrary.TryLoad(Lib, assembly, path, out nint h)) { _handle = h; return h; }
        // Safety net: probe right next to the loaded assembly.
        string file = OperatingSystem.IsWindows() ? "ce_render.dll"
                    : OperatingSystem.IsMacOS()   ? "libce_render.dylib"
                    :                               "libce_render.so";
        string local = Path.Combine(AppContext.BaseDirectory, file);
        if (File.Exists(local) && NativeLibrary.TryLoad(local, out h)) { _handle = h; return h; }
        return IntPtr.Zero;
    }

    private static bool Probe()
    {
        try
        {
            nuint len;
            // pixels == null returns -1 cleanly in the crate; a non-throwing call proves the symbol loaded.
            _ = ce_pixels_to_ansi(null, 1, 1, 0, 0, 0, Crlf, null, 0, &len);
            return true;
        }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
        catch (BadImageFormatException) { return false; }
    }

    /// <summary>
    /// Fills <paramref name="dest"/> with the half-block ANSI byte string. <paramref name="dest"/>
    /// must be at least <see cref="PixelArtRenderer.MaxAnsiByteCount"/> bytes. Returns the length.
    /// </summary>
    internal static int ToAnsiBytes(PixelArtRenderer.Rgb[,] pixels, Span<byte> dest, PixelArtRenderer.Rgb transparent)
    {
        int h = pixels.GetLength(0), w = pixels.GetLength(1);
        fixed (PixelArtRenderer.Rgb* p = pixels)
        fixed (byte* d = dest)
        {
            nuint len;
            int rc = ce_pixels_to_ansi((byte*)p, w, h, transparent.R, transparent.G, transparent.B,
                                       Crlf, d, (nuint)dest.Length, &len);
            if (rc != 0) throw new InvalidOperationException($"ce_pixels_to_ansi failed (rc={rc}, need {(int)len}).");
            return (int)len;
        }
    }
}
