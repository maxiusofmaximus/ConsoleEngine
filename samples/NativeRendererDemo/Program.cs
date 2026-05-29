using ConsoleEngine.Core;
using ConsoleEngine.Rendering.Native;
using System.Numerics;

// ── NativeRenderer Demo ───────────────────────────────────────────────────────
// Opens a 1280×720 window using OpenGL via Silk.NET.
// Renders a bouncing ConsoleEngine logo sprite.
// Press Escape to close.

using var renderer = new NativeRenderer("ConsoleEngine — Native Renderer Demo", 1280, 720);

TextureHandle logoTex = TextureHandle.Invalid;

// Position and velocity for the bouncing sprite
var position = new Vector2(608f, 328f);
var velocity = new Vector2(220f, 160f);   // pixels per second
var spriteSize = new Vector2(64f, 64f);
float angle = 0f;

// ── Load assets once GL context is ready ─────────────────────────────────────

renderer.Loaded += () =>
{
    // Try to find a sprite — use the DinoGame sprite if present, otherwise draw nothing
    string[] candidates =
    [
        Path.Combine(AppContext.BaseDirectory, "logo.png"),
        // Repo assets — always present
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "icon.png"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", "icon.png"),
    ];

    foreach (string path in candidates)
    {
        string full = Path.GetFullPath(path);
        if (!File.Exists(full)) continue;
        logoTex = renderer.LoadTexture(full);
        Console.WriteLine($"[NativeRendererDemo] Loaded texture: {full}");
        break;
    }

    if (!logoTex.IsValid)
        Console.WriteLine("[NativeRendererDemo] No texture found — window will show background only.");
};

// ── Update: move and bounce ───────────────────────────────────────────────────

renderer.OnUpdate += delta =>
{
    float dt = (float)delta;

    position += velocity * dt;
    angle    += dt * 0.8f;   // slow spin

    // Bounce off window edges
    if (position.X < 0f)
    {
        position.X = 0f;
        velocity.X = Math.Abs(velocity.X);
    }
    else if (position.X + spriteSize.X > renderer.Width)
    {
        position.X = renderer.Width - spriteSize.X;
        velocity.X = -Math.Abs(velocity.X);
    }

    if (position.Y < 0f)
    {
        position.Y = 0f;
        velocity.Y = Math.Abs(velocity.Y);
    }
    else if (position.Y + spriteSize.Y > renderer.Height)
    {
        position.Y = renderer.Height - spriteSize.Y;
        velocity.Y = -Math.Abs(velocity.Y);
    }
};

// ── Render ────────────────────────────────────────────────────────────────────

renderer.OnRender += _ =>
{
    // Dark background — not a terminal, a real window
    renderer.Clear(new Vector4(0.04f, 0.04f, 0.06f, 1f));

    if (logoTex.IsValid)
        renderer.DrawSprite(logoTex, position, spriteSize, rotation: angle);
};

// ── Run (blocks until window closes or Escape is pressed) ────────────────────

Console.WriteLine("[NativeRendererDemo] Opening native window — press Escape to close.");
renderer.Run();
Console.WriteLine("[NativeRendererDemo] Window closed.");
