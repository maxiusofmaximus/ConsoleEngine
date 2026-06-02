# ConsoleEngine — Roadmap / Next Steps

Forward-looking backlog (what's left), prioritized. Engine-only; the homelab/CachyOS track lives
separately in `C:\Users\maxli\migracion-cachyOS\`.

## Recently shipped
- **Terminal PTY backend** optimized + POSIX bugs fixed (0.7.2): read-path alloc −99% / concurrency
  +38% (O1), zero-copy POSIX write (E2); fixed Linux EINTR deadlock, macOS ARM64 variadic ioctl,
  and the lost-`Exited` race. Evidence in `docs/perf-audit/terminal/`.
- **Rust native render acceleration** L0→L2b, released in **0.7.3**: `PixelArtRenderer.ToAnsiBytes` /
  `WriteAnsi` (~5× faster, ~0 alloc), per-RID natives shipped under `runtimes/<rid>/native/`
  (win-x64, linux-x64, linux-arm64, osx-arm64) with a managed fallback. Evidence:
  `docs/perf-audit/rust-{baseline-L0,L1-result,L2-result,L2b-packaging,roadmap}.md`.
- CI on Node 24, green cross-platform (native path now tested on ubuntu/macos/windows).
- Local-AI offload skill (`~/.claude/skills/qwen-offload`, CUDA) for token-cheap bulk analysis.

## Backlog (prioritized)

### P1 — Rust L3: native PNG decode (cross-platform **and** fast)
`RenderPng` / `ToAnsiString(string path)` use `System.Drawing.Bitmap.GetPixel` — slow (per-pixel)
**and Windows-only** (`System.Drawing.Common` 8 throws on Linux/macOS; CA1416 is suppressed).
Add a PNG decoder to the `ce_render` crate (the `image` crate): PNG bytes → `Rgb` buffer → feed the
existing `ToAnsiBytes`. Same managed API + fallback (System.Drawing on Windows). **Dual win:** makes
PNG loading cross-platform *and* removes the GetPixel bottleneck. Gate + benchmark like L1.

### P1 — Managed quick win: `LockBits` instead of `GetPixel`
Independent of Rust: the managed PNG path can use `Bitmap.LockBits` + a raw byte buffer (10–100×
faster pixel access on Windows). Cheap; do it unless the Rust decoder (above) supersedes it.

### P2 — Rust drop-in `ToAnsiString` + `RenderCore` streaming
- Route the string-returning `ToAnsiString` through native when present (deferred from L2a): ~22%
  less alloc / faster, capped by the final-string allocation.
- `RenderCore` (absolute-positioned sprite draw): stream bytes with embedded cursor-move escapes via
  native, avoiding the per-row `Console.SetCursorPosition` + string building.

### P2 — Packaging / release hygiene
- Pin the Rust toolchain version in CI (determinism) instead of the runner's preinstalled stable.
- Pre-release **consume smoke**: restore the freshly-packed package from a local feed and assert
  `PixelArtRenderer.IsNativeAccelerationAvailable == true` before pushing a release tag.
- Optional extra RIDs: `osx-x64`, `win-arm64`.

### P2 — Tests
- Property-based encoder equivalence: random grids → native output == managed output (fuzz).
- Terminal: a stress test for the `Exited`-replay race (many fast-exit children under parallel load).
- Perf-regression guard: a lightweight BenchmarkDotNet smoke (scheduled or on-demand) to catch
  renderer regressions vs the L0/L2 baselines.

### P3 — `DrawAt` batching
`AnimationEngine.DrawAt` does a `Console.SetCursorPosition` + `Console.Write` per call; batching a
frame's draws into one buffered write cuts syscalls. Managed optimization (DrawAt is IO, not a Rust
candidate).

## Other tracks (separate, not this backlog)
- **Editor Phase A** remaining (embedded terminal panel, sprite import/ANSI preview, drag-&-drop
  reorder) — see `docs/EDITOR_PLAN.md`.
- **Homelab / CachyOS migration** (two-machine client/server: AI server, storage tiering, game
  streaming, save backups) — `C:\Users\maxli\migracion-cachyOS\` (ADR 0002: both machines →
  CachyOS; execution pending — tomorrow).
- **Local-AI offload** usage/benchmarks — skill `qwen-offload`; results in
  `migracion-cachyOS/benchmarks/`.

## Release process (reference)
Bump `src/Directory.Build.props` `<Version>`, commit, push a `v*` tag → `nuget-publish.yml` builds
the per-RID natives and publishes. `workflow_dispatch` on that workflow is a dry-run (never pushes).
The `NUGET_API_KEY` secret has **push but not unlist** permission — unlist via the nuget.org web UI.
