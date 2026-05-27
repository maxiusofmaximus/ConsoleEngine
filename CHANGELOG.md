# Changelog

All notable changes to ConsoleEngine are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [1.0.0] — 2026-05-27

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
- **`EngineVersion`** updated to `1.0.0`

### Fixed

- `CS0419` ambiguous cref warnings in `ILocalizationService` and `CL` XML docs
- `CS1734` invalid `<paramref>` in `SaveRepository<T>` class-level doc

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
