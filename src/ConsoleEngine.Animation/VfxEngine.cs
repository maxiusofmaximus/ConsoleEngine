using ConsoleEngine.Rendering;

namespace ConsoleEngine.Animation;

/// <summary>
/// Terminal visual effects: flash, screen shake, ASCII particle bursts, and dissolve.
/// All methods are async and respect cancellation.
/// </summary>
public static class VfxEngine
{
    // ── Flash ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills a rectangle with <paramref name="flashColor"/> for <paramref name="durationMs"/>,
    /// then clears it. Useful for hit flashes, explosions, and transitions.
    /// </summary>
    public static async Task FlashAsync(
        int          col,
        int          row,
        int          width,
        int          height,
        ConsoleColor flashColor,
        int          durationMs,
        CancellationToken ct = default)
    {
        string rowStr = new('█', width);
        for (int i = 0; i < height; i++)
            AnimationEngine.DrawAt(col, row + i, rowStr, flashColor);

        try { await Task.Delay(durationMs, ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { }

        AnimationEngine.ClearRect(col, row, width, height);
    }

    /// <summary>
    /// Flashes the entire terminal for <paramref name="durationMs"/> milliseconds.
    /// </summary>
    public static async Task FullScreenFlashAsync(
        ConsoleColor flashColor,
        int          durationMs,
        CancellationToken ct = default)
    {
        int w = 0, h = 0;
        try { w = Console.WindowWidth; h = Console.WindowHeight; } catch { return; }
        await FlashAsync(0, 0, w, h, flashColor, durationMs, ct).ConfigureAwait(false);
    }

    // ── Screen shake ──────────────────────────────────────────────────────────

    /// <summary>
    /// Simulates screen shake by rapidly printing blank lines at the top of the terminal
    /// to visually "jolt" the frame, then restoring it.
    /// Works best with a small <paramref name="intensity"/> (1–3 rows).
    /// </summary>
    public static async Task ScreenShakeAsync(
        int intensity,
        int durationMs,
        int stepsMs    = 60,
        CancellationToken ct = default)
    {
        int elapsed = 0;
        string blank = string.Empty;
        try { blank = new string(' ', Console.WindowWidth); } catch { return; }

        while (elapsed < durationMs && !ct.IsCancellationRequested)
        {
            int offset = Random.Shared.Next(1, intensity + 1)
                       * (Random.Shared.Next(2) == 0 ? 1 : -1);
            int absOff = Math.Abs(offset);

            // Shift content by inserting blank lines at top
            if (offset > 0)
                for (int i = 0; i < absOff; i++)
                    AnimationEngine.DrawAt(0, i, blank);

            try { await Task.Delay(stepsMs, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }

            elapsed += stepsMs;
        }
    }

    // ── Particle burst ────────────────────────────────────────────────────────

    /// <summary>
    /// Scatters ASCII characters around a point for the given duration, then erases them.
    /// Use <paramref name="chars"/> to choose the character set (e.g. <c>"*+•·"</c>).
    /// </summary>
    public static async Task ParticleBurstAsync(
        int          centerCol,
        int          centerRow,
        string       chars,
        ConsoleColor color,
        int          count,
        int          lifetimeMs,
        int          spreadX    = 12,
        int          spreadY    = 4,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(chars)) return;
        var rng = Random.Shared;

        var particles = Enumerable.Range(0, count).Select(_ => (
            Col:  centerCol + rng.Next(-spreadX, spreadX + 1),
            Row:  centerRow + rng.Next(-spreadY, spreadY + 1),
            Ch:   chars[rng.Next(chars.Length)].ToString()
        )).ToList();

        foreach (var p in particles)
            AnimationEngine.DrawAt(p.Col, p.Row, p.Ch, color);

        try { await Task.Delay(lifetimeMs, ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { }

        foreach (var p in particles)
            AnimationEngine.DrawAt(p.Col, p.Row, " ");
    }

    // ── Dissolve ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Progressively replaces a rectangular region with <paramref name="dissolveChar"/>
    /// over <paramref name="steps"/> frames of <paramref name="stepMs"/> ms each.
    /// Useful for death/destruction effects and scene transitions.
    /// </summary>
    public static async Task DissolveAsync(
        int    col,
        int    row,
        int    width,
        int    height,
        char   dissolveChar = '░',
        ConsoleColor color  = ConsoleColor.DarkGray,
        int    steps        = 8,
        int    stepMs       = 50,
        CancellationToken ct = default)
    {
        int total    = width * height;
        var rng      = Random.Shared;
        var allCells = Enumerable.Range(0, total)
            .Select(i => (Col: col + i % width, Row: row + i / width))
            .OrderBy(_ => rng.Next())
            .ToList();

        int perStep = Math.Max(1, total / steps);

        for (int s = 0; s < steps && !ct.IsCancellationRequested; s++)
        {
            int start = s * perStep;
            int end   = Math.Min(start + perStep, total);
            for (int i = start; i < end; i++)
                AnimationEngine.DrawAt(allCells[i].Col, allCells[i].Row,
                    dissolveChar.ToString(), color);

            try { await Task.Delay(stepMs, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }
}
