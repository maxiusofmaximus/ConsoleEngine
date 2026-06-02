# Rust roadmap — L2a result (integrated native ToAnsiBytes/WriteAnsi)

> Evidence-first. The L1 Rust buffer path is now a shipping API on `ConsoleEngine.Rendering`
> (`PixelArtRenderer.ToAnsiBytes` / `WriteAnsi`) with a managed fallback. Measured through the
> **integrated** API (not the PoC interop). BenchmarkDotNet, .NET 8.0.22, Core Ultra 7 265K.
> Correctness: native output is byte-identical to managed (tests + benchmark gate).

## Results
| Sprite | Method | Mean | vs C# | Allocated | vs C# |
|---:|---|---:|---:|---:|---:|
| 512²  | `Csharp_ToAnsiString` (baseline) | 5.175 ms | 1.00× | 18,713,788 B | 1.00 |
| 512²  | **`Native_ToAnsiBytes`** | **1.105 ms** | **0.21× (4.7× faster)** | **0 B** | **0.00** |
| 512²  | `Managed_ToAnsiBytes` (fallback) | 8.163 ms | 1.58× | 23,564,845 B | 1.26 |
| 1024² | `Csharp_ToAnsiString` (baseline) | 22.173 ms | 1.00× | 74,622,828 B | 1.00 |
| 1024² | **`Native_ToAnsiBytes`** | **4.503 ms** | **0.20× (4.9× faster)** | **2 B** | **~0.00** |
| 1024² | `Managed_ToAnsiBytes` (fallback) | 29.899 ms | 1.35× | 94,024,648 B | 1.26 |

## Findings
- **Native path (the goal): ~4.9× faster and ~100% less allocation** through the production API —
  reproduces the L1 win (eliminates the ~75 MB/op at 1024²). Clears the gate decisively.
- **Managed fallback is slower than `ToAnsiString`** (1.35–1.58×, +26% alloc): it wraps
  `Encoding.UTF8.GetBytes(ToAnsiString(...))`, so it pays the string cost *plus* a byte copy. It
  exists for **correctness** (the API must work with no native lib), **not** speed. Consumers that
  want the win need the native library present.

## Status & next (L2b)
- ✅ L2a done: native-accelerated byte/stream API integrated, byte-identical, managed fallback,
  270 tests green (native path exercised locally; fallback on CI), 0 warnings.
- ⏭️ **L2b (packaging)**: build `ce_render` per-RID (`win-x64`, `linux-x64`, `osx-arm64`,
  `linux-arm64`), add a `cargo` step to CI, and ship the natives under NuGet
  `runtimes/<rid>/native/` so downstream consumers get the fast path by default — the fallback then
  only covers RIDs without a native build. Re-measure on each OS.
