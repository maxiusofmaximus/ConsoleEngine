# TerminalBench

Evidence-first benchmark & stress harness for `ConsoleEngine.Terminal` (the cross-platform
PTY backend). It measures the real backend through a **real console host** and writes
structured, append-only logs so runs are comparable across machines and dates without
copy-pasting console output.

## Why a standalone harness (not BenchmarkDotNet / xUnit)

- **ConPTY only renders content in a real console host.** Under the vstest host (or a
  redirected/headless process) ConPTY delivers lifecycle VT but not rendered child output —
  so throughput/round-trip must run from a console, which a plain `dotnet run` in a terminal
  provides and a test host does not.
- PTY throughput is an **I/O integration** measurement (background reader threads, pipe/fd
  draining), not a CPU micro-benchmark — wall-clock MB/s and alloc-per-byte are the right
  units, and we want them logged with full environment context for historical diffing.

## Run

```bash
# Full baseline (publishable). Run from a real terminal, NOT a test host.
dotnet run -c Release --project tools/TerminalBench -- full

# Quick smoke of the harness itself (smaller iteration counts)
dotnet run -c Release --project tools/TerminalBench -- quick
```

On Windows, run it in **Windows Terminal / PowerShell / cmd** directly (or via Claude's `!`
prefix) so ConPTY content flows. A redirected/piped invocation will record
`throughput`/`concurrent_stress` as `"no_output"` and `write_roundtrip` as `partial` — that
status is the harness telling you content never flowed, not a real measurement.

## Output

Logs land in `artifacts/benchmarks/terminal/`:

- `terminal-bench-<runId>.jsonl` — one JSON object per scenario (see schema below)
- `terminal-bench-<runId>.md`   — human-readable summary of the same run

The resolved paths are printed when the harness finishes.

### Why JSON Lines

- **Append-only**: each run is a new file; a crash mid-run can't corrupt prior history
  (a single JSON array would have to be rewritten each time).
- **Self-contained lines**: every record embeds the full `env` block (platform, arch, build
  config, framework, git commit, cores), so two runs months apart on different machines are
  directly comparable with no external join.
- **Nested metrics**: latency distributions and GC sub-objects don't flatten cleanly to CSV.
- **Tooling-friendly**: greppable, tailable, one `JsonSerializer.Deserialize` per line.

### Record schema (`ce-terminal-bench/v1`)

```jsonc
{
  "schema":   "ce-terminal-bench/v1",
  "scenario": "throughput",
  "result":   "ok",            // ok | partial | no_output | error
  "error":    null,
  "params":   { "lines": 20000 },
  "metrics":  { "total_bytes": ..., "throughput_mb_s": ..., "alloc_per_byte": ..., "gc_gen0": ... },
  "env":      { "RunId": ..., "OsPlatform": ..., "OsArchitecture": ..., "BuildConfig": ..., "GitCommit": ... }
}
```

## Scenarios

| scenario | measures | host requirement |
|---|---|---|
| `startup_latency`   | spawn → exit lifecycle latency + alloc/spawn | any |
| `throughput`        | sustained read MB/s, chunk size, alloc/byte  | **console** |
| `write_roundtrip`   | write → echo round-trip latency              | **console** |
| `resize_latency`    | resize syscall latency (no child re-layout wait) | any |
| `dispose_latency`   | DisposeAsync teardown latency                | any |
| `concurrent_stress` | N concurrent sessions, aggregate throughput + stability | **console** |

`startup_latency`, `resize_latency`, and `dispose_latency` are valid in any host; the
content-dependent scenarios need a console.
