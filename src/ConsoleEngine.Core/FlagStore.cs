using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleEngine.Core;

/// <summary>
/// Typed, JSON-serialisable key-value store for game flags and progress variables.
/// Use instead of a raw <c>Dictionary&lt;string, object&gt;</c> — values are strongly typed
/// at the call site while remaining serialisable without reflection at the storage layer.
/// </summary>
/// <remarks>
/// <b>Usage</b>
/// <code>
/// var flags = new FlagStore();
/// flags.Set("player.name", "Ari");
/// flags.Set("chapter", 2);
/// flags.Set("visited.forest", true);
///
/// string name    = flags.Get&lt;string&gt;("player.name")!;
/// int    chapter = flags.Get&lt;int&gt;("chapter");
/// bool   visited = flags.Get&lt;bool&gt;("visited.forest");
///
/// // Persist:
/// string json = flags.ToJson();
/// var loaded  = FlagStore.FromJson(json);
/// </code>
/// </remarks>
public sealed class FlagStore
{
    private readonly Dictionary<string, JsonElement> _store =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        WriteIndented = true,
    };

    // ── Write ─────────────────────────────────────────────────────────────

    /// <summary>Sets (or replaces) the value at <paramref name="key"/>.</summary>
    public void Set<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _store[key] = JsonSerializer.SerializeToElement(value);
    }

    /// <summary>Removes the entry for <paramref name="key"/>. No-op if absent.</summary>
    public bool Remove(string key) => _store.Remove(key);

    /// <summary>Removes all entries.</summary>
    public void Clear() => _store.Clear();

    // ── Read ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the value at <paramref name="key"/> as <typeparamref name="T"/>,
    /// or <see langword="default"/> if the key is absent or the type cannot be converted.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (!_store.TryGetValue(key, out JsonElement el)) return default;
        try { return el.Deserialize<T>(); }
        catch (JsonException) { return default; }
    }

    /// <summary>
    /// Returns <see langword="true"/> and the typed value if <paramref name="key"/> exists;
    /// <see langword="false"/> otherwise.
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        if (_store.TryGetValue(key, out JsonElement el))
        {
            try
            {
                value = el.Deserialize<T>();
                return true;
            }
            catch (JsonException) { /* fall through */ }
        }
        value = default;
        return false;
    }

    /// <summary>Returns <see langword="true"/> if <paramref name="key"/> has been set.</summary>
    public bool Has(string key) => _store.ContainsKey(key);

    /// <summary>All keys currently stored.</summary>
    public IReadOnlyCollection<string> Keys => _store.Keys;

    /// <summary>Count of stored entries.</summary>
    public int Count => _store.Count;

    // ── Serialisation ─────────────────────────────────────────────────────

    /// <summary>Serialises the flag store to a JSON string for persistence.</summary>
    public string ToJson() =>
        JsonSerializer.Serialize(_store, SerializeOptions);

    /// <summary>
    /// Deserialises a <see cref="FlagStore"/> from a JSON string previously produced
    /// by <see cref="ToJson"/>.
    /// </summary>
    /// <exception cref="JsonException">The JSON is malformed.</exception>
    public static FlagStore FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                   ?? throw new JsonException("FlagStore JSON parsed as null.");

        var store = new FlagStore();
        foreach (var (k, v) in dict)
            store._store[k] = v;

        return store;
    }

    // ── DTO for SaveRepository integration ────────────────────────────────

    /// <summary>
    /// Raw backing dictionary for direct JSON serialisation via <see cref="System.Text.Json"/>.
    /// Prefer <see cref="ToJson"/> / <see cref="FromJson"/> for isolated persistence.
    /// </summary>
    [JsonInclude]
    public IReadOnlyDictionary<string, JsonElement> Data => _store;
}
