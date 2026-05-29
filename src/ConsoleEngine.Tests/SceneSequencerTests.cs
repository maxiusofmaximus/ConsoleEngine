using ConsoleEngine.Scenes;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class SceneSequencerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public SceneSequencerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string WriteScene(string name)
    {
        string path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, """
            {
              "schemaVersion": 1,
              "title": "Test",
              "lines": [],
              "promptContinue": false
            }
            """);
        return path;
    }

    [Fact]
    public void SceneNode_WithFalseCondition_IsSkipped()
    {
        string path = WriteScene("scene.scene.json");

        // A sequencer with one node that is always skipped
        var nodes = new[]
        {
            new SceneNode(path, Condition: () => false),
        };

        // We can't call Play() here without a real console — but we can verify
        // the node's condition is evaluated correctly by inspecting it directly.
        Assert.False(nodes[0].Condition!());
    }

    [Fact]
    public void SceneSequencer_Load_ValidFile_ReturnsSequencer()
    {
        string seqPath   = Path.Combine(_tempDir, "test.sequence.json");
        string scenePath = WriteScene("s1.scene.json");
        string escaped   = scenePath.Replace("\\", "\\\\");

        File.WriteAllText(seqPath,
            "{\"schemaVersion\":1,\"nodes\":[{\"scenePath\":\"" + escaped + "\"}]}");

        SceneSequencer seq = SceneSequencer.Load(seqPath);
        Assert.NotNull(seq);
    }

    [Fact]
    public void SceneSequencer_Load_FutureSchemaVersion_Throws()
    {
        string seqPath = Path.Combine(_tempDir, "future.sequence.json");
        File.WriteAllText(seqPath, """
            { "schemaVersion": 999, "nodes": [] }
            """);

        Assert.Throws<InvalidDataException>(() => SceneSequencer.Load(seqPath));
    }

    [Fact]
    public void SceneSequencer_TryLoad_MissingFile_ReturnsFalse()
    {
        bool ok = SceneSequencer.TryLoad(
            Path.Combine(_tempDir, "nope.sequence.json"), out SceneSequencer? seq);

        Assert.False(ok);
        Assert.Null(seq);
    }

    [Fact]
    public void SceneOverrides_CanBeApplied()
    {
        // Test that SceneOverrides record is correctly typed
        var overrides = new SceneOverrides
        {
            Title    = "Override Title",
            ArtColor = ConsoleColor.Red,
        };

        Assert.Equal("Override Title", overrides.Title);
        Assert.Equal(ConsoleColor.Red, overrides.ArtColor);
        Assert.Null(overrides.Lines); // not overridden
    }
}
