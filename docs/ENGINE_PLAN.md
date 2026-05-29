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
<PackageReference Include="ConsoleEngine.Core"       Version="0.5.*" />
<PackageReference Include="ConsoleEngine.Rendering"  Version="0.5.*" />
<PackageReference Include="ConsoleEngine.Locale"     Version="0.5.*" />
<PackageReference Include="ConsoleEngine.Scenes"     Version="0.5.*" />
<PackageReference Include="ConsoleEngine.Animation"  Version="0.5.*" />
<PackageReference Include="ConsoleEngine.World"      Version="0.5.*" />
<PackageReference Include="ConsoleEngine.Config"     Version="0.5.*" />
<PackageReference Include="ConsoleEngine.Persistence" Version="0.5.*" />
<PackageReference Include="ConsoleEngine.Launcher"   Version="0.5.*" />
```

Until the API is stable, use direct project references or a local NuGet feed.

---

## 7. Actual Module State (v0.6.0 — 2026-05-29)

The repository structure differs slightly from the target in §2 because some
planned modules have not been created yet. Here is what actually exists:

| Module | Status | Notes |
|---|---|---|
| `ConsoleEngine.Core` | ✅ Shipped | `ILocalizationService`, `IInputProvider`, `IScenePlayer`, `IAudioPlayer`, `FlagStore`, `EngineVersion` |
| `ConsoleEngine.Rendering` | ✅ Shipped | `PixelArtRenderer` (PNG→▀), `AnimationEngine` |
| `ConsoleEngine.Locale` | ✅ Shipped | `CL`, `CK`, `MarkdownLocalizationLoader`, `InMemoryLocalizationService` |
| `ConsoleEngine.Scenes` | ✅ Shipped | `SceneDefinition`, `SceneLoader`, `ScenePlayer`, `DialogueLoader`, `DialoguePlayer`, `SceneSequencer`, `TransitionEngine` |
| `ConsoleEngine.World` | ✅ Shipped | `WorldState`, `WorldMap`, `ExplorationPlayer`, `IExplorationAction` |
| `ConsoleEngine.Config` | ✅ Shipped | `GameConfig`, `GameSettingsCatalog`, `GameSettingsCommands`, `ResolutionPreset` |
| `ConsoleEngine.Persistence` | ✅ Shipped | `SaveRepository<T>`, `GameConfigRepository` |
| `ConsoleEngine.Input` | ✅ Shipped | `ConsoleInputProvider`, `MockInputProvider` (early — planned for v0.9.0) |
| `ConsoleEngine.SceneRunner` | ✅ Shipped | Standalone exe; takes `.scene.json`, calls `ScenePlayer.Play()` |
| `ConsoleEngine.Tests` | ✅ Shipped | 139 xUnit tests — all modules covered (early — planned for v0.8.0) |
| `ConsoleEngine.Editor` | 🔨 Phase A | Avalonia 12 editor — scene mgmt, text editor, play/stop/reload/duplicate/delete done |
| `ConsoleEngine.Animation` | ❌ Not created | Planned v0.7.0: `AnimationTimeline`, `VfxEngine`, keyframe system |
| `ConsoleEngine.Launcher` | ❌ Not created | Planned v0.8.0: `OpenLauncher`, CLI arg parsing |
| `ConsoleEngine.Audio` | ❌ Not created | Planned v0.8.0: `IAudioPlayer`, `NullAudioPlayer`, `NAudioPlayer` (interfaces exist in Core) |

---

## 8. Roadmap

### v0.5.0 ✅ — NuGet + loaders (shipped)
- [x] NuGet packaging centralizado (`Directory.Build.props`)
- [x] `SceneLoader`, `WorldLoader` — carga desde disco con `TryLoad/Load`
- [x] GitHub Actions CI (build + pack dry-run) y publish pipeline (`v*` tags)

### v0.6.0 🔨 — Editor Phase A + fundamentos (en progreso)
- [x] `DialogueLoader` — patrón idéntico a `SceneLoader`
- [x] `SceneSequencer` — encadena escenas; implementa `IScenePlayer`
- [x] `FlagStore` — diccionario serializable `string → object`
- [x] `IInputProvider` / `ConsoleInputProvider` / `MockInputProvider`
- [x] `IAudioPlayer` / `NullAudioPlayer` (interfaces + stubs)
- [x] `ConsoleEngine.Tests` (139 tests xUnit — todos los módulos)
- [x] `CL.Get(key, fallback)` — overload para contextos sin locale inicializado
- [x] Editor: Play lanza WT desacoplado (fix crash al usar botón Play)
- [ ] `PixelArtRenderer.ToAnsiString()` — overload que devuelve string
- [ ] Sprite import y ANSI preview en el editor (Module 5)
- [ ] Drag & drop reorden de escenas (Module 1)
- [ ] Panel de terminal embebido con WebView2 + xterm.js (Module 9)
- [ ] `EDITOR_STATE.json` writer en `MainViewModel`
- [ ] Roslyn Analyzers activados en `Directory.Build.props`
- [ ] Validador de integridad en `MainViewModel.TrySave()`

### v0.7.0 📋 — ConsoleEngine.Animation
- [ ] Nuevo módulo `ConsoleEngine.Animation`: `AnimationTimeline`, `Keyframe`, `VfxEngine`, `AnimationPlayer`
- [ ] `IAnimationEngine` conectado con `AnimationPlayer` en Core
- [ ] `IExplorationAction.IsAvailable` extendido con `FlagStore`
- [ ] Sample: VFX de muerte en DinoGame

### v0.8.0 📋 — ConsoleEngine.Launcher + ConsoleEngine.Audio + CI
- [ ] Nuevo módulo `ConsoleEngine.Launcher`: `OpenLauncher`, `OpenGameArguments`, `RunOptionsLoop`, `IntroSceneBase`
- [ ] Nuevo módulo `ConsoleEngine.Audio`: implementación real `NAudioPlayer`
- [ ] CI: `dotnet test` + build de samples + verificación de EngineVersion vs Directory.Build.props

### v0.9.0 📋 — Editor Phase B
- [ ] Editor: timeline de animación, node graph, narrative flow graph, asset panel drag & drop
- [ ] Editor: editor de diálogos visual, visualizador de mapa de mundo, locale side-by-side
- [ ] `KeyBinding` — remapeo de teclas en runtime

### v0.10.0 📋 — Editor Phase C
- [ ] Health dashboard, asset dependency map, localisation workflow (auto-draft + status)
- [ ] Animation debugger (breakpoints + step-frame)
- [ ] Multilingual layout stress test, notas y comentarios por escena

### v1.0.0 📋 — API estable — NuGet Release
- [ ] API freeze: sin cambios breaking en ningún módulo público
- [ ] Todos los módulos publicados en NuGet.org
- [ ] `AkashicEnd` consume paquetes publicados (sin project references)
- [ ] `dotnet new consoleengine` — template instalable
- [ ] Decisión de open-source resuelta

---

## 10. Architectural Decision Log

Decisiones de arquitectura tomadas con justificación, basadas en investigación de engines modernos.  
Ver `docs/ARCHITECTURE_RESEARCH.md` para el análisis completo.

| ID | Decisión | Alternativa rechazada | Razón | Versión |
|---|---|---|---|---|
| ADR-001 | `sealed record` para `SceneDefinition`, `LocationDefinition`, `DialogueDefinition` | `sealed class` con constructor copia | `with` expressions para variantes; igualdad estructural; patrón ScriptableObject | 0.2.0 |
| ADR-002 | JSON + `"schemaVersion"` en todos los schemas de disco | Binary, YAML sin versión | Migrable, legible, versionable; lección de Unity binary saves | 0.6.0 |
| ADR-003 | `PixelArtRenderer.ToAnsiString()` + `ScenePlayer.RenderToString()` | Reimplementar layout en editor | Editor usa mismo código que runtime; lección de Godot (editor = engine) | 0.6.0 |
| ADR-004 | `ILocalizationService.LanguageChanged` event C# nativo | MediatR, custom EventBus | Single-threaded; `event Action` es suficiente; MediatR es overkill | 0.7.0 |
| ADR-005 | Node-tree para `SceneSequencer` (lista de `SceneNode` con condiciones) | ECS puro | Godot demuestra que node-tree es más apropiado para narrativa que ECS | 0.6.0 |
| ADR-006 | `IInputProvider` abstraction; `Console.ReadKey()` solo en implementación | Hardcode `Console.ReadKey()` | Testeable, rebindable, mockeable; lección de Unity TestRunner | 0.9.0 |
| ADR-007 | `ChunkedWorldMap` (lazy-loading por región) | Carga total en constructor | UE5 World Partition / Godot tile loading: streaming por chunks escala mejor | 0.9.0 |
| ADR-008 | `IGamePlugin` ≠ `IEditorPlugin` (separación runtime/editor plugins) | Un solo tipo de plugin | Flax Engine: game plugins van con runtime; editor plugins son herramientas | 0.10.0+ |
| ADR-009 | Sin ECS como modelo principal | Arch-ECS global | ECS óptimo para miles de NPCs homogéneos, no para narrativa compleja | — |
| ADR-010 | Sin `Activator.CreateInstance` en game loop | Reflection para dispatch | UE5 Blueprint VM overhead: ~0.2μs/actor/frame; dispatch estático = cero overhead | — |

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
**Status**: v0.5.0 shipped — Editor Phase A in progress; roadmap continues to v1.0.0
