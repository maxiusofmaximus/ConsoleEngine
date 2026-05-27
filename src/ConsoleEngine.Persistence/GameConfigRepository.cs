using ConsoleEngine.Config;
using Newtonsoft.Json;

namespace ConsoleEngine.Persistence;

/// <summary>
/// Loads and saves <see cref="GameConfig"/> as JSON.
/// File path: <c>{configDirectory}/config.json</c>.
/// Creates a default config if the file is absent; normalises values on every load.
/// </summary>
public sealed class GameConfigRepository
{
    private readonly string _filePath;

    public GameConfigRepository(string configDirectory)
    {
        ArgumentNullException.ThrowIfNull(configDirectory);
        Directory.CreateDirectory(configDirectory);
        _filePath = Path.Combine(configDirectory, "config.json");
    }

    public GameConfig Load()
    {
        GameConfig config;
        try
        {
            if (!File.Exists(_filePath)) return CreateDefault();
            string json = File.ReadAllText(_filePath);
            config = JsonConvert.DeserializeObject<GameConfig>(json) ?? CreateDefault();
        }
        catch { config = CreateDefault(); }

        GameSettingsCatalog.NormalizeOnLoad(config);
        return config;
    }

    public void Save(GameConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        File.WriteAllText(_filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }

    private static GameConfig CreateDefault() => new();
}
