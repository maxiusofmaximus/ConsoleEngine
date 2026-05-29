using ConsoleEngine.Scenes;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class DialogueLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public DialogueLoaderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string Write(string name, string json)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Load_ValidDialogue_ReturnsDefinition()
    {
        string path = Write("test.dialogue.json", """
            {
              "schemaVersion": 1,
              "leftLabel": "Hero",
              "leftColor": "Cyan",
              "leftArt": ["  O  ", " /|\\ "],
              "rightLabel": "Merchant",
              "rightColor": "Yellow",
              "lines": ["Hero: Hello.", "Merchant: Hi."],
              "textColor": "Gray",
              "promptContinue": true
            }
            """);

        DialogueDefinition d = DialogueLoader.Load(path);

        Assert.Equal("Hero", d.LeftLabel);
        Assert.Equal(ConsoleColor.Cyan, d.LeftColor);
        Assert.Equal(2, d.LeftArt.Length);
        Assert.Equal("Merchant", d.RightLabel);
        Assert.Equal(2, d.Lines.Length);
        Assert.True(d.PromptContinue);
    }

    [Fact]
    public void Load_FutureSchemaVersion_ThrowsInvalidDataException()
    {
        string path = Write("future.dialogue.json", """
            { "schemaVersion": 999, "leftLabel": "X" }
            """);

        Assert.Throws<InvalidDataException>(() => DialogueLoader.Load(path));
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            DialogueLoader.Load(Path.Combine(_tempDir, "nope.dialogue.json")));
    }

    [Fact]
    public void TryLoad_MissingFile_ReturnsFalse()
    {
        bool ok = DialogueLoader.TryLoad(
            Path.Combine(_tempDir, "missing.dialogue.json"), out DialogueDefinition? d);

        Assert.False(ok);
        Assert.Null(d);
    }

    [Fact]
    public void Load_Defaults_WhenFieldsMissing()
    {
        string path = Write("minimal.dialogue.json", """
            { "schemaVersion": 1 }
            """);

        DialogueDefinition d = DialogueLoader.Load(path);

        Assert.Equal(string.Empty, d.LeftLabel);
        Assert.Empty(d.Lines);
        Assert.True(d.PromptContinue);
    }
}
