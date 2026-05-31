using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerminalBench;

/// <summary>
/// One scenario result. Serialized as a single line of JSON (JSON Lines / NDJSON).
///
/// Why JSON Lines over CSV or a single JSON array:
///  - Append-only: each run adds lines without rewriting prior history, so a crashed/partial
///    run can never corrupt earlier records (a single-array file must be rewritten each time).
///  - Self-contained lines: every record carries its full <see cref="RunMetadata"/>, so two
///    runs months apart on different machines are directly comparable with no external join.
///  - Nested metrics: latency distributions and GC sub-objects don't flatten cleanly into CSV.
///  - Tooling-friendly: greppable, tailable, and one `JsonSerializer.Deserialize` per line.
/// </summary>
public sealed record BenchRecord(
    [property: JsonPropertyName("schema")]   string Schema,
    [property: JsonPropertyName("scenario")] string Scenario,
    [property: JsonPropertyName("result")]   string Result,
    [property: JsonPropertyName("error")]    string? Error,
    [property: JsonPropertyName("params")]   IReadOnlyDictionary<string, object> Params,
    [property: JsonPropertyName("metrics")]  IReadOnlyDictionary<string, object> Metrics,
    [property: JsonPropertyName("env")]      RunMetadata Env);

/// <summary>
/// Writes <see cref="BenchRecord"/>s to a per-run <c>.jsonl</c> file under
/// <c>artifacts/benchmarks/terminal/</c> and prints the resolved path on close.
/// </summary>
public sealed class BenchLogger : IDisposable
{
    public const string SchemaId = "ce-terminal-bench/v1";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly RunMetadata _env;
    private readonly StreamWriter _writer;
    private readonly List<BenchRecord> _records = new();

    public string LogPath { get; }

    public BenchLogger(string outputDir, RunMetadata env)
    {
        _env = env;
        Directory.CreateDirectory(outputDir);
        LogPath = Path.Combine(outputDir, $"terminal-bench-{env.RunId}.jsonl");
        // UTF-8 *without* BOM: a BOM on the first line breaks strict JSON parsers reading the
        // file line-by-line (the first record would fail to deserialize).
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        _writer = new StreamWriter(LogPath, append: true, utf8NoBom) { AutoFlush = true };
    }

    public void Record(
        string scenario,
        string result,
        string? error,
        IReadOnlyDictionary<string, object> @params,
        IReadOnlyDictionary<string, object> metrics)
    {
        var rec = new BenchRecord(SchemaId, scenario, result, error, @params, metrics, _env);
        _records.Add(rec);
        _writer.WriteLine(JsonSerializer.Serialize(rec, JsonOpts));
    }

    /// <summary>Writes a sibling human-readable Markdown summary next to the JSONL log.</summary>
    public string WriteMarkdownSummary()
    {
        string mdPath = Path.ChangeExtension(LogPath, ".md");
        var sb = new StringBuilder();
        sb.AppendLine($"# Terminal backend benchmark — {_env.RunId}");
        sb.AppendLine();
        sb.AppendLine($"- **UTC**: {_env.TimestampUtc}");
        sb.AppendLine($"- **Platform**: {_env.OsPlatform} ({_env.OsArchitecture}) — {_env.OsDescription}");
        sb.AppendLine($"- **Process arch**: {_env.ProcessArchitecture} · **Cores**: {_env.LogicalCores}");
        sb.AppendLine($"- **Build**: {_env.BuildConfig} · **Framework**: {_env.Framework}");
        sb.AppendLine($"- **Git**: {_env.GitCommit} · **Machine**: {_env.MachineName}");
        sb.AppendLine();
        foreach (var r in _records)
        {
            sb.AppendLine($"## {r.Scenario} — {r.Result}");
            if (r.Error is not null) sb.AppendLine($"> error: {r.Error}");
            if (r.Params.Count > 0)
                sb.AppendLine("params: " + string.Join(", ", r.Params.Select(kv => $"{kv.Key}={kv.Value}")));
            sb.AppendLine();
            sb.AppendLine("| metric | value |");
            sb.AppendLine("|---|---|");
            foreach (var kv in r.Metrics)
                sb.AppendLine($"| {kv.Key} | {kv.Value} |");
            sb.AppendLine();
        }
        File.WriteAllText(mdPath, sb.ToString(), Encoding.UTF8);
        return mdPath;
    }

    public void Dispose() => _writer.Dispose();
}
