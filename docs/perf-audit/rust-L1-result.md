# Rust roadmap — L1 result (ToAnsiString cdylib PoC)

> Evidence-first. Rust `cdylib` (`native/ce_render`) vs the C# baseline, measured with
> BenchmarkDotNet against the **L0** bar (`rust-baseline-L0.md`). Correctness gate passed: the Rust
> output is **byte-identical** to C# (`Encoding.UTF8.GetString` of the Rust bytes == the C# string),
> validated for the all-solid path plus transparent / top-only / bottom-only / odd-height branches.

## Setup
- Rust crate `native/ce_render` — `ce_pixels_to_ansi` writes UTF-8 half-block ANSI bytes straight
  into a caller buffer (no Rust-side allocation; `panic = "abort"`, `lto`, `opt-level=3`).
- C# interop `src/ConsoleEngine.Benchmarks/RustRender.cs` (P/Invoke, `NativeLibrary` resolver):
  - `Rust_ToAnsiString` — drop-in: Rust fills a temp buffer, then one `GetString`.
  - `Rust_ToAnsiBytes_ReuseBuffer` — zero-alloc: Rust fills a **reused** buffer, returns length.
- Env: BenchmarkDotNet v0.14, .NET 8.0.22, Core Ultra 7 265K, 5 iters / 3 warmups.

## Results
| Sprite | Method | Mean | vs C# | Allocated | vs C# |
|---:|---|---:|---:|---:|---:|
| 512²  | C# `ToAnsiString` (baseline) | 4,040.8 µs | 1.00× | 18,713,788 B | 1.00 |
| 512²  | Rust string (drop-in) | 3,422.4 µs | 0.85× | 14,553,758 B | 0.78 |
| 512²  | **Rust bytes (reused buffer)** | **770.0 µs** | **0.19× (5.2× faster)** | **0 B** | **0.00** |
| 1024² | C# `ToAnsiString` (baseline) | 18,367.4 µs | 1.00× | 74,622,842 B | 1.00 |
| 1024² | Rust string (drop-in) | 13,011.3 µs | 0.71× | 58,205,390 B | 0.78 |
| 1024² | **Rust bytes (reused buffer)** | **3,104.0 µs** | **0.17× (5.9× faster)** | **2 B** | **~0.00** |

## Verdict vs the L1 gate (≥2× time OR ≥50% allocation)
- **Zero-alloc buffer path: PASSES decisively** — **5.9× faster AND ~100% less allocation** at
  1024² (eliminates the ~75 MB/op). Clears *both* criteria by a wide margin.
- **Drop-in string variant: does NOT clear the gate on its own** — ~22% less alloc, ~15-29% faster.
  Its ceiling is the **mandatory final-string allocation** (a large UTF-16 string ≈ its own size).

## Key insight
The decisive win is **not materializing the giant string**, not "Rust vs C#" per se. The real
consumer path (`PixelArtRenderer.RenderCore`) **writes the ANSI to the console** — it can consume
the bytes directly and skip the string entirely. So the value lands when the API exposes a
byte/stream path, which Rust makes both allocation-free and ~6× faster.

## Recommendation → L2
1. Add a real **byte/stream API** to `ConsoleEngine.Rendering` (e.g. `ToAnsiBytes(Rgb[,], Span<byte>)`
   or `WriteAnsi(Rgb[,], Stream)`), backed by the Rust core, **behind a feature flag**, and make
   `RenderCore` write bytes straight to `Console.OpenStandardOutput()` (no string). Keep the
   string-returning API as the managed fallback / for callers that need a string.
2. **Packaging:** build `ce_render` per-RID (`win-x64`, `linux-x64`, `osx-arm64`, `linux-arm64`)
   and ship the natives under the NuGet `runtimes/<rid>/native/` layout; add a `cargo` step to CI.
   Provide a managed fallback when the native lib is absent (so the package still works everywhere).
3. Re-measure each step against L0/L1.

The L1 gate is **met** — the Rust buffer path is worth pursuing.
