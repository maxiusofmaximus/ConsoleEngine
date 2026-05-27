using System.Text.Json;
using ConsoleEngine.Config;

namespace ConsoleEngine.Persistence;

/// <summary>
/// Loads and saves <see cref="GameConfig"/> as JSON.
/// File path: <c>{configDirectory}/config.json</c>.
/// Creates a default config if the file is absent; normalises values on every load.
/// </summary>
public sealed class GameConfigRepository
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented           = true,
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="configDirectory">Directory that contains <c>config.json</c> (created if absent).</param>
    public GameConfigRepository(string configDirectory)
    {
        ArgumentNullException.ThrowIfNull(configDirectory);
        Directory.CreateDirectory(configDirectory);
        _filePath = Path.Combine(configDirectory, "config.json");
    }

    /// <summary>
    /// Loads <c>config.json</c>, normalises values, and returns the result.
    /// Returns a default config if the file is missing or corrupt.
    /// </summary>
    public GameConfig Load()
    {
        GameConfig config;
        try
        {
            if (!File.Exists(_filePath)) return CreateDefault();
            config = JsonSerializer.Deserialize<GameConfig>(File.ReadAllText(_filePath), JsonOptions)
                     ?? CreateDefault();
        }
        catch { config = CreateDefault(); }

        GameSettingsCatalog.NormalizeOnLoad(config);
        return config;
    }

    /// <summary>Serialises <paramref name="config"/> to <c>config.json</c>.</summary>
    public void Save(GameConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    private static GameConfig CreateDefault() => new();
}
