using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleEngine.Animation;

/// <summary>
/// Loads <see cref="AnimationTimeline"/> instances from <c>.anim.json</c> files on disk.
/// </summary>
/// <remarks>
/// <b>File format</b>
/// <code>
/// {
///   "name":  "death",
///   "loop":  false,
///   "frames": [
///     { "durationMs": 200, "asciiArt": ["  X  ", " XXX "], "color": "Red", "col": 10, "row": 5 },
///     { "durationMs": 100, "clearBefore": true, "asciiArt": [" *** "],       "color": "DarkRed" }
///   ]
/// }
/// </code>
/// Color values are any <see cref="ConsoleColor"/> name (case-insensitive).
/// </remarks>
public static class AnimationTimelineLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        AllowTrailingCommas     = true,
        PropertyNameCaseInsensitive = true,
        Converters              = { new JsonStringEnumConverter() },
    };

    /// <summary>Loads a timeline from disk. Throws on parse or I/O error.</summary>
    public static AnimationTimeline Load(string path)
    {
        using var stream = File.OpenRead(path);
        var dto = JsonSerializer.Deserialize<TimelineDto>(stream, Options)
            ?? throw new InvalidDataException($"Empty or invalid animation file: {path}");
        return dto.ToTimeline();
    }

    /// <summary>Attempts to load; returns <see langword="false"/> and <see langword="null"/> on failure.</summary>
    public static bool TryLoad(string path, out AnimationTimeline? timeline)
    {
        try   { timeline = Load(path); return true;  }
        catch { timeline = null;       return false; }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class TimelineDto
    {
        public string     Name   { get; init; } = string.Empty;
        public bool       Loop   { get; init; }
        public FrameDto[] Frames { get; init; } = [];

        public AnimationTimeline ToTimeline() => new()
        {
            Name   = Name,
            Loop   = Loop,
            Frames = Frames.Select(f => f.ToKeyframe()).ToList(),
        };
    }

    private sealed class FrameDto
    {
        public int          DurationMs  { get; init; } = 100;
        public string[]?    AsciiArt    { get; init; }
        public string?      SpritePath  { get; init; }
        public ConsoleColor Color       { get; init; } = ConsoleColor.White;
        public int          Col         { get; init; }
        public int          Row         { get; init; }
        public bool         ClearBefore { get; init; }
        public string?      Sound       { get; init; }

        public Keyframe ToKeyframe() => new()
        {
            DurationMs  = DurationMs,
            AsciiArt    = AsciiArt,
            SpritePath  = SpritePath,
            Color       = Color,
            Col         = Col,
            Row         = Row,
            ClearBefore = ClearBefore,
            Sound       = Sound,
        };
    }
}
