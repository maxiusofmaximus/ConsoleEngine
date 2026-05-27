using Newtonsoft.Json;

namespace ConsoleEngine.Persistence;

/// <summary>
/// Generic slot-based save system. Persists any serialisable <typeparamref name="T"/>
/// as JSON files inside <paramref name="saveDirectory"/>.
///
/// <b>Usage</b>
/// <code>
/// var saves = new SaveRepository&lt;MyGameSession&gt;("Saves");
/// saves.Save(session);                          // writes Saves/{session.SaveSlotId}.json
/// MyGameSession loaded = saves.LoadMostRecent();
/// </code>
/// </summary>
public sealed class SaveRepository<T> where T : class
{
    private readonly string _directory;
    private readonly Func<T, string> _slotIdSelector;

    /// <param name="saveDirectory">Directory where save files are written.</param>
    /// <param name="slotIdSelector">Function that extracts the slot ID from a save object.</param>
    public SaveRepository(string saveDirectory, Func<T, string> slotIdSelector)
    {
        ArgumentNullException.ThrowIfNull(saveDirectory);
        ArgumentNullException.ThrowIfNull(slotIdSelector);
        _directory      = saveDirectory;
        _slotIdSelector = slotIdSelector;
        Directory.CreateDirectory(_directory);
    }

    public void Save(T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        string slotId = _slotIdSelector(data);
        string path   = SlotPath(slotId);
        File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
    }

    public T Load(string slotId)
    {
        string path = SlotPath(slotId);
        if (!File.Exists(path)) throw new FileNotFoundException($"Save slot '{slotId}' not found.", path);
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json)
               ?? throw new InvalidOperationException($"Failed to deserialise save slot '{slotId}'.");
    }

    public T LoadMostRecent()
    {
        string[] files = Directory.GetFiles(_directory, "*.json");
        if (files.Length == 0) throw new InvalidOperationException("No save files found.");
        string newest = files.OrderByDescending(File.GetLastWriteTimeUtc).First();
        string json   = File.ReadAllText(newest);
        return JsonConvert.DeserializeObject<T>(json)
               ?? throw new InvalidOperationException("Failed to deserialise the most recent save.");
    }

    public bool HasAnySave() =>
        Directory.Exists(_directory) && Directory.GetFiles(_directory, "*.json").Length > 0;

    public IEnumerable<string> ListSlotIds() =>
        Directory.GetFiles(_directory, "*.json").Select(f => Path.GetFileNameWithoutExtension(f));

    private string SlotPath(string slotId) =>
        Path.Combine(_directory, $"{slotId}.json");
}
