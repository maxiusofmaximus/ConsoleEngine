# ConsoleEngine — Rust migration roadmap (research only; no rewrite code yet)

> Evidence-first. This document evaluates **where Rust helps, how to adopt it progressively, and
> the measured gates** that must be cleared before each step. It deliberately contains **no rewrite
> code** — the first executable step is establishing a measured .NET baseline (Level 0).

## Goal & principles
- Improve raw performance/efficiency of the CPU- and allocation-heavy parts of ConsoleEngine
  **without breaking the .NET/NuGet line** that consumers (e.g. AkashicEnd) depend on.
- Advance **slowly, measured, reversible** — each step gated on a real speedup, 0 warnings, green tests.
- Rust earns its place only where managed .NET is actually the bottleneck; IO-bound and
  orchestration code stays in .NET.

## Where Rust helps (and where it doesn't)
| Module | Workload | Rust value | Note |
|---|---|---|---|
| `PixelArtRenderer` (PNG → ▀ half-block + ANSI truecolor) | image decode + per-pixel→cell mapping + string building; CPU + alloc heavy | **High** | best first candidate (Rust `image` crate, tight loops, no GC) |
| `AnimationEngine.DrawAt(x,y,text,color)` | per-frame terminal draw | **Low** (revised) | it is the single **Console-IO** primitive (cursor move + `Console.Write`), not CPU-bound — Rust can't speed up terminal IO; the CPU cost is upstream in `PixelArtRenderer` |
| `ConsoleEngine.Terminal` (ConPTY/POSIX PTY) | native interop, byte pumping | **Low now** | just optimized + stabilized in .NET (O1/E2, 0.7.2); a Rust `portable-pty` crate could unify platforms later, but ROI is low today — **defer** |
| Scenes, Locale, Config, Persistence, World | JSON IO, orchestration, pure data | **None** | keep in .NET; Rust would add FFI cost for no gain |

## Architecture options
1. **Hybrid FFI (recommended, progressive):** rewrite one perf-critical module as a Rust
   `cdylib`, consumed via P/Invoke behind the *same* C# interface (DIP — callers don't change).
   Ship the native lib per-RID inside the NuGet package (`runtimes/<rid>/native/`). Keeps the .NET
   product intact, is feature-flaggable, and is reversible per module.
2. **Pure rewrite to Rust:** entire engine as Rust crates. Massive effort; breaks the existing
   .NET/NuGet consumers; only justified if the whole product pivots away from .NET. **Not now.**

→ **Recommendation: Hybrid FFI**, one module at a time, measured.

## Progressive levels (each is a gate)
- **L0 — Baseline (the only immediate step):** add BenchmarkDotNet benches for `PixelArtRenderer`
  (and the `DrawAt` hot path) on representative inputs; record ms/op, alloc/op, throughput. This is
  the bar everything else is measured against. *No Rust yet.*
- **L1 — Rust `PixelArtRenderer` PoC:** implement the PNG→cells conversion as a Rust `cdylib`,
  expose a narrow C ABI, call it via P/Invoke behind the existing C# API, feature-flagged.
  **Gate:** keep it only if it clears a pre-agreed threshold (suggest **≥2× faster or ≥50% less
  alloc**) — otherwise the FFI/build complexity isn't worth it and we revert.
- **L2 — Extend to the next CPU-bound candidate** only if L1 validates the pattern (e.g. PNG decode
  or sprite-build paths). *Note: `DrawAt` is Console-IO, not a Rust candidate — dropped from scope.*
- **L3 — Consider a Rust PTY crate** for `ConsoleEngine.Terminal` *only* if cross-platform
  maintenance cost rises; today the .NET backend is fast and stable, so this stays deferred.

Each level: re-measured vs L0, 0 warnings, all tests green, committed separately with the deltas
(same evidence-first method used for the Terminal O1/E2 work in `docs/perf-audit/terminal/`).

## Cross-cutting concerns
- **Build/CI:** add a `cargo` build that cross-compiles per RID (`win-x64`, `linux-x64`,
  `osx-arm64`, `linux-arm64` for RPi) and packs the natives into the NuGet `runtimes/` layout; the
  CI matrix already covers the three OSes.
- **Marshalling:** pass `ReadOnlySpan<byte>`/pointers, batch at the FFI boundary, avoid per-call
  copies (the Terminal backend already uses `fixed`/pointer interop — reuse that style).
- **Safety/AOT:** Rust gives memory safety on the native side; keep the C ABI tiny and `unsafe`
  surface auditable; verify NativeAOT compatibility if the consumer needs it.

## On "bun" (clarification)
`bun` is a **JavaScript/TypeScript runtime** (originally Zig, now being rewritten in Rust as of
2026). It has **no role in the engine's .NET or Rust core**. It would only matter if a future
*JS/TS* surface is added (a web playground, a docs site, or a web/Tauri editor). That is out of
scope for this roadmap and, if pursued, belongs in its own tooling track — not the engine.

## Recommendation summary
Do **L0 (measure) first**. Pursue Rust via **Hybrid FFI starting at `PixelArtRenderer`**, gated on
a real ≥2× / ≥50%-alloc win. **Do not** rewrite the just-stabilized Terminal backend or the
IO/logic modules. Pure-Rust rewrite is not justified while the .NET/NuGet line is the product.
No rewrite code until L0 numbers exist and the L1 gate threshold is agreed.
