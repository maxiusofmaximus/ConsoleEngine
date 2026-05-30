using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ConsoleEngine.Locale;
using ConsoleEngine.Scenes;

namespace ConsoleEngine.Benchmarks;

/// <summary>
/// Measures JSON/Markdown loading at realistic and stress scales (1 → 1000 files/entries).
/// Detects O(n²) regressions by scaling params x10 and watching if time grows x100.
/// Run with: dotnet run -c Release -- --filter "*JsonLoader*"
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class JsonLoaderBenchmarks
{
    [Params(1, 10, 100, 1000)]
    public int Count { get; set; }

    private string   _sceneDir    = null!;
    private string   _dialogueDir = null!;
    private string   _bigMarkdown = null!;
    private string[] _sceneFiles  = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sceneDir    = Path.Combine(Path.GetTempPath(), $"ce_bench_scenes_{Count}_{Guid.NewGuid():N}");
        _dialogueDir = Path.Combine(Path.GetTempPath(), $"ce_bench_dialogues_{Count}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_sceneDir);
        Directory.CreateDirectory(_dialogueDir);

        for (int i = 0; i < Count; i++)
        {
            File.WriteAllText(Path.Combine(_sceneDir, $"scene_{i}.scene.json"),
                BuildSceneJson($"Scene {i}", lineCount: 10, artRows: 5));
            File.WriteAllText(Path.Combine(_dialogueDir, $"dlg_{i}.dialogue.json"),
                BuildDialogueJson($"Speaker_{i}", lineCount: 5));
        }

        _sceneFiles  = Directory.GetFiles(_sceneDir, "*.scene.json");
        _bigMarkdown = BuildMarkdown("en", Count);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_sceneDir))    Directory.Delete(_sceneDir, recursive: true);
        if (Directory.Exists(_dialogueDir)) Directory.Delete(_dialogueDir, recursive: true);
    }

    [Benchmark(Baseline = true)]
    public void SceneLoader_LoadAll()
    {
        foreach (string f in _sceneFiles)
            SceneLoader.Load(f);
    }

    [Benchmark]
    public void MarkdownLocalizationLoader_Parse()
    {
        MarkdownLocalizationLoader.Parse("en", _bigMarkdown);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildSceneJson(string title, int lineCount, int artRows)
    {
        var lines = Enumerable.Range(0, lineCount).Select(i => $"Line {i} text content here.");
        // Use only JSON-safe characters in art strings (no backslash sequences)
        var art   = Enumerable.Range(0, artRows).Select(_ => "  ^^^  ");
        return $$"""
        {
          "title": "{{title}}",
          "lines": [{{string.Join(", ", lines.Select(l => $"\"{l}\""))}}],
          "asciiArt": [{{string.Join(", ", art.Select(a => $"\"{a}\""))}}],
          "artColor": "DarkGreen",
          "textColor": "Gray",
          "promptContinue": false
        }
        """;
    }

    private static string BuildDialogueJson(string speaker, int lineCount)
    {
        var lines = Enumerable.Range(0, lineCount).Select(i => $"{{\"speaker\":\"{speaker}\",\"text\":\"Line {i}\"}}");
        return $$"""{"lines": [{{string.Join(", ", lines)}}]}""";
    }

    private static string BuildMarkdown(string lang, int entryCount)
    {
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(ic, $"## {lang}");
        sb.AppendLine();
        sb.AppendLine("| Key | Value |");
        sb.AppendLine("|-----|-------|");
        for (int i = 0; i < entryCount; i++)
            sb.AppendLine(ic, $"| key.{i} | Value number {i} |");
        return sb.ToString();
    }
}
