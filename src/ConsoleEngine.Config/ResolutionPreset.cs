namespace ConsoleEngine.Config;

/// <summary>A named screen resolution preset.</summary>
public readonly struct ResolutionPreset
{
    public ResolutionPreset(int width, int height)
    {
        Width  = width;
        Height = height;
    }

    public int    Width  { get; }
    public int    Height { get; }
    public string Label  => $"{Width}x{Height}";
}
