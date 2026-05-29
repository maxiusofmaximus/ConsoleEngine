# Changelog

All notable changes to ConsoleEngine are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [1.0.0] — 2026-05-28

### Added

- **`schemaVersion` field** in all JSON schemas (`.scene.json`, `.world.json`, `.dialogue.json`, `.sequence.json`)
  - `SceneLoader`, `WorldLoader`, `DialogueLoader`, `SceneSequencer.Load()` reject future unknown versions with `InvalidDataException`
  - Legacy files without the field are treated as version 0 (no-op migration)
- **`ScenePlayer.RenderToString()`** — dry-run mode that returns a string instead of writing to the terminal; editor preview and tests now call the same rendering code as the runtime
- **`DialogueLoader`** (`ConsoleEngine.Scenes`) — loads `.dialogue.json` files from disk; same `Load` / `TryLoad` pattern as `SceneLoader`
- **`SceneSequencer`** (`ConsoleEngine.Scenes`) — plays an ordered list of `SceneNode` records; each node has an optional `Condition` and `SceneOverrides`; also loads from `.sequence.json`
- **`IScenePlayer`** (`ConsoleEngine.Core`) — narrow interface (`Play()`) implemented by both `ScenePlayer` and `SceneSequencer`
- **`ILocalizationService.LanguageChanged`** event — fires when `SetLanguage()` changes the active language; lets live UIs re-render without restarting
- **`FlagStore`** (`ConsoleEngine.Core`) — typed, JSON-serialisable key-value store (`Set<T>` / `Get<T>` / `TryGet<T>`) for game progress and flags
- **`IAudioPlayer`** + **`NullAudioPlayer`** (`ConsoleEngine.Core`) — audio abstraction; `NullAudioPlayer.Instance` is the silent default
- **`IInputProvider`** (`ConsoleEngine.Core`) — keyboard/line input abstraction (`ReadKey`, `ReadLine`, `KeyAvailable`)
- **`ConsoleEngine.Input`** — new module containing `ConsoleInputProvider` (wraps `Console`) and `MockInputProvider` (queued keys for tests)
- **`ConsoleEngine.Tests`** — new xUnit test project (56 tests); covers `SceneLoader`, `WorldLoader`, `InMemoryLocalizationService`, `FlagStore`, `ScenePlayer.RenderToString`, `SceneSequencer`, `DialogueLoader`, `SaveRepository`
- **Roslyn analyzers** — `AnalysisLevel=latest-recommended` in `Directory.Build.props`; 0 warnings enforced

### Changed

- **`ScenePlayer.Play()`** and **`DialoguePlayer.Play()`** accept an optional `IInputProvider?` parameter (default = `Console.ReadLine`)
- **`ExplorationPlayer.Run()`** accepts an optional `IInputProvider?` parameter (`inputProvider`)
- **`MainViewModel`** (editor) uses `DispatcherTimer` to throttle preview rebuilds to 100 ms — prevents full rebuild on every keystroke
- **`MainViewModel.RebuildPreview()`** calls `ScenePlayer.RenderToString()` — editor preview now uses the same layout code as the runtime
- **`GameConfig`** public instance fields converted to properties (CA1051)
- **`ILocalizationService.Get()`** renamed to `Resolve()` (CA1716 — avoids keyword conflict in VB.NET); `InMemoryLocalizationService` keeps `Get()` as a convenience alias
- **`CK.Error`** renamed to `CK.Errors` (CA1716)
- **`IAudioPlayer.Stop()`** renamed to `StopPlayback()` (CA1716)
- **`SceneDocument`** gains `SchemaVersion = 1` field and `ToDefinition()` conversion method
- `ConsoleEngine.Input` and `ConsoleEngine.Tests` added to `ConsoleEngine.sln`

### Fixed

- Various Roslyn analyzer warnings (CA1051, CA1305, CA1716, CA1859, CA1865, CA1869, CS8600)

---

## [0.5.0] — 2026-05-27

### Added

- **NuGet packaging** — all 7 library modules are now NuGet-ready
  - `src/Directory.Build.props` centralises `Version`, `Authors`, `PackageLicenseExpression`, `PackageTags`, `PackageReadmeFile`, and `PackageIcon` for every project
  - Individual `.csproj` files reduced to only their unique properties
- **`SceneLoader`** (`ConsoleEngine.Scenes`) — loads `.scene.json` files from disk; `Load()` and `TryLoad()` variants
- **`WorldLoader`** (`ConsoleEngine.World`) — loads `.world.json` files from disk; `Load()` and `TryLoad()` variants
- **`ExplorationOptions.HudHint`** — configurable HUD hint string (default: `[look] [exits] [wait] [menu] [quit]`)
- **GitHub Actions CI** (`.github/workflows/ci.yml`) — build + dry-run pack on every push and pull request to `main`
- **GitHub Actions NuGet publish** (`.github/workflows/nuget-publish.yml`) — auto-publishes all 7 packages to NuGet.org on `v*` tag push
- **`assets/icon.png`** — 128×128 package icon

### Changed

- **`SceneDefinition`** changed from `sealed class` to `sealed record` — enables `with` expressions for partial overrides (e.g. injecting dynamic lines into a JSON-loaded template)
- **`LocationDefinition`** changed from `sealed class` to `sealed record`
- **`DialogueDefinition`** changed from `sealed class` to `sealed record`
- **`SaveRepository<T>`** migrated from `Newtonsoft.Json` to `System.Text.Json` (built into .NET 8; zero extra dependency)
- **`GameConfigRepository`** migrated from `Newtonsoft.Json` to `System.Text.Json`
- **`samples/DinoGame`** — intro scene extracted from compiled code to `GameData/scenes/intro.scene.json`
- **`samples/WorldDemo`** — intro and outro scenes extracted to JSON; world definition extracted to `GameData/world/world.world.json`; `TheWorld.Build()` reduced from 155 lines to a single `WorldLoader.Load()` call
- **`EngineVersion`** updated to `0.5.0`

### Fixed

- `CS0419` ambiguous cref warnings in `ILocalizationService` and `CL` XML docs
- `CS1734` invalid `<paramref>` in `SaveRepository<T>` class-level doc

---

## [Unreleased] — 0.6.0 · Editor Phase A complete + fundamentos pendientes

### Planned

**Editor — Phase A restante**
- **Sprite import y ANSI preview** (Module 5) — botón file-picker en el panel derecho; nuevo overload `PixelArtRenderer.ToAnsiString()` (devuelve string, no escribe en consola); center panel muestra el sprite como half-block text
- **Drag & drop reorden de escenas** (Module 1) — `DragDrop.DoDragDrop` / `DragDrop.Drop` en el `ListBox`; mueve items en `ObservableCollection<SceneFileEntry>`
- **Panel de terminal embebido** (Module 9) — WebView2 + xterm.js; interface `ITerminalPanel` (ISP); abre con botón en toolbar; lanza `claude --context GAME_CONTEXT.md --context EDITOR_STATE.json`
- **`EDITOR_STATE.json` writer** — `MainViewModel` escribe el JSON antes de lanzar el terminal; campos: `activeScene`, `projectPath`, `validationErrors`

**Engine — piezas del diseño aún sin código**
- **`DialogueLoader`** (`ConsoleEngine.Scenes`) — carga `.dialogue.json` desde disco; patrón idéntico a `SceneLoader` (`Load()` / `TryLoad()`)
- **`SceneSequencer`** (`ConsoleEngine.Scenes`) — encadena escenas en orden; implementa `IScenePlayer` (Core); permite branching básico (índice de escena siguiente)
- **`GAME_CONTEXT.md`** — archivo de contexto de proyecto generado manualmente; documenta convenciones, formatos de datos y API del engine para asistentes AI

**Calidad**
- **Roslyn Analyzers** — `<AnalysisLevel>latest-recommended</AnalysisLevel>` en `Directory.Build.props`; todos los warnings corregidos en origen (sin supresiones masivas)
- **Validador de integridad al guardar** — `MainViewModel.TrySave()` verifica antes de escribir: `spritePath` existente en disco, `artColor`/`textColor` valores válidos de `ConsoleColor`, líneas no vacías si `promptContinue: true`

---

## [Unreleased] — 0.7.0 · ConsoleEngine.Animation + FlagStore

### Planned

**ConsoleEngine.Animation** (nuevo módulo)
- `AnimationTimeline` — lista ordenada de keyframes con timing en ms
- `Keyframe` — posición, sprite, color, visibilidad, función de easing
- `VfxEngine` — partículas ASCII (sangre, chispas, explosiones de texto), screen shake, flash effects
- `AttackAnimationBase` — template reutilizable de secuencia de ataque
- `AnimationPlayer` — ejecuta una timeline contra el estado actual del terminal
- Conectar `AnimationPlayer` con `IAnimationEngine` en `ConsoleEngine.Core`
- Sample: extender `DinoGame` con VFX de muerte en game-over

**FlagStore** (nuevo mini-módulo en `ConsoleEngine.World`)
- `FlagStore` — diccionario `string → bool/int/string` serializable con `System.Text.Json`
- Integración con `IExplorationAction.IsAvailable(WorldState, FlagStore)` — amplía la firma existente
- Integración con `SaveRepository<T>` — flags se guardan en el slot de partida
- Locale key `CK.Flags.*` para mensajes relacionados con flags

---

## [Unreleased] — 0.8.0 · ConsoleEngine.Launcher + ConsoleEngine.Audio + Tests + CI

### Planned

**ConsoleEngine.Launcher** (nuevo módulo)
- `OpenLauncher` — entry point CLI base: new / continue / options / exit
- `OpenGameArguments` — parsea args CLI (`--ui`, `--scene`, `--lang`, etc.)
- `RunOptionsLoop` — shell de opciones compartido con soporte de shorthand de idioma
- `IntroSceneBase` — skeleton de intro: escenas locale-driven → entrega control al juego

**ConsoleEngine.Audio** (nuevo módulo)
- `IAudioPlayer` — interface en Core: `Play(clip)`, `Stop()`, `SetVolume(channel, 0-100)`
- `NullAudioPlayer` — implementación vacía (terminal puro no tiene audio nativo)
- `NAudioPlayer` — implementación real con NAudio (para juegos con ventana WinForms/WPF)
- Locale key `CK.Audio.*` para mensajes de error de audio
- Conecta con los 5 canales de volumen ya definidos en `GameConfig`

**ConsoleEngine.Tests** (xUnit — nuevo proyecto)
- `SceneLoader` — round-trip serialize/deserialize; `artColor` inválido → fallback `Gray`
- `WorldLoader` — location sin ID → `InvalidDataException`; exits vacíos → OK
- `InMemoryLocalizationService` — clave ausente → `[key]` literal; fallback a `en` cuando lang falta
- `GameSettingsCatalog` — volumen fuera de rango → normalización; alias `"en"` == `"English"`
- `SaveRepository<T>` — crear slot → cargar → borrar; `LoadMostRecent` con múltiples slots
- `MarkdownLocalizationLoader` — archivo vacío; clave duplicada; formato incorrecto

**CI — pasos faltantes**
- `dotnet test ConsoleEngine.Tests` antes del pack
- `dotnet build` de los 3 samples como smoke test
- Verificación de que `EngineVersion.Full` coincide con `<Version>` en `Directory.Build.props`
- (Opcional) Coverlet + informe de cobertura como artefacto de CI

---

## [Unreleased] — 0.9.0 · ConsoleEngine.Input + Editor Phase B

### Planned

**ConsoleEngine.Input** (nuevo módulo)
- `IInputProvider` — interface en Core: `ReadKey()`, `KeyAvailable`
- `ConsoleInputProvider` — implementación con `Console.ReadKey(intercept: true)`
- `KeyBinding` — mapeo nombre-lógico → tecla física; cargable desde `config.json`
- Conecta con `GameSettingsCommands`: nuevo comando `bind jump Space`
- Permite mocking del input en tests unitarios

**Editor — Phase B**
- **Panel de assets drag & drop** (Module 3) — catálogo de sprites, backgrounds y VFX con thumbnails
- **Background editor** (Module 6) — editor de cuadrícula ASCII + importar PNG→ANSI mosaic
- **Timeline de animación por keyframe** (Module 7) — por escena; copy/paste de frames; loop preview
- **Inspector de propiedades no-code** (Module 3) — al seleccionar un elemento sus parámetros aparecen en sidebar
- **Node graph básico** (Module 4) — nodos `ShowSprite`, `ShowText`, `Delay`, `Transition` conectados con wires
- **Narrative flow graph** (Module 10) — canvas macro donde cada nodo es una escena; flechas = transiciones
- **Editor de diálogos visual** — árbol de conversación con nodos de elección; edita `.dialogue.json` visualmente
- **Visualizador de mapa de mundo** — grafo de `LocationDefinition` y sus exits; click en nodo abre la location
- **Editor de locale side-by-side** — `en.md` a la izquierda, idioma destino a la derecha; diff de claves faltantes resaltado en rojo

---

## [Unreleased] — 0.10.0 · Editor Phase C

### Planned

- **Health dashboard** (Module 14) — pantalla de inicio del editor; lista escenas con claves locale faltantes, sprites no encontrados, assets huérfanos
- **Asset dependency map** (Module 12) — grafo bidireccional asset → escenas; rename/delete con aviso de referencias
- **Localisation workflow** (Module 13) — auto-draft de traducciones; estado por clave: `draft / reviewed / approved`; exportar claves no traducidas como lista plana
- **Animation debugger** (Module 15) — breakpoints por frame; step-frame con `F10`; inspector de estado al pausar
- **Multilingual layout stress test** (Module 18) — muestra las 11 traducciones en miniatura; resalta en rojo strings que desbordan su área
- **Editor de notas y comentarios** (Module 19) — sticky notes por escena: `TODO`, `DECISION`, `QUESTION`, `AUDIO`; visibles en el health dashboard

---

## [Unreleased] — 1.0.0 · API Estable — NuGet Release

### Planned

- **API freeze** — sin cambios breaking después de este tag
- Todos los módulos publicados en NuGet.org via workflow `v*`
- `AkashicEnd` actualizado para consumir paquetes NuGet publicados (sin project references)
- Documentación XML completa en toda la superficie pública de la API
- CI verde en cada push a `main`; workflow de publish probado end-to-end
- **`dotnet new consoleengine`** — template instalable; genera `Program.cs`, `GameData/locale/en.md`, `.csproj` con referencias correctas
- Editor Phase A completo (todos los items restantes entregados en 0.6.0)
- Decisión de open-source resuelta

---

## [0.4.0] — 2026-05-27

### Added

- **ConsoleEngine.Editor** — Avalonia 12 desktop scene editor
  - `SceneDocument` — JSON-serialisable model mirroring `SceneDefinition`; fields: title, lines, asciiArt, artColor, textColor, spritePath, promptContinue
  - `SceneFileEntry` — immutable list-item model with `DisplayName` (prefixed `* ` when unsaved)
  - `MainViewModel` — full `INotifyPropertyChanged` view model with:
    - `LoadProject(folder)` — enumerates all `*.scene.json` files recursively, O(1) re-display
    - `LoadScene(entry)` — deserialises JSON and populates flat bindable properties
    - `CreateNewScene(folder)` — allocates a unique `sceneNNN.scene.json` filename and marks dirty
    - `TrySave(entry)` — serialises to indented JSON, clears dirty flag, refreshes list entry
    - `RebuildPreview()` — generates a 54-char-wide monospace terminal preview string on every edit
    - `SyncAndPreview()` — round-trips flat properties back to the document model before rebuild
  - `MainWindow.axaml` — three-panel layout (220 px file list | preview | 300 px properties)
    - Toolbar: Open Project / New Scene / Save with `IsEnabled` bindings
    - Left panel: `ListBox` with `x:DataType="SceneFileEntry"` compiled bindings
    - Centre panel: dark (`#0C0C0C`) monospace `TextBlock` preview, scrollable
    - Right panel: Title `TextBox`, Text/Art color `ComboBox`, Continue prompt `CheckBox`,
      Narration and ASCII Art multi-line `TextBox` editors
    - Status bar (`#007ACC` blue) with live `StatusText`
  - Avalonia compiled bindings (`x:DataType`) throughout — zero runtime reflection binding

---

## [0.3.0] — 2026-05-26

### Added

- **ConsoleEngine.World** — world movement framework and exploration HUD
  - `TimeOfDay` enum (`Morning / Afternoon / Evening / Night`) with `Next()` and `LocaleKey()` extension methods
  - `LocationDefinition` — node in the world graph: ID, name, description, ASCII art, PNG sprite, exits dictionary
  - `WorldMap` — immutable indexed collection with `Get`, `TryGet`, `TryMove` (O(1) look-ups)
  - `WorldState` — mutable snapshot of current location + time + day; `AdvanceTime(steps)` wraps Night→Morning
  - `ExplorationResult` — `ReturnToMenu | Quit`
  - `IExplorationAction` + `ExplorationOutcome` — extension point for custom per-location commands
  - `ExplorationOptions` — text/HUD colours, move transition, `ShowExits`, `CustomActions`, `OnLocationEnter` callback
  - `ExplorationPlayer.Run(map, state, options)` — full interactive exploration loop with:
    - HUD bar (location · time of day · day number · key hints)
    - Bottom-anchored ASCII art / PNG sprite in content area
    - Flowing description, exits list, and feedback messages
    - Built-in commands: cardinal directions, `look`, `exits`, `wait`, `help`, `menu`, `quit`
    - Direction aliases: `n/s/e/w/u/d` → canonical names
    - Optional animated transitions between locations (`TransitionType`)
- **`CK.World`** — locale key constants for world commands added to `CK.cs`
  - `Exits`, `NoExits`, `NoExit`, `Waited`, `UnknownCmd`
- **samples/WorldDemo** — five-room interactive exploration demo
  - Locations: Crossroads → Forest → Hilltop, Crossroads → Ruins → Cave
  - Intro screen via `ScenePlayer`, outro via `ScenePlayer`
  - `FadeToBlack` transition between locations
  - Custom action (`look up`) available only on the hilltop — shows `IExplorationAction` usage
  - English and Spanish locale files

## [0.2.0] — 2026-05-26

### Added

- **ConsoleEngine.Scenes** — scene system, dialogue player, and transition engine
  - `SceneDefinition` — data model for narrative scenes (ASCII art, PNG sprite, narration lines)
  - `DialogueDefinition` — two-character dialogue data model with optional PNG sprite for left character
  - `TransitionType` — enum of available transition effects
  - `ScenePlayer.Play(SceneDefinition)` — two-pass renderer: bottom-anchored art, top-flowing text
  - `DialoguePlayer.Play(DialogueDefinition)` — renders two characters at the bottom with dialogue at the top
  - `TransitionEngine.Out(TransitionType)` — animated screen transitions:
    - `Cut` — instant clear
    - `FadeToBlack` — fills rows with ░→▒→▓→█ in 4 passes
    - `WipeDown` — black curtain sweeps from top to bottom
    - `WipeUp` — black curtain sweeps from bottom to top
    - `CloseIn` — darkness closes from all four edges toward the centre
- **samples/DinoGame** — Chrome-Dino-style terminal game showcasing v0.2.0
  - Smooth jump physics with gravity arc (≈ 1 second airborne)
  - Scrolling cacti that get faster over time
  - Animated dino running sprite (2-frame) and dead sprite
  - Background clouds with parallax scroll (0.35× obstacle speed)
  - HUD with live score + high score
  - Game-over overlay with restart/quit
  - Intro screen built with `ScenePlayer`
  - English and Spanish locale files

## [0.1.0] — 2026-05-26

### Added

- **ConsoleEngine.Core** — `ILocalizationService`, `LocalizationTable`, `EngineVersion`
- **ConsoleEngine.Locale** — `CL` static façade, `CK` base key constants, `MarkdownLocalizationLoader`, `InMemoryLocalizationService`
  - Markdown-driven locale files (`en.md`, `es.md`, …)
  - Hot-reload via repeated `CL.Initialize()` calls
  - 11 supported languages: en, ja, zh-Hans, es, pt, de, ru, it, fr, ca, ko
  - Graceful degradation: returns raw key when service is not initialised
- **ConsoleEngine.Rendering** — `PixelArtRenderer`, `AnimationEngine`
  - PNG → ANSI half-block (`▀`) rendering with truecolor (32×32 px = 32 chars × 16 rows)
  - `RenderRgb` for char-map sprites with no external files
  - `BuildSprite` helper for palette-based sprite construction
  - `EnableAnsi` for Windows Virtual Terminal Processing
  - `AnimationEngine`: `DrawAt`, `DrawSprite`, `ClearRect`, `Sleep`, cursor hide/show
- **ConsoleEngine.Config** — `GameConfig`, `GameSettingsCatalog`, `GameSettingsCommands`, `ResolutionPreset`
  - Shared config consumed by CLI runtime and Unity/Godot front-end
  - Settings: language, 5 audio channels, focus-loss mute, display mode, aspect ratio, resolution
  - Language shorthand resolver (`en` or `English` both work)
  - Resolution presets for 4:3, 16:9, 16:10
- **ConsoleEngine.Persistence** — `GameConfigRepository`, `SaveRepository<T>`
  - JSON-based config and slot-based save system
  - Automatic normalisation of config values on load
- **samples/HelloConsoleEngine** — minimal working example showing locale + pixel art rendering

### Notes

- First public release. Extracted from [AkashicEnd](https://github.com/maxiusofmaximus/AkashicEnd).
- The editor (`ConsoleEngine.Editor`) and the scene/animation systems are planned for v0.2.0.
