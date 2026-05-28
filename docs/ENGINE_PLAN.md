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

## 7. Roadmap

### Phase 0 — Plan (current)
- [x] Define engine vs game boundaries
- [x] Document architecture
- [ ] Create `ENGINE_PLAN.md`, `EDITOR_PLAN.md`, `MIGRATION_PLAN.md`

### Phase 1 — Extract
- [ ] Create `ConsoleEngine.sln` alongside `AkashicEnd.sln`
- [ ] Move each subsystem to its `ConsoleEngine.*` project
- [ ] Replace direct class references in AkashicEnd with interface references
- [ ] Verify AkashicEnd builds and runs identically after extraction
- [ ] All existing tests still pass

### Phase 2 — Stabilise API
- [ ] Define and freeze public interfaces for each module
- [ ] Add XML doc comments to all public API surface
- [ ] Write engine-level unit tests (independent of AkashicEnd content)
- [ ] Semantic versioning policy documented

### Phase 3 — Editor
- [ ] Build `ConsoleEngine.Editor` (Avalonia) against engine interfaces
- [ ] Editor opens AkashicEnd as its first target project
- [ ] Hot-reload pipeline working end-to-end

### Phase 4 — Publish
- [ ] Pack as NuGet packages
- [ ] README and getting-started guide
- [ ] AkashicEnd updated to consume published packages
- [ ] Optional: open-source the engine, keep AkashicEnd private

---

## 8. Context Files for AI Assistance

Two files are maintained by the engine project and consumed by any AI assistant:

| File | Updated by | Contains |
|---|---|---|
| `GAME_CONTEXT.md` | Developer (manually) | GDD summary, conventions, data formats, engine API |
| `EDITOR_STATE.json` | Editor (automatically) | Current open scene, assets in use, active errors |

When launching an AI CLI from the embedded terminal:
```
claude --context GAME_CONTEXT.md --context EDITOR_STATE.json
```

---

**Created**: 2026-05-26
**Author**: AkashicEnd Development Team
**Status**: Planning — Phase 0
