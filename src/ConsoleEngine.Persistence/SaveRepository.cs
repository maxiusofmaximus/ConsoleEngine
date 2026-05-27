using System.Text.Json;

namespace ConsoleEngine.Persistence;

/// <summary>
/// Generic slot-based save system. Persists any serialisable <typeparamref name="T"/>
/// as JSON files inside a save directory.
///
/// <b>Usage</b>
/// <code>
/// var saves = new SaveRepository&lt;MyGameSession&gt;("Saves", s => s.SaveSlotId);
/// saves.Save(session);                          // writes Saves/{session.SaveSlotId}.json
/// MyGameSession loaded = saves.LoadMostRecent();
/// </code>
/// </summary>
public sealed class SaveRepository<T> where T : class
{
    private readonly string        _directory;
    private readonly Func<T, string> _slotIdSelector;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented           = true,
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="saveDirectory">Directory where save files are written (created if absent).</param>
    /// <param name="slotIdSelector">Extracts the slot ID from a save object; used as the filename.</param>
    public SaveRepository(string saveDirectory, Func<T, string> slotIdSelector)
    {
        ArgumentNullException.ThrowIfNull(saveDirectory);
        ArgumentNullException.ThrowIfNull(slotIdSelector);
        _directory       = saveDirectory;
        _slotIdSelector  = slotIdSelector;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Serialises <paramref name="data"/> and writes it to <c>{saveDirectory}/{slotId}.json</c>.</summary>
    public void Save(T data)
    {
        ArgumentNullException.ThrowIfNull(data);
        string slotId = _slotIdSelector(data);
        File.WriteAllText(SlotPath(slotId), JsonSerializer.Serialize(data, JsonOptions));
    }

    /// <summary>Loads the save file for <paramref name="slotId"/>.</summary>
    /// <exception cref="FileNotFoundException">Slot does not exist.</exception>
    public T Load(string slotId)
    {
        string path = SlotPath(slotId);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Save slot '{slotId}' not found.", path);

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
               ?? throw new InvalidOperationException($"Failed to deserialise save slot '{slotId}'.");
    }

    /// <summary>Loads the most recently written save file.</summary>
    /// <exception cref="InvalidOperationException">No save files exist.</exception>
    public T LoadMostRecent()
    {
        string[] files = Directory.GetFiles(_directory, "*.json");
        if (files.Length == 0)
            throw new InvalidOperationException("No save files found.");

        string newest = files.OrderByDescending(File.GetLastWriteTimeUtc).First();
        return JsonSerializer.Deserialize<T>(File.ReadAllText(newest), JsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialise the most recent save.");
    }

    /// <summary>Returns <see langword="true"/> if at least one save file exists.</summary>
    public bool HasAnySave() =>
        Directory.Exists(_directory) && Directory.GetFiles(_directory, "*.json").Length > 0;

    /// <summary>Enumerates all save slot IDs (filenames without extension).</summary>
    public IEnumerable<string> ListSlotIds() =>
        Directory.GetFiles(_directory, "*.json")
                 .Select(Path.GetFileNameWithoutExtension)
                 .Where(id => id is not null)
                 .Cast<string>();

    private string SlotPath(string slotId) =>
        Path.Combine(_directory, $"{slotId}.json");
}
