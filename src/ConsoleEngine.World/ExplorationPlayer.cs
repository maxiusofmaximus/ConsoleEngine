using ConsoleEngine.Locale;
using ConsoleEngine.Rendering;
using ConsoleEngine.Scenes;

namespace ConsoleEngine.World;

/// <summary>
/// Interactive exploration loop that renders a world location and processes player commands.
///
/// <b>Screen layout</b>
/// <code>
/// Row 0:    HUD — "  Location — Time of Day    Day N    [look] [exits] [menu]"
/// Row 1:    ─────────────────────────────────────────────────────────────────
/// Row 2+:   Location name, description, exits, message
///           └── ASCII art / sprite anchored at the bottom of the content area
/// Row H-2:  ─────────────────────────────────────────────────────────────────
/// Row H-1:  "  > _"  (command prompt)
/// </code>
///
/// <b>Built-in commands</b>
/// <list type="bullet">
///   <item>Cardinal directions: <c>n / north / s / south / e / east / w / west / u / up / d / down / in / out</c></item>
///   <item><c>go &lt;direction&gt;</c> — explicit move</item>
///   <item><c>look</c> (or <c>l</c>) — re-render current location</item>
///   <item><c>exits</c> — list available directions</item>
///   <item><c>wait</c> — advance time one period</item>
///   <item><c>help</c> (or <c>?</c>) — show command list</item>
///   <item><c>menu</c> (or <c>back</c>) — return <see cref="ExplorationResult.ReturnToMenu"/></item>
///   <item><c>quit</c> — return <see cref="ExplorationResult.Quit"/></item>
/// </list>
/// </summary>
public static class ExplorationPlayer
{
    // ── Direction alias table ─────────────────────────────────────────────────

    private static readonly Dictionary<string, string> DirAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["n"] = "north", ["s"] = "south", ["e"] = "east", ["w"] = "west",
            ["u"] = "up",    ["d"] = "down",
        };

    private static readonly HashSet<string> KnownDirections =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "north", "south", "east", "west", "up", "down", "in", "out", "enter",
        };

    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the interactive exploration loop.
    /// Blocks until the player types <c>menu</c> or <c>quit</c>.
    /// </summary>
    /// <param name="map">The world graph.</param>
    /// <param name="state">Current world state (mutated in-place as the player moves).</param>
    /// <param name="options">Display and behaviour options. <c>null</c> uses all defaults.</param>
    public static ExplorationResult Run(WorldMap map, WorldState state, ExplorationOptions? options = null)
    {
        options ??= new ExplorationOptions();
        string? message   = null;
        bool    firstEnter = true;

        while (true)
        {
            // Locate current position
            if (!map.TryGet(state.CurrentLocationId, out var location))
                throw new InvalidOperationException(
                    $"WorldState.CurrentLocationId '{state.CurrentLocationId}' was not found in the WorldMap.");

            // Notify listeners (scripted events, lock checks, encounters…)
            if (firstEnter)
            {
                options.OnLocationEnter?.Invoke(state, location);
                firstEnter = false;
            }

            // Render
            Render(location, state, options, message);
            message = null;

            // Prompt
            AnimationEngine.ShowCursor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            AnimationEngine.DrawAt(0, Console.WindowHeight - 1, "  > ");
            Console.ForegroundColor = ConsoleColor.White;
            try { Console.SetCursorPosition(4, Console.WindowHeight - 1); } catch { }

            string? raw = Console.ReadLine();
            Console.CursorVisible = false;

            string input = raw?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrEmpty(input)) continue;

            // Process
            var (cont, nextMsg, movedTo) = ProcessCommand(input, map, state, location, options);

            if (cont is ExplorationResult exitCode) return exitCode;

            if (movedTo != null)
            {
                // Apply move transition
                if (options.MoveTransition != TransitionType.Cut)
                    TransitionEngine.Out(options.MoveTransition, options.TransitionStepMs);

                state.CurrentLocationId = movedTo;
                message = nextMsg;
                options.OnLocationEnter?.Invoke(state, map.Get(movedTo));
                // firstEnter stays false; OnLocationEnter was already gated by it — reset not needed
            }
            else
            {
                message = nextMsg;
            }
        }
    }

    // ── Command processing ────────────────────────────────────────────────────

    /// <returns>
    /// (exitResult, message, newLocationId).
    /// exitResult != null → loop should end.
    /// newLocationId != null → player moved.
    /// </returns>
    private static (ExplorationResult? exit, string? msg, string? moveTo) ProcessCommand(
        string input,
        WorldMap map,
        WorldState state,
        LocationDefinition location,
        ExplorationOptions options)
    {
        // Expand direction aliases
        string cmd = DirAliases.TryGetValue(input, out string? expanded) ? expanded : input;

        // Strip "go " prefix
        if (cmd.StartsWith("go ", StringComparison.OrdinalIgnoreCase))
            cmd = cmd[3..].Trim();

        // ── Built-in commands ─────────────────────────────────────────────
        switch (cmd)
        {
            case "look" or "l":
                return (null, null, null); // just re-render

            case "exits":
                return (null, BuildExitLine(location), null);

            case "wait":
                state.AdvanceTime();
                return (null, CL.Get(CK.World.Waited), null);

            case "help" or "?":
                return (null, BuildHelp(location, state, options), null);

            case "menu" or "back":
                return (ExplorationResult.ReturnToMenu, null, null);

            case "quit":
                return (ExplorationResult.Quit, null, null);
        }

        // ── Movement ──────────────────────────────────────────────────────
        if (location.Exits.TryGetValue(cmd, out string? destId))
        {
            if (map.TryGet(destId, out _))
                return (null, null, destId);

            // Exit defined but destination missing from map (data error)
            return (null, CL.Get(CK.World.NoExit), null);
        }

        if (KnownDirections.Contains(cmd))
            return (null, CL.Get(CK.World.NoExit), null);

        // ── Custom actions ────────────────────────────────────────────────
        if (options.CustomActions != null)
        {
            foreach (var action in options.CustomActions)
            {
                if (!string.Equals(action.Command, cmd, StringComparison.OrdinalIgnoreCase)) continue;
                if (!action.IsAvailable(state, location)) continue;

                var outcome = action.Execute(state, location);
                if (outcome.ExitResult.HasValue)
                    return (outcome.ExitResult.Value, null, null);
                return (null, outcome.Message, null);
            }
        }

        // ── Unknown ───────────────────────────────────────────────────────
        return (null, CL.Get(CK.World.UnknownCmd, input), null);
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    private static void Render(
        LocationDefinition location,
        WorldState state,
        ExplorationOptions options,
        string? message)
    {
        Console.Clear();
        AnimationEngine.HideCursor();

        int winH = Console.WindowHeight;
        int winW = Console.WindowWidth;

        // ── HUD (rows 0-1) ────────────────────────────────────────────────
        string timeStr  = CL.Get(state.TimeOfDay.LocaleKey());
        string dayStr   = CL.Get(CK.Status.Day);
        string hudLeft  = $"  {location.Name} — {timeStr}    {dayStr} {state.Day}";
        string hudRight = $"  {options.HudHint}  ";
        int    hudGap   = Math.Max(1, winW - hudLeft.Length - hudRight.Length);
        string hud0     = hudLeft + new string(' ', hudGap) + hudRight;
        if (hud0.Length > winW) hud0 = hud0[..winW];

        AnimationEngine.DrawAt(0, 0, hud0, options.HudColor);
        AnimationEngine.DrawAt(0, 1, new string('─', winW), ConsoleColor.DarkGray);

        // ── Bottom border and prompt row (rows H-2 and H-1) ──────────────
        AnimationEngine.DrawAt(0, winH - 2, new string('─', winW), ConsoleColor.DarkGray);
        // Prompt is written after ReadLine — just blank it here
        AnimationEngine.DrawAt(0, winH - 1, new string(' ', winW));

        // ── Content area bounds ───────────────────────────────────────────
        int contentTop    = 2;
        int contentBottom = winH - 3; // last usable content row (above bottom border)

        // ── ASCII art (bottom-anchored in content area) ───────────────────
        if (location.AsciiArt is { Length: > 0 } art)
        {
            int artTopRow = contentBottom - art.Length + 1;
            for (int i = 0; i < art.Length; i++)
                AnimationEngine.DrawAt(0, Math.Max(contentTop, artTopRow + i), art[i], location.ArtColor);
        }

        // ── PNG sprite (right-aligned, feet at contentBottom) ─────────────
        if (!string.IsNullOrEmpty(location.SpritePath))
        {
            int spriteTopRow = contentBottom - location.SpriteRows + 1;
            int spriteCol    = winW - location.SpriteWidth - 1;
            PixelArtRenderer.RenderPng(
                location.SpritePath,
                Math.Max(0, spriteCol),
                Math.Max(contentTop, spriteTopRow));
        }

        // ── Text content (flows downward from contentTop) ─────────────────
        // Text area stops above the art to avoid overlap
        int artHeight    = location.AsciiArt?.Length ?? 0;
        int textBottom   = contentBottom - artHeight - 1; // 1 blank row before art

        int row = contentTop;

        // Location name
        AnimationEngine.DrawAt(2, row++, location.Name, options.TextColor);

        // Name underline
        int underLen = Math.Min(location.Name.Length, winW - 4);
        AnimationEngine.DrawAt(2, row++, new string('─', underLen), ConsoleColor.DarkGray);
        row++;

        // Description
        foreach (string line in location.Description)
        {
            if (row > textBottom) break;
            AnimationEngine.DrawAt(2, row++, line, options.TextColor);
        }

        // Exits
        if (options.ShowExits && row <= textBottom)
        {
            row++;
            string exits = BuildExitLine(location);
            AnimationEngine.DrawAt(2, row++, exits, options.ExitColor);
        }

        // Message (feedback from previous command)
        if (!string.IsNullOrEmpty(message) && row <= textBottom)
        {
            row++;
            AnimationEngine.DrawAt(2, row, message, options.MsgColor);
        }
    }

    // ── String builders ───────────────────────────────────────────────────────

    private static string BuildExitLine(LocationDefinition location)
    {
        if (location.Exits.Count == 0)
            return CL.Get(CK.World.NoExits);

        string label = CL.Get(CK.World.Exits);
        var parts = location.Exits.Keys.Select(dir => $"[{dir}]");
        return $"{label}: {string.Join("  ", parts)}";
    }

    private static string BuildHelp(
        LocationDefinition location, WorldState state, ExplorationOptions options)
    {
        var lines = new List<string>
        {
            $"look · exits · wait · help · menu · quit",
        };

        if (location.Exits.Count > 0)
            lines.Add($"  directions: {string.Join(", ", location.Exits.Keys)}");

        if (options.CustomActions != null)
        {
            var available = options.CustomActions
                .Where(a => a.IsAvailable(state, location))
                .Select(a => $"  {a.Command} — {a.Description}");
            lines.AddRange(available);
        }

        return string.Join(" | ", lines);
    }
}
