using System.Collections.Generic;

namespace ConsoleEngine.Core;

/// <summary>
/// Resolves localisation keys to translated strings.
/// English is the canonical source language.
/// All translations fall back to English when a key is missing.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Current language code (e.g. "en", "es"). Always lower-case ISO code.</summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Resolves a key to its translated string. If the key is missing in the current
    /// language, falls back to English. If missing in English too, returns the key
    /// wrapped in [brackets] so missing translations are visible during QA.
    /// </summary>
    string Resolve(string key);

    /// <summary>Resolves a key and applies positional formatting (string.Format semantics).</summary>
    string Resolve(string key, params object[] args);

    /// <summary>
    /// Switches the active language. Subsequent <see cref="Resolve(string)"/> calls reflect the new language.
    /// Falls back to English silently if the requested language is not loaded.
    /// </summary>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Languages currently loaded. Useful for QA validation
    /// (e.g. confirm every key has a Spanish translation).
    /// </summary>
    IReadOnlyCollection<string> AvailableLanguages { get; }

    /// <summary>
    /// Raised after the active language changes.
    /// The argument is the new language code (e.g. <c>"es"</c>).
    /// Subscribe to re-render any live UI when the player switches language.
    /// </summary>
    event Action<string>? LanguageChanged;
}
