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

    [Fact]
    public void TryLoad_ValidFile_ReturnsTrueAndDefinition()
    {
        string path = Write("ok.dialogue.json", """
            { "schemaVersion": 1, "leftLabel": "Hero", "lines": ["Hi."] }
            """);

        bool ok = DialogueLoader.TryLoad(path, out DialogueDefinition? d);

        Assert.True(ok);
        Assert.NotNull(d);
        Assert.Equal("Hero", d!.LeftLabel);
    }

    [Fact]
    public void Load_CaseInsensitiveColorNames_Work()
    {
        string path = Write("casecolor.dialogue.json", """
            { "schemaVersion": 1, "leftColor": "CYAN", "rightColor": "yellow", "textColor": "darkgreen" }
            """);

        DialogueDefinition d = DialogueLoader.Load(path);

        Assert.Equal(ConsoleColor.Cyan,      d.LeftColor);
        Assert.Equal(ConsoleColor.Yellow,    d.RightColor);
        Assert.Equal(ConsoleColor.DarkGreen, d.TextColor);
    }

    [Fact]
    public void Load_LegacyWithoutSchemaVersion_Succeeds()
    {
        string path = Write("legacy.dialogue.json", """
            { "leftLabel": "Old", "lines": ["A line."] }
            """);

        DialogueDefinition d = DialogueLoader.Load(path);
        Assert.Equal("Old", d.LeftLabel);
        Assert.Single(d.Lines);
    }

    [Fact]
    public void Load_UnknownColorName_FallsBackToDefault()
    {
        string path = Write("badcolor.dialogue.json", """
            { "schemaVersion": 1, "leftColor": "NotAColor", "rightColor": "AlsoNotAColor" }
            """);

        DialogueDefinition d = DialogueLoader.Load(path);

        Assert.Equal(ConsoleColor.DarkCyan,   d.LeftColor);
        Assert.Equal(ConsoleColor.DarkYellow, d.RightColor);
    }

    [Fact]
    public void Load_LeftSpritePath_IsPreserved()
    {
        string path = Write("sprite.dialogue.json", """
            { "schemaVersion": 1, "leftSpritePath": "chars/hero.png" }
            """);

        DialogueDefinition d = DialogueLoader.Load(path);
        Assert.Equal("chars/hero.png", d.LeftSpritePath);
    }
}
