using ConsoleEngine.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

namespace ConsoleEngine.Rendering.Native;

/// <summary>
/// OpenGL-backed 2D sprite renderer that opens a native OS window.
/// Replaces <c>System.Console</c> output with GPU-accelerated rendering.
/// </summary>
/// <remarks>
/// Coordinate system: (0,0) at top-left, X grows right, Y grows down.
/// This matches 2D screen conventions and the existing <c>ConsoleRenderer</c>.
/// </remarks>
/// <example>
/// <code>
/// using var renderer = new NativeRenderer("My Game", 1280, 720);
///
/// ConsoleEngine.Core.TextureHandle heroTex = default;
/// renderer.Loaded += () => heroTex = renderer.LoadTexture("hero.png");
///
/// renderer.OnUpdate += delta => { /* move entities */ };
/// renderer.OnRender += delta =>
/// {
///     renderer.Clear(new Vector4(0.05f, 0.05f, 0.05f, 1f));
///     renderer.DrawSprite(heroTex, new Vector2(100, 100), new Vector2(64, 64));
/// };
///
/// renderer.Run();  // blocks until window closes
/// </code>
/// </example>
public sealed class NativeRenderer : IGameRenderer
{
    private readonly IWindow          _window;
    private GL?                       _gl;
    private GlSpriteBatch?            _batch;
    private Matrix4x4                 _projection;
    private readonly Dictionary<uint, GlTexture> _textures = new();
    private uint _nextId = 1;

    // ── IGameRenderer events ──────────────────────────────────────────────────

    public event Action?         Loaded;
    public event Action<double>? OnUpdate;
    public event Action<double>? OnRender;

    // ── Properties ────────────────────────────────────────────────────────────

    public int Width  => _window.Size.X;
    public int Height => _window.Size.Y;

    // ── Constructor ───────────────────────────────────────────────────────────

    public NativeRenderer(string title = "ConsoleEngine", int width = 1280, int height = 720)
    {
        var options = WindowOptions.Default with
        {
            Title = title,
            Size  = new Vector2D<int>(width, height),
            // Request OpenGL 3.3 Core — widely supported baseline
            API   = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.ForwardCompatible,
                new APIVersion(3, 3)),
        };

        _window        =  Window.Create(options);
        _window.Load   += WindowLoaded;
        _window.Update += dt => OnUpdate?.Invoke(dt);
        _window.Render += RenderFrame;
        _window.Resize += sz => RebuildProjection(sz.X, sz.Y);
    }

    // ── IGameRenderer ─────────────────────────────────────────────────────────

    public void Run() => _window.Run();

    public void Close() => _window.Close();

    public TextureHandle LoadTexture(string path)
    {
        if (_gl is null)
            throw new InvalidOperationException(
                "LoadTexture must be called from the Loaded event or later, not before Run().");

        var tex = new GlTexture(_gl, path);
        uint id = _nextId++;
        _textures[id] = tex;
        return new TextureHandle(id);
    }

    public void UnloadTexture(TextureHandle handle)
    {
        if (_textures.Remove(handle.Id, out var tex))
            tex.Dispose();
    }

    public void Clear(Vector4 color)
    {
        _gl!.ClearColor(color.X, color.Y, color.Z, color.W);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void DrawSprite(
        TextureHandle handle,
        Vector2 position,
        Vector2 size,
        float   rotation = 0f,
        Vector4 tint     = default)
    {
        if (!handle.IsValid || !_textures.TryGetValue(handle.Id, out var tex)) return;
        _batch!.DrawSprite(tex, _projection, position, size, rotation, tint);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void WindowLoaded()
    {
        _gl    = GL.GetApi(_window);
        _batch = new GlSpriteBatch(_gl);

        // Alpha blending for PNG transparency
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        RebuildProjection(_window.Size.X, _window.Size.Y);

        // Wire Escape → close
        var input    = _window.CreateInput();
        var keyboard = input.Keyboards.Count > 0 ? input.Keyboards[0] : null;
        if (keyboard is not null)
            keyboard.KeyDown += (_, key, _) =>
            {
                if (key == Key.Escape) _window.Close();
            };

        // Notify subscribers that GL is ready and textures can now be loaded
        Loaded?.Invoke();
    }

    private void RenderFrame(double delta)
    {
        OnRender?.Invoke(delta);
    }

    private void RebuildProjection(int w, int h)
    {
        if (w <= 0 || h <= 0) return;
        _gl?.Viewport(0, 0, (uint)w, (uint)h);
        // Orthographic: (0,0) at top-left, Y grows downward — matches screen space
        _projection = Matrix4x4.CreateOrthographicOffCenter(
            left: 0f, right: w,
            bottom: h, top: 0f,
            zNearPlane: -1f, zFarPlane: 1f);
    }

    public void Dispose()
    {
        foreach (var tex in _textures.Values) tex.Dispose();
        _textures.Clear();
        _batch?.Dispose();
        _gl?.Dispose();
        _window.Dispose();
    }
}
