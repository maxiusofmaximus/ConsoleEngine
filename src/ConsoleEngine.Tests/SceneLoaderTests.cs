using ConsoleEngine.Scenes;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class SceneLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public SceneLoaderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string Write(string name, string json)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Load_ValidScene_ReturnsDefinition()
    {
        string path = Write("test.scene.json", """
            {
              "schemaVersion": 1,
              "title": "Hello",
              "lines": ["Line 1", "Line 2"],
              "artColor": "DarkGreen",
              "textColor": "Gray",
              "promptContinue": true
            }
            """);

        SceneDefinition scene = SceneLoader.Load(path);

        Assert.Equal("Hello", scene.Title);
        Assert.Equal(2, scene.Lines.Length);
        Assert.Equal(ConsoleColor.DarkGreen, scene.ArtColor);
        Assert.True(scene.PromptContinue);
    }

    [Fact]
    public void Load_LegacySceneWithoutSchemaVersion_Succeeds()
    {
        string path = Write("legacy.scene.json", """
            { "title": "Old", "lines": [] }
            """);

        SceneDefinition scene = SceneLoader.Load(path);
        Assert.Equal("Old", scene.Title);
    }

    [Fact]
    public void Load_FutureSchemaVersion_ThrowsInvalidDataException()
    {
        string path = Write("future.scene.json", """
            { "schemaVersion": 999, "title": "Future" }
            """);

        Assert.Throws<InvalidDataException>(() => SceneLoader.Load(path));
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            SceneLoader.Load(Path.Combine(_tempDir, "nonexistent.scene.json")));
    }

    [Fact]
    public void TryLoad_ValidScene_ReturnsTrueAndScene()
    {
        string path = Write("ok.scene.json", """
            { "schemaVersion": 1, "title": "OK", "lines": [] }
            """);

        bool result = SceneLoader.TryLoad(path, out SceneDefinition? scene);

        Assert.True(result);
        Assert.NotNull(scene);
        Assert.Equal("OK", scene!.Title);
    }

    [Fact]
    public void TryLoad_MissingFile_ReturnsFalse()
    {
        bool result = SceneLoader.TryLoad(
            Path.Combine(_tempDir, "missing.scene.json"), out SceneDefinition? scene);

        Assert.False(result);
        Assert.Null(scene);
    }

    [Fact]
    public void Load_UnknownColorName_FallsBackToDefault()
    {
        string path = Write("badcolor.scene.json", """
            { "artColor": "NotAColor", "textColor": "AlsoNotAColor" }
            """);

        SceneDefinition scene = SceneLoader.Load(path);

        Assert.Equal(ConsoleColor.DarkGray, scene.ArtColor);
        Assert.Equal(ConsoleColor.Gray, scene.TextColor);
    }

    [Fact]
    public void Load_CaseInsensitiveColorName_Works()
    {
        string path = Write("casecolor.scene.json", """
            { "artColor": "darkgreen", "textColor": "CYAN" }
            """);

        SceneDefinition scene = SceneLoader.Load(path);

        Assert.Equal(ConsoleColor.DarkGreen, scene.ArtColor);
        Assert.Equal(ConsoleColor.Cyan, scene.TextColor);
    }
}
