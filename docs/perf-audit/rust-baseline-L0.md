# Rust roadmap — L0 baseline (PixelArtRenderer, .NET)

> Evidence-first baseline that any Rust PoC (L1) must beat. Source: existing
> `ConsoleEngine.Benchmarks/PixelArtRendererBenchmarks.cs` run with BenchmarkDotNet.
> `dotnet run -c Release --project src/ConsoleEngine.Benchmarks -- --filter "*PixelArt*" --job short`
> (ran both the full **.NET 8.0 Default** job and a ShortRun job; numbers below are the **Default**
> job — the rigorous ones). Re-run to refresh; do not hand-edit.

## Environment
BenchmarkDotNet v0.14.0 · Windows 11 (10.0.26200) · .NET 8.0.22, X64 RyuJIT AVX2 ·
Intel Core Ultra 7 265K · 2026-06-02.

## Measured baseline (Default job, mean time + allocation per op)

`ToAnsiString(Rgb[,])` — PNG/grid → half-block ANSI string (**the Rust candidate**):
| SpriteSize | Mean | Allocated/op |
|---:|---:|---:|
| 32   | 11.27 µs    | 78.2 KB |
| 64   | 83.13 µs    | 298.15 KB |
| 128  | 317.56 µs   | 1,164 KB |
| 256  | 1,025.2 µs  | 4,600 KB |
| 512  | 4,966.0 µs  | 18,275 KB |
| 1024 | 18,721.7 µs | **72,874 KB (~71 MB)** |

`ToAnsiString` with transparent color ≈ identical to the grid variant (1024 slightly higher,
~21.6 ms). `BuildSprite(string[], palette)` is much cheaper (alloc ratio ~0.07):
| SpriteSize | Mean | Allocated/op |
|---:|---:|---:|
| 256  | 420.4 µs   | 328 KB |
| 512  | 1,602.0 µs | 1,297 KB |
| 1024 | 6,287.4 µs | 5,153 KB |

## Findings
- **`ToAnsiString` is the hotspot** — both time and allocation scale ~quadratically with the
  sprite dimension (pixels = dim²). At 1024² it costs **~18.7 ms and allocates ~71 MB per call**,
  with heavy Gen0/1/2 GC activity (it builds large intermediate ANSI strings).
- **Allocation is the standout problem**, not just CPU: ~71 MB/op at 1024² is enormous and drives
  GC pauses. This is exactly what a native Rust implementation can crush (build into a single
  reused/pooled byte buffer, zero GC).
- `BuildSprite` is ~3× faster and allocates ~14× less than `ToAnsiString`; **not** a priority.
- `DrawAt` is Console-IO (not CPU) and is **not** a Rust candidate (see roadmap).

## L1 gate (the bar to beat)
A Rust `cdylib` `ToAnsiString` (consumed via P/Invoke behind the same C# API) is worth keeping
**only if** it clears, on these same inputs, **≥2× faster OR ≥50% less allocated** vs the table
above. Given the ~71 MB/op allocation, the **allocation win is the most promising** (a buffer-based
Rust path could plausibly cut it ≥90%). If it fails the gate, revert — the FFI/build complexity
isn't justified.

## Next (L1, only on approval)
Prototype `ToAnsiString` as a Rust `cdylib`, expose a narrow C ABI (pixels in → bytes out), call
via P/Invoke, feature-flag it, and re-run this exact benchmark to compare against L0.
