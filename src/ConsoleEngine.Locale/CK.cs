namespace ConsoleEngine.Locale;

/// <summary>
/// Base localisation key constants for ConsoleEngine.
/// Naming convention: <c>area.specific</c> — all lower-case with dots.
///
/// Game projects extend this class with their own keys:
/// <code>
/// public static class MyGameKeys : CK
/// {
///     public static class World
///     {
///         public const string ForestName = "world.forest.name";
///     }
/// }
/// </code>
///
/// All keys must have a matching entry in <c>GameData/locale/en.md</c>.
/// </summary>
public static class CK
{
    // ── Main menu ─────────────────────────────────────────────────────────────

    public static class Menu
    {
        public const string Title         = "cli.menu.title";
        public const string NewGame       = "cli.menu.new_game";
        public const string Continue      = "cli.menu.continue";
        public const string Options       = "cli.menu.options";
        public const string Exit          = "cli.menu.exit";
        public const string InvalidOption = "cli.menu.invalid_option";
    }

    // ── In-session ───────────────────────────────────────────────────────────

    public static class Session
    {
        public const string Hint        = "cli.session.hint";
        public const string SavedMenu   = "cli.session.saved_menu";
        public const string SavedQuit   = "cli.session.saved_quit";
        public const string SavedSlot   = "cli.session.saved_slot";    // {0} = slot id
        public const string BackWalk    = "cli.session.back_walk";
        public const string BackOptions = "cli.session.back_options";
    }

    // ── Options shell ────────────────────────────────────────────────────────

    public static class Options
    {
        public const string Title        = "cli.options.title";
        public const string Hint1        = "cli.options.hint1";
        public const string Hint2        = "cli.options.hint2";
        public const string MissingValue = "cli.options.missing_value";
        public const string Saved        = "cli.options.saved";
        public const string Unknown      = "cli.options.unknown";
        public const string UseSet       = "cli.options.use_set";
        public const string UpdateFailed = "cli.options.update_failed";  // {0} = error message
    }

    // ── Status header ────────────────────────────────────────────────────────

    public static class Status
    {
        public const string Title      = "cli.status.title";
        public const string Player     = "cli.status.player";
        public const string Location   = "cli.status.location";
        public const string Squad      = "cli.status.squad";
        public const string Day        = "cli.status.day";
        public const string SquadUnit  = "cli.status.squad_unit";
        public const string SquadUnits = "cli.status.squad_units";
        public const string Morning    = "cli.status.morning";
        public const string Afternoon  = "cli.status.afternoon";
        public const string Evening    = "cli.status.evening";
        public const string Night      = "cli.status.night";
    }

    // ── Prompts ───────────────────────────────────────────────────────────────

    public static class Prompt
    {
        public const string Name     = "cli.prompt.name";      // {0} = default name
        public const string Continue = "cli.prompt.continue";
        public const string Begin    = "cli.prompt.begin";
    }

    // ── Walk / exploration HUD ────────────────────────────────────────────────

    public static class Walk
    {
        public const string Hud = "cli.walk.hud"; // {0} = location name, {1} = time of day
    }

    // ── World / exploration ───────────────────────────────────────────────────

    public static class World
    {
        public const string Exits      = "cli.world.exits";
        public const string NoExits    = "cli.world.no_exits";
        public const string NoExit     = "cli.world.no_exit";      // "Nothing that way."
        public const string Waited     = "cli.world.waited";       // "Time passes..."
        public const string UnknownCmd = "cli.world.unknown_cmd";  // {0} = typed command
    }

    // ── Errors ───────────────────────────────────────────────────────────────

    public static class Errors
    {
        public const string GameDataNotFound = "cli.error.gamedata_not_found"; // {0} = path
        public const string ContinueFailed   = "cli.error.continue_failed";   // {0} = message
        public const string UiBuildNotFound  = "cli.error.ui_build_not_found";// {0} = path
        public const string UiBuildHint      = "cli.error.ui_build_hint";
        public const string OpeningUi        = "cli.error.opening_ui";        // {0} = game name
        public const string RepoNotFound     = "cli.error.repo_not_found";
    }
}
