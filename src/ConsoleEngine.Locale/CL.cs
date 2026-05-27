using ConsoleEngine.Core;

namespace ConsoleEngine.Locale;

/// <summary>
/// Static façade for terminal localisation.
/// Wraps <see cref="ILocalizationService"/> so every Console-output site can call
/// <c>CL.Get(key)</c> without constructor injection.
///
/// <b>Usage</b>
/// <code>
/// // At startup — once, before any translated output:
/// CL.Initialize("path/to/GameData", "es");
///
/// // At any call site:
/// Console.WriteLine(CL.Get(CK.Menu.Title));
/// Console.WriteLine(CL.Get(CK.Greeting, playerName));
/// </code>
///
/// Graceful degradation: if not yet initialised (or locale files are absent),
/// <see cref="Get"/> returns the raw key so the game stays functional.
/// </summary>
/// <remarks>Named <c>CL</c> for brevity at call sites.</remarks>
public static class CL
{
    private static ILocalizationService? _service;

    /// <summary>
    /// Loads locale tables from <c>{gameDataRoot}/locale/</c> and activates the
    /// requested language. Safe to call multiple times — last call wins (hot-reload).
    /// Falls back silently on any I/O or parse error.
    /// </summary>
    /// <param name="gameDataRoot">
    /// Directory that contains a <c>locale/</c> sub-folder with
    /// <c>en.md</c> and optional <c>{lang}.md</c> files.
    /// </param>
    /// <param name="language">ISO language code, e.g. <c>"en"</c>, <c>"es"</c>.</param>
    public static void Initialize(string gameDataRoot, string language = "en")
    {
        try
        {
            string localeDir = Path.Combine(gameDataRoot, "locale");
            string enPath    = Path.Combine(localeDir, "en.md");
            if (!File.Exists(enPath)) return;

            var enTable = MarkdownLocalizationLoader.Parse("en", File.ReadAllText(enPath));
            var service = new InMemoryLocalizationService(enTable);

            string lang = language?.Trim().ToLowerInvariant() ?? "en";
            if (lang != "en")
            {
                string langPath = Path.Combine(localeDir, $"{lang}.md");
                if (File.Exists(langPath))
                    service.LoadTable(MarkdownLocalizationLoader.Parse(lang, File.ReadAllText(langPath)));
            }

            service.SetLanguage(lang);
            _service = service;
        }
        catch { /* Non-fatal — raw keys appear as fallback. */ }
    }

    /// <summary>Returns the translated string for <paramref name="key"/>.</summary>
    public static string Get(string key) => _service?.Get(key) ?? key;

    /// <summary>Returns the translated, formatted string for <paramref name="key"/>.</summary>
    public static string Get(string key, params object[] args) => _service?.Get(key, args) ?? key;

    /// <summary>Current active language code, or <c>"en"</c> if not initialised.</summary>
    public static string CurrentLanguage => _service?.CurrentLanguage ?? "en";
}
