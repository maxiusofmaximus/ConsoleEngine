# Changelog

All notable changes to ConsoleEngine are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

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
