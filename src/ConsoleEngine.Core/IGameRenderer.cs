using System.Numerics;

namespace ConsoleEngine.Core;

/// <summary>
/// Abstraction for 2D sprite rendering to a native window or surface.
/// Decouples game logic from the specific graphics backend (OpenGL, Vulkan, etc.).
/// </summary>
/// <remarks>
/// Implementations:
/// <list type="bullet">
///   <item><c>NativeRenderer</c> — OpenGL via Silk.NET (windowed, GPU-accelerated).</item>
///   <item><c>ConsoleRenderer</c> — existing ANSI terminal output (legacy mode).</item>
/// </list>
/// </remarks>
public interface IGameRenderer : IDisposable
{
    /// <summary>
    /// Fired once after the graphics context is initialised and textures can be loaded.
    /// Subscribe here to call <see cref="LoadTexture"/>.
    /// </summary>
    event Action? Loaded;

    /// <summary>Fired each frame before <see cref="OnRender"/>. Argument is delta time in seconds.</summary>
    event Action<double>? OnUpdate;

    /// <summary>Fired each frame after <see cref="OnUpdate"/>. Argument is delta time in seconds.</summary>
    event Action<double>? OnRender;

    /// <summary>Render surface width in pixels.</summary>
    int Width { get; }

    /// <summary>Render surface height in pixels.</summary>
    int Height { get; }

    /// <summary>
    /// Loads a PNG file into GPU memory and returns an opaque handle.
    /// Must be called from the <see cref="Loaded"/> event handler or later.
    /// </summary>
    TextureHandle LoadTexture(string path);

    /// <summary>Releases a previously loaded texture from GPU memory.</summary>
    void UnloadTexture(TextureHandle handle);

    /// <summary>Fills the frame with the given RGBA colour (0–1 range).</summary>
    void Clear(Vector4 color);

    /// <summary>
    /// Draws a textured rectangle at <paramref name="position"/> (top-left corner, pixels).
    /// </summary>
    /// <param name="texture">Handle returned by <see cref="LoadTexture"/>.</param>
    /// <param name="position">Top-left corner in screen pixels.</param>
    /// <param name="size">Width and height in screen pixels.</param>
    /// <param name="rotation">Clockwise rotation in radians around the top-left corner.</param>
    /// <param name="tint">RGBA multiplier applied to each pixel. Default (<c>default</c>) = white = no tint.</param>
    void DrawSprite(
        TextureHandle texture,
        Vector2 position,
        Vector2 size,
        float   rotation = 0f,
        Vector4 tint     = default);

    /// <summary>
    /// Enters the render loop. Blocks until the window is closed or
    /// <see cref="Close"/> is called.
    /// </summary>
    void Run();

    /// <summary>Signals the renderer to close the window and exit the render loop.</summary>
    void Close();
}

/// <summary>Opaque handle to a GPU-resident texture. Created by <see cref="IGameRenderer.LoadTexture"/>.</summary>
public readonly record struct TextureHandle(uint Id)
{
    /// <summary>Sentinel value representing no texture.</summary>
    public static readonly TextureHandle Invalid = new(0);

    /// <summary><see langword="true"/> when this handle refers to a valid loaded texture.</summary>
    public bool IsValid => Id != 0;
}
