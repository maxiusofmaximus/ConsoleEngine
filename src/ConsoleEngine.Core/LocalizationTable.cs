using System;
using System.Collections.Generic;

namespace ConsoleEngine.Core;

/// <summary>
/// Immutable key → string mapping for a single language.
/// Reload requires building a new table and calling <see cref="ILocalizationService.SetLanguage"/>.
/// </summary>
public sealed class LocalizationTable
{
    private readonly Dictionary<string, string> _entries;

    public LocalizationTable(string languageCode, IReadOnlyDictionary<string, string> entries)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("A valid language code is required.", nameof(languageCode));

        ArgumentNullException.ThrowIfNull(entries);

        LanguageCode = languageCode.Trim().ToLowerInvariant();
        _entries = new Dictionary<string, string>(entries.Count, StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.Key))
                _entries[entry.Key] = entry.Value ?? string.Empty;
        }
    }

    /// <summary>ISO language code, e.g. "en", "es", "zh-hans".</summary>
    public string LanguageCode { get; }

    /// <summary>Number of entries in this table.</summary>
    public int Count => _entries.Count;

    /// <summary>All entries as a read-only dictionary.</summary>
    public IReadOnlyDictionary<string, string> Entries => _entries;

    /// <summary>Attempts to resolve a key. Returns false when the key is absent.</summary>
    public bool TryGet(string key, out string value) =>
        _entries.TryGetValue(key, out value!);
}
