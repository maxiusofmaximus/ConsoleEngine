# Rust roadmap — L2b: per-RID native packaging (wired + validated, not published)

> Ships the native `ce_render` accelerator inside the NuGet package per RID so consumers get the
> ~5× faster / ~0-alloc path automatically (managed fallback covers anything else). Validated by a
> CI dry-run — **no version bump, no publish** (that stays an explicit step).

## What changed
- **`ci.yml`**: `cargo build --release` runs before the .NET build on every matrix OS, so the
  native-gated `PixelArtRendererNativeTests` now run (not skip) on **ubuntu / macos / windows** —
  cross-platform byte-identity is covered in CI.
- **`nuget-publish.yml`**: a `build-natives` matrix builds the lib on each platform
  (win-x64 → windows-latest, linux-x64 → ubuntu-latest, osx-arm64 → macos-latest,
  linux-arm64 → **ubuntu-24.04-arm**) and uploads artifacts; a `pack` job stages them into
  `artifacts/native/<rid>/` and packs `runtimes/<rid>/native/<lib>`, then verifies the package
  carries them. Push is gated to **real `v*` tag pushes only**; `workflow_dispatch` is always a
  dry-run.
- **`ConsoleEngine.Rendering.csproj`**: dev-copy generalized to `.dll`/`.so`/`.dylib`; per-RID
  `Pack` items under `runtimes/<rid>/native/` (Condition=Exists → skipped when artifacts absent, so
  local/CI builds and the managed fallback are unaffected).

## Validation evidence (dry-run, 2026-06-02)
- CI run: native built on all 3 OSes; `ToAnsiBytes_NativePath_IsByteIdentical` **passed** on
  ubuntu (and macos/windows) — native == managed cross-platform. 270 tests green, 0 warnings.
- `nuget-publish.yml` dry-run (`workflow_dispatch dry_run=true`): all 4 native legs **success**
  (incl. linux-arm64 on the ARM runner). The packed `ConsoleEngine.Rendering.nupkg` contains:
  ```
  runtimes/win-x64/native/ce_render.dll        (98 KB)
  runtimes/linux-x64/native/libce_render.so    (341 KB)
  runtimes/linux-arm64/native/libce_render.so  (355 KB)
  runtimes/osx-arm64/native/libce_render.dylib (349 KB)
  ```
  No push occurred (dry-run).

## How consumers get the fast path
A consumer referencing `ConsoleEngine.Rendering` on a supported RID gets the matching native copied
to its output by .NET's `runtimes/` resolution; `NativeRender` loads it and
`PixelArtRenderer.ToAnsiBytes`/`WriteAnsi` use the fast path. On any other RID (or a trimmed/odd
deployment) the managed fallback keeps the API correct.

## Next (separate, explicit)
- **Release**: bump version + push a `v*` tag → the same workflow publishes the package with the
  natives. Consider whether to also unlist/supersede prior versions.
- Optional extra RIDs (`osx-x64`, `win-arm64`); a local-feed consume smoke before release.
