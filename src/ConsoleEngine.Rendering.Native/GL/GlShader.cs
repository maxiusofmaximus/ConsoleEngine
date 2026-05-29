using System.Numerics;
using Silk.NET.OpenGL;

namespace ConsoleEngine.Rendering.Native;

/// <summary>
/// Compiles and links a vertex + fragment GLSL shader pair.
/// Embeds the sprite shaders directly — no file I/O at runtime.
/// </summary>
internal sealed class GlShader : IDisposable
{
    private readonly GL _gl;
    private readonly uint   _program;

    // ── Embedded GLSL ─────────────────────────────────────────────────────────

    private const string VertSrc = """
        #version 330 core
        layout(location = 0) in vec2 aPosition;
        layout(location = 1) in vec2 aTexCoord;

        uniform mat4 uMVP;

        out vec2 vTexCoord;

        void main()
        {
            gl_Position = uMVP * vec4(aPosition, 0.0, 1.0);
            vTexCoord   = aTexCoord;
        }
        """;

    private const string FragSrc = """
        #version 330 core
        in  vec2 vTexCoord;
        out vec4 fragColor;

        uniform sampler2D uTexture;
        uniform vec4      uTint;

        void main()
        {
            fragColor = texture(uTexture, vTexCoord) * uTint;
        }
        """;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public GlShader(GL gl)
    {
        _gl = gl;

        uint vs = Compile(ShaderType.VertexShader,   VertSrc);
        uint fs = Compile(ShaderType.FragmentShader, FragSrc);

        _program = gl.CreateProgram();
        gl.AttachShader(_program, vs);
        gl.AttachShader(_program, fs);
        gl.LinkProgram(_program);

        gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int ok);
        if (ok == 0)
            throw new InvalidOperationException(
                $"Shader link error: {gl.GetProgramInfoLog(_program)}");

        gl.DeleteShader(vs);
        gl.DeleteShader(fs);
    }

    public void Use() => _gl.UseProgram(_program);

    // ── Uniform setters ───────────────────────────────────────────────────────

    public unsafe void SetMatrix4(string name, Matrix4x4 m)
    {
        int loc = _gl.GetUniformLocation(_program, name);
        // transpose: true — System.Numerics is row-major; OpenGL expects column-major
        _gl.UniformMatrix4(loc, 1, transpose: true, in m.M11);
    }

    public void SetVector4(string name, Vector4 v)
    {
        int loc = _gl.GetUniformLocation(_program, name);
        _gl.Uniform4(loc, v.X, v.Y, v.Z, v.W);
    }

    public void SetInt(string name, int value)
    {
        int loc = _gl.GetUniformLocation(_program, name);
        _gl.Uniform1(loc, value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private uint Compile(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int ok);
        if (ok == 0)
            throw new InvalidOperationException(
                $"Shader compile error ({type}): {_gl.GetShaderInfoLog(shader)}");

        return shader;
    }

    public void Dispose() => _gl.DeleteProgram(_program);
}
