using System.Numerics;
using Silk.NET.OpenGL;

namespace ConsoleEngine.Rendering.Native;

/// <summary>
/// Renders textured quads via OpenGL.
/// Uses a single VAO with a unit quad (0,0)→(1,1).
/// Each sprite applies a scale+rotate+translate transform via a uniform.
/// </summary>
/// <remarks>
/// One draw call per sprite. Sufficient for the initial implementation —
/// instanced or batched rendering can be added later without changing the API.
/// </remarks>
internal sealed class GlSpriteBatch : IDisposable
{
    private readonly GL   _gl;
    private readonly GlShader _shader;
    private readonly uint     _vao;
    private readonly uint     _vbo;
    private readonly uint     _ebo;

    // Unit quad: two triangles, (0,0) at top-left, (1,1) at bottom-right.
    // Each vertex: x, y, u, v
    private static readonly float[] Vertices =
    [
        // x     y     u     v
        0.0f,  0.0f,  0.0f, 1.0f,   // top-left
        1.0f,  0.0f,  1.0f, 1.0f,   // top-right
        1.0f,  1.0f,  1.0f, 0.0f,   // bottom-right
        0.0f,  1.0f,  0.0f, 0.0f,   // bottom-left
    ];

    private static readonly uint[] Indices = [0u, 1u, 2u,  2u, 3u, 0u];

    public unsafe GlSpriteBatch(GL gl)
    {
        _gl     = gl;
        _shader = new GlShader(gl);

        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();

        gl.BindVertexArray(_vao);

        // Vertices
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* ptr = Vertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(Vertices.Length * sizeof(float)), ptr,
                BufferUsageARB.StaticDraw);

        // Indices
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* ptr = Indices)
            gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                (nuint)(Indices.Length * sizeof(uint)), ptr,
                BufferUsageARB.StaticDraw);

        // Attribute 0: vec2 position  (offset 0)
        // Attribute 1: vec2 tex coord (offset 2 floats)
        const uint stride = 4 * sizeof(float);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);
    }

    /// <summary>
    /// Draws a single textured sprite.
    /// </summary>
    /// <param name="texture">GPU texture to sample.</param>
    /// <param name="projection">Orthographic projection from <see cref="NativeRenderer"/>.</param>
    /// <param name="position">Top-left corner in screen pixels.</param>
    /// <param name="size">Width × height in screen pixels.</param>
    /// <param name="rotation">Clockwise rotation in radians around the top-left corner.</param>
    /// <param name="tint">RGBA colour multiplier.</param>
    public unsafe void DrawSprite(
        GlTexture texture,
        Matrix4x4 projection,
        Vector2   position,
        Vector2   size,
        float     rotation,
        Vector4   tint)
    {
        // Build model matrix: scale the unit quad to sprite dimensions, then rotate, then translate.
        // System.Numerics is row-major (DirectX convention): A*B means "apply A, then B".
        var model = Matrix4x4.CreateScale(size.X, size.Y, 1f)
                  * Matrix4x4.CreateRotationZ(rotation)
                  * Matrix4x4.CreateTranslation(position.X, position.Y, 0f);

        var mvp = model * projection;

        _shader.Use();
        _shader.SetMatrix4("uMVP",     mvp);
        _shader.SetVector4("uTint",    tint == default ? Vector4.One : tint);
        _shader.SetInt    ("uTexture", 0);

        texture.Bind(0);

        _gl.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length,
            DrawElementsType.UnsignedInt, null);
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
    }
}
