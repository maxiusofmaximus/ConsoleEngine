using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleEngine.Core;

namespace ConsoleEngine.Locale;

/// <summary>
/// Default <see cref="ILocalizationService"/> implementation.
/// Holds localisation tables in memory; resolves keys through the active language,
/// falls back to English, then to a bracketed missing-key marker so QA spots gaps immediately.
/// </summary>
/// <remarks>
/// English (<c>"en"</c>) is required. Other languages are optional and can be added at
/// runtime via <see cref="LoadTable"/> — useful for hot-reload.
/// Pure C#; no UnityEngine or platform dependencies.
/// </remarks>
public sealed class InMemoryLocalizationService : ILocalizationService
{
    public const string CanonicalLanguage = "en";

    private readonly Dictionary<string, LocalizationTable> _tables =
        new(StringComparer.OrdinalIgnoreCase);

    private string _currentLanguage = CanonicalLanguage;

    public InMemoryLocalizationService(LocalizationTable englishTable, params LocalizationTable[] otherTables)
    {
        ArgumentNullException.ThrowIfNull(englishTable);

        if (!string.Equals(englishTable.LanguageCode, CanonicalLanguage, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"The canonical table must be language '{CanonicalLanguage}', not '{englishTable.LanguageCode}'.",
                nameof(englishTable));

        _tables[CanonicalLanguage] = englishTable;

        if (otherTables is null) return;
        foreach (LocalizationTable table in otherTables)
            if (table is not null)
                _tables[table.LanguageCode] = table;
    }

    public string CurrentLanguage  => _currentLanguage;
    public IReadOnlyCollection<string> AvailableLanguages => _tables.Keys;

    /// <summary>Adds or replaces the table for a language. Enables hot-reload.</summary>
    public void LoadTable(LocalizationTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        _tables[table.LanguageCode] = table;
    }

    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("A valid language code is required.", nameof(languageCode));

        string normalized = languageCode.Trim().ToLowerInvariant();
        _currentLanguage = _tables.ContainsKey(normalized) ? normalized : CanonicalLanguage;
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "[]";

        if (_tables.TryGetValue(_currentLanguage, out LocalizationTable? current)
            && current.TryGet(key, out string? value)
            && !string.IsNullOrEmpty(value))
            return value;

        // Fallback to English.
        if (!string.Equals(_currentLanguage, CanonicalLanguage, StringComparison.OrdinalIgnoreCase)
            && _tables.TryGetValue(CanonicalLanguage, out LocalizationTable? english)
            && english.TryGet(key, out string? enValue)
            && !string.IsNullOrEmpty(enValue))
            return enValue;

        return $"[{key}]"; // Deliberately ugly so QA spots it.
    }

    public string Get(string key, params object[] args)
    {
        string raw = Get(key);
        if (args is null || args.Length == 0) return raw;
        try { return string.Format(CultureInfo.InvariantCulture, raw, args); }
        catch (FormatException) { return $"[fmt:{key}]"; }
    }
}
