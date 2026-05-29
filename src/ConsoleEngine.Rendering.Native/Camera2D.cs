using System.Numerics;

namespace ConsoleEngine.Rendering.Native;

/// <summary>
/// Orthographic 2D camera. Translates and zooms the view so that a target point
/// stays centred on the viewport.
/// </summary>
/// <remarks>
/// Usage: multiply <see cref="GetViewMatrix"/> with the renderer's projection matrix
/// to get a view-projection matrix that centres the camera on <see cref="Position"/>.
/// </remarks>
public sealed class Camera2D
{
    /// <summary>World-space position the camera looks at (centred on viewport).</summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>Zoom multiplier. 1 = no zoom, 2 = 2× zoom, 0.5 = zoomed out.</summary>
    public float Zoom { get; set; } = 1f;

    /// <summary>
    /// Returns a view matrix that centres <see cref="Position"/> on a viewport
    /// of the given dimensions.
    /// </summary>
    public Matrix4x4 GetViewMatrix(int viewportWidth, int viewportHeight)
    {
        // Translate so that Position maps to the viewport centre, then scale by Zoom.
        var centre = new Vector2(viewportWidth * 0.5f, viewportHeight * 0.5f);
        var offset = centre - Position * Zoom;

        return Matrix4x4.CreateScale(Zoom, Zoom, 1f)
             * Matrix4x4.CreateTranslation(offset.X, offset.Y, 0f);
    }

    /// <summary>
    /// Converts a screen-pixel point to a world-space point using this camera.
    /// Useful for mouse picking.
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPoint, int viewportWidth, int viewportHeight)
    {
        var centre = new Vector2(viewportWidth * 0.5f, viewportHeight * 0.5f);
        return (screenPoint - centre) / Zoom + Position;
    }
}
