# ![ConsoleEngine](../Icon.png) ConsoleEngine

> A standalone .NET framework for building terminal / CLI games with pixel art,
> localisation, scene management, animation, and player-editable content.

---

## 1. Vision

ConsoleEngine is the engine layer extracted from AkashicEnd and published as an
independent, reusable framework. Any developer can reference it to build a
terminal game without reinventing rendering, localisation, config, persistence,
or scene flow.

AkashicEnd is the first — and currently the only — game built on ConsoleEngine.

**Design principles**

- Game content is data, not code. Scenes, dialogue, loot, and behaviour live in
  JSON or Markdown files that non-programmers can edit.
- The engine knows nothing about any specific game. All game logic lives in the
  game project.
- The same config, locale, and save files are consumed by both the terminal
  runtime and any future Unity / Godot front-end.
- The editor (ConsoleEngine.Editor) is a first-class citizen of the framework,
  not an afterthought.

---

## 2. Repository Structure (target)

```
ConsoleEngine/
  src/
    ConsoleEngine.Core/          ← models, interfaces, contracts
    ConsoleEngine.Rendering/     ← PixelArtRenderer, CharacterArt, ConsoleArt
    ConsoleEngine.Locale/        ← CL, CK (base keys), loaders, ILocalisationService
    ConsoleEngine.Scenes/        ← ScenePlayer, DialoguePlayer, TransitionEngine
    ConsoleEngine.Animation/     ← AnimationTimeline, VfxEngine, keyframe system
    ConsoleEngine.World/         ← WorldMovement, exploration framework
    ConsoleEngine.Config/        ← GameConfig, GameSettingsCatalog, settings commands
    ConsoleEngine.Persistence/   ← SaveRepository, GameConfigRepository
    ConsoleEngine.Launcher/      ← base OpenLauncher entry-point, CLI arg parsing
  ConsoleEngine.Editor/          ← Avalonia editor (separate project)
  ConsoleEngine.sln

AkashicEnd/
  src/
    AkashicEnd.Content/          ← scenes, locale files, sprites, loot tables
    AkashicEnd.Mechanics/        ← combat, squad, injuries, economy
    AkashicEnd.Models/           ← Unit, GameSession, etc.
    AkashicEnd.Launcher/         ← game entry point, wires ConsoleEngine + content
  AkashicEnd.sln
```

---

## 3. Engine Modules

### 3.1 ConsoleEngine.Core

Shared contracts with no external dependencies.

- `ILocalisationService` — get/set language, resolve keys with format args
- `IScenePlayer` — play a named scene by ID
- `IAnimationEngine` — execute a keyframe sequence
- `IWorldMap` — location graph and travel rules
- `ISaveRepository<T>` — generic save slot management
- `IGameConfigRepository` — load/save shared config
- `EngineContext` — root DI container passed to all subsystems

### 3.2 ConsoleEngine.Rendering

Everything that draws to the terminal.

- `PixelArtRenderer` — PNG → ANSI half-block (`▀`) with truecolor
- `ConsoleArt` — status headers, menus, prompts, error/success formatting
- `CharacterArt` — base ASCII art arrays; game projects extend with their own
- `BackgroundRenderer` — full-width ANSI background layer system
- `LayerCompositor` — composites background / midground / foreground layers
- `TerminalCapabilities` — detects truecolor support, window size, font

### 3.3 ConsoleEngine.Locale

- `CL` (ConsoleLocale) — static façade, `CL.Get(key)`, `CL.Get(key, args)`
- `CK` (ConsoleLocalisationKeys) — base key constants; games extend with their own
- `MarkdownLocalisationLoader` — parses `en.md`, `es.md`, etc.
- `InMemoryLocalisationService` — runtime locale table
- Supported languages: `en`, `ja`, `zh-Hans`, `es`, `pt`, `de`, `ru`, `it`, `fr`, `ca`, `ko`
- Hot-reload: call `CL.Initialize(root, lang)` at any time

### 3.4 ConsoleEngine.Scenes

- `SceneDefinition` — data model: title, art, lines, color, timing
- `DialogueDefinition` — speaker pairs, line list, character art references
- `ScenePlayer` — renders `SceneDefinition` with bottom-anchored layout
- `DialoguePlayer` — renders `DialogueDefinition` with two-character layout
- `TransitionEngine` — fade-in, fade-out, wipe, darkness-close
- `SceneSequencer` — plays an ordered list of scenes, handles branching

### 3.5 ConsoleEngine.Animation

- `AnimationTimeline` — ordered list of keyframes with timing in ms
- `Keyframe` — position, sprite, color, visibility, easing
- `VfxEngine` — ASCII particles, screen shake, flash effects
- `AttackAnimationBase` — reusable attack sequence template
- `AnimationPlayer` — executes a timeline against the current terminal state

### 3.6 ConsoleEngine.World

- `LocationDefinition` — ID, title, description, travel links, resources
- `WorldMap` — location graph loaded from `locations/*.md`
- `WorldMovement` — side-scrolling exploration renderer with HUD
- `ExplorationContext` — current location, visited flags, visible enemies

### 3.7 ConsoleEngine.Config

- `GameConfig` — language, volumes, display mode, aspect ratio, resolution
- `GameSettingsCatalog` — normalise + validate all setting values
- `GameSettingsCommands` — `set language en`, `set master 80`, etc.
- Shared between terminal runtime and Unity front-end via JSON

### 3.8 ConsoleEngine.Persistence

- `SaveRepository<T>` — slot-based JSON save files
- `GameConfigRepository` — read/write `Config/config.json`
- `MarkdownContentRepository` — load location, dialogue, loot Markdown files

### 3.9 ConsoleEngine.Launcher

- `OpenLauncher` — base CLI entry point: new / continue / options / exit
- `OpenGameArguments` — parse CLI args (`--ui`, `--scene`, `--lang`, etc.)
- `RunOptionsLoop` — shared options shell with language shorthand support
- `IntroSceneBase` — skeleton intro: Seoul → death → forest → name prompt

---

## 4. Extension Points for Game Projects

Games extend the engine without modifying it:

| Extension Point | How |
|---|---|
| Localisation keys | Subclass or extend `CK` with game-specific key constants |
| Character art | Add `string[]` arrays; register with `CharacterArt` registry |
| Location content | Drop `*.md` files in `GameData/locations/` |
| Locale strings | Add rows to `GameData/locale/en.md` (and other langs) |
| Save model | Pass any `T` to `SaveRepository<T>` |
| Scene sequences | Register named sequences in `SceneSequencer` |
| Node graph | Implement `IAnimationNode` for custom node types |

---

## 5. Editor Integration

`ConsoleEngine.Editor` (Avalonia) depends only on `ConsoleEngine.Core` interfaces.
It never references AkashicEnd. It can open any project that follows the engine's
data conventions.

Communication contract:
- Editor reads/writes `GameData/**/*.md` and `GameData/**/*.json`
- Editor writes `EDITOR_STATE.json` (current open scene, errors, asset list)
- Game reads the same data files at runtime — no intermediate compilation step
- Hot-reload: game watches for file changes and reloads without restart

---

## 6. NuGet / Distribution

Target: publish `ConsoleEngine.*` packages to NuGet or a private feed.

```xml
<!-- AkashicEnd.Launcher.csproj -->
<PackageReference Include="ConsoleEngine.Core"       Version="1.0.*" />
<PackageReference Include="ConsoleEngine.Rendering"  Version="1.0.*" />
<PackageReference Include="ConsoleEngine.Locale"     Version="1.0.*" />
<PackageReference Include="ConsoleEngine.Scenes"     Version="1.0.*" />
<PackageReference Include="ConsoleEngine.Animation"  Version="1.0.*" />
<PackageReference Include="ConsoleEngine.World"      Version="1.0.*" />
<PackageReference Include="ConsoleEngine.Config"     Version="1.0.*" />
<PackageReference Include="ConsoleEngine.Persistence" Version="1.0.*" />
<PackageReference Include="ConsoleEngine.Launcher"   Version="1.0.*" />
```

Until the API is stable, use direct project references or a local NuGet feed.

---

## 7. Actual Module State (v1.0.0 — 2026-05-28)

The repository structure differs slightly from the target in §2 because some
planned modules have not been created yet. Here is what actually exists:

| Module | Status | Notes |
|---|---|---|
| `ConsoleEngine.Core` | ✅ Shipped | `ILocalizationService`, `EngineVersion`, interfaces |
| `ConsoleEngine.Rendering` | ✅ Shipped | `PixelArtRenderer` (PNG→▀), `AnimationEngine` |
| `ConsoleEngine.Locale` | ✅ Shipped | `CL`, `CK`, `MarkdownLocalizationLoader`, `InMemoryLocalizationService` |
| `ConsoleEngine.Scenes` | ✅ Shipped | `SceneDefinition`, `SceneLoader`, `ScenePlayer`, `DialoguePlayer`, `TransitionEngine` |
| `ConsoleEngine.World` | ✅ Shipped | `WorldState`, `WorldMap`, `ExplorationPlayer`, `IExplorationAction` |
| `ConsoleEngine.Config` | ✅ Shipped | `GameConfig`, `GameSettingsCatalog`, `GameSettingsCommands`, `ResolutionPreset` |
| `ConsoleEngine.Persistence` | ✅ Shipped | `SaveRepository<T>`, `GameConfigRepository` |
| `ConsoleEngine.SceneRunner` | ✅ Shipped | Standalone exe; takes `.scene.json`, calls `ScenePlayer.Play()` |
| `ConsoleEngine.Editor` | 🔨 Phase A | Avalonia 12 editor — scene mgmt, text editor, play/stop/reload done |
| `ConsoleEngine.Animation` | ❌ Not created | Planned: `AnimationTimeline`, `VfxEngine`, keyframe system |
| `ConsoleEngine.Launcher` | ❌ Not created | Planned: `OpenLauncher`, CLI arg parsing |

---

## 8. Roadmap

### Phase 0 — Plan
- [x] Define engine vs game boundaries
- [x] Document architecture
- [x] Create `ENGINE_PLAN.md`, `EDITOR_PLAN.md`, `MIGRATION_PLAN.md`

### Phase 1 — Extract
- [x] Create `ConsoleEngine.sln`
- [x] Move each subsystem to its `ConsoleEngine.*` project
- [x] AkashicEnd verified to build and run after extraction

### Phase 2 — Stabilise API
- [x] Public interfaces defined for all shipped modules
- [x] XML doc comments on all public API surface
- [x] Semantic versioning: `EngineVersion` constant, NuGet tags `v*`
- [ ] Engine-level unit tests (independent of AkashicEnd content)

### Phase 3 — Editor
- [x] `ConsoleEngine.Editor` Avalonia app scaffolded
- [x] Scene management (create, list, duplicate, delete, save, reload)
- [x] Text editor with live terminal preview
- [x] Play / Stop modes via `ConsoleEngine.SceneRunner.exe`
- [ ] Sprite import and ANSI preview (Phase A remaining)
- [ ] Embedded AI terminal panel (Phase A remaining)
- [ ] Full Phase B–D features — see `EDITOR_PLAN.md`

### Phase 4 — Publish
- [x] NuGet workflow (`nuget-publish.yml`) — triggers on `v*` tags
- [x] README and getting-started guide
- [ ] AkashicEnd updated to consume published packages (pending API freeze)
- [ ] Open-source decision pending

---

## 9. Context Files for AI Assistance

Two files are maintained by the engine project and consumed by any AI assistant:

| File | Updated by | Contains |
|---|---|---|
| `GAME_CONTEXT.md` | Developer (manually) | GDD summary, conventions, data formats, engine API |
| `EDITOR_STATE.json` | Editor (automatically) | Current open scene, assets in use, active errors |

The `EDITOR_STATE.json` writer is a **planned feature** of the editor (Phase A / Module 9).
Until it is implemented, load `GAME_CONTEXT.md` manually:
```
claude --context GAME_CONTEXT.md
```

---

**Created**: 2026-05-26
**Author**: AkashicEnd Development Team
**Status**: v1.0.0 shipped — Editor Phase A in progress
