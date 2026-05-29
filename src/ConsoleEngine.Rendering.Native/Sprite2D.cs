using ConsoleEngine.Core;
using System.Numerics;

namespace ConsoleEngine.Rendering.Native;

/// <summary>
/// Data for a single 2D sprite to render each frame.
/// </summary>
public sealed class Sprite2D
{
    /// <summary>GPU texture handle, obtained from <see cref="IGameRenderer.LoadTexture"/>.</summary>
    public TextureHandle Texture  { get; set; }

    /// <summary>Top-left corner position in screen pixels.</summary>
    public Vector2 Position { get; set; }

    /// <summary>Width and height in screen pixels.</summary>
    public Vector2 Size { get; set; }

    /// <summary>Clockwise rotation in radians around <see cref="Position"/>.</summary>
    public float Rotation { get; set; }

    /// <summary>
    /// RGBA colour multiplier (0–1). Default is white (no tint).
    /// </summary>
    public Vector4 Tint { get; set; } = Vector4.One;

    /// <summary>Whether this sprite is drawn this frame.</summary>
    public bool Visible { get; set; } = true;

    public Sprite2D(TextureHandle texture, Vector2 position, Vector2 size)
    {
        Texture  = texture;
        Position = position;
        Size     = size;
    }
}
