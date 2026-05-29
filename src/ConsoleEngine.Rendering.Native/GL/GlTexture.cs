using Silk.NET.OpenGL;
using StbImageSharp;

namespace ConsoleEngine.Rendering.Native;

/// <summary>
/// Loads a PNG/JPEG file into a GPU texture object.
/// Uses StbImageSharp (pure C#) — no GDI+ / System.Drawing required.
/// </summary>
internal sealed class GlTexture : IDisposable
{
    private readonly GL _gl;

    public uint Id     { get; }
    public int  Width  { get; }
    public int  Height { get; }

    public unsafe GlTexture(GL gl, string path)
    {
        _gl = gl;

        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture not found: '{path}'", path);

        // Flip Y so UV (0,0) = top-left, matching our screen-space coordinate system
        StbImage.stbi_set_flip_vertically_on_load(1);

        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        Width  = image.Width;
        Height = image.Height;

        Id = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, Id);

        fixed (byte* ptr = image.Data)
        {
            gl.TexImage2D(
                TextureTarget.Texture2D,
                level:          0,
                internalformat: InternalFormat.Rgba,
                width:          (uint)image.Width,
                height:         (uint)image.Height,
                border:         0,
                format:         PixelFormat.Rgba,
                type:           PixelType.UnsignedByte,
                pixels:         ptr);
        }

        // Nearest-neighbour — preserves pixel art without blurring
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>Binds this texture to the specified texture unit.</summary>
    public void Bind(uint unit = 0u)
    {
        _gl.ActiveTexture(TextureUnit.Texture0 + (int)unit);
        _gl.BindTexture(TextureTarget.Texture2D, Id);
    }

    public void Dispose() => _gl.DeleteTexture(Id);
}
