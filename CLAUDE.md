# ConsoleEngine — Project Context for AI Sessions

## What This Is

ConsoleEngine is a reusable .NET 8 framework for building terminal/CLI games.
It was extracted from **AkashicEnd** (tactical auto-battle RPG) and is now an
independent library publishable on NuGet. Current version: **0.6.0**.

AkashicEnd is the primary consumer and validation target. The engine knows
nothing about AkashicEnd — all game logic lives in the game project.

---

## Repository Layout

```
ConsoleEngine/
  src/
    ConsoleEngine.Core/         ← contracts, interfaces, EngineVersion  (no deps)
    ConsoleEngine.Rendering/    ← PixelArtRenderer (PNG→▀), AnimationEngine
    ConsoleEngine.Locale/       ← CL/CK, MarkdownLocalizationLoader, InMemoryLocalizationService
    ConsoleEngine.Scenes/       ← SceneDefinition, SceneLoader, ScenePlayer, DialoguePlayer
    ConsoleEngine.World/        ← WorldState, WorldMap, ExplorationPlayer, IExplorationAction
    ConsoleEngine.Config/       ← GameConfig, GameSettingsCatalog, GameSettingsCommands
    ConsoleEngine.Persistence/  ← SaveRepository<T>, GameConfigRepository
    ConsoleEngine.Input/        ← ConsoleInputProvider, MockInputProvider (IInputProvider)
    ConsoleEngine.SceneRunner/  ← standalone exe: takes .scene.json path, calls ScenePlayer.Play()
    ConsoleEngine.Editor/       ← Avalonia 12 desktop editor (Windows-first)
    ConsoleEngine.Tests/        ← xUnit test project (56 tests)
  docs/
    ENGINE_PLAN.md              ← module architecture and roadmap
    EDITOR_PLAN.md              ← 20-module editor plan with Phase A status
    MIGRATION_PLAN.md           ← AkashicEnd → ConsoleEngine extraction record
  samples/
    DinoGame/                   ← minimal demo project
  ConsoleEngine.sln
```

---

## Module Dependency Rules

```
Core ← Rendering, Locale, Scenes, World, Config, Persistence, Editor
Scenes ← Locale, Rendering
World  ← Locale
Editor ← Core, Scenes   (never references game projects)
SceneRunner ← Scenes
```

**Core has zero dependencies.** All other modules depend on Core only.
Game projects depend on engine modules; engine modules never depend on game projects.

---

## Design Principles — Applied Here

### DRY
- `CL.Get(CK.*)` is the single call site for all localized strings — never
  hardcode display text in C#.
- `AnimationEngine.DrawAt()` is the single primitive for all terminal drawing.
- `SceneLoader` owns JSON→`SceneDefinition` conversion; no other class parses scene files.

### SRP (Single Responsibility)
- `ScenePlayer` renders, `SceneLoader` parses, `SceneDefinition` is pure data.
- `MainViewModel` is pure state/logic; `MainWindow.axaml.cs` is pure UI event routing.

### OCP (Open/Closed)
- `IExplorationAction` lets games add exploration commands without touching the engine loop.
- `CK` subclassing lets games add locale keys without modifying base engine keys.

### LSP (Liskov Substitution)
- `ExplorationOutcome` uses static factory methods (`Continue`, `ReturnToMenu`, `Quit`)
  so every outcome is structurally valid — no subclass can violate the contract.
- `SaveRepository<T>` is generic; any serializable `T` works identically.

### ISP (Interface Segregation)
- `ILocalizationService` has exactly 5 members. It is not a god interface.
- `IExplorationAction` has exactly 4 members: Command, Description, IsAvailable, Execute.
- Never inflate interfaces with convenience methods — put those in extension methods or
  a static facade (`CL`, `AnimationEngine`).

### DIP (Dependency Inversion)
- `ScenePlayer` calls `CL.Get()` (the abstraction) — never `InMemoryLocalizationService` directly.
- The editor `MainViewModel` accepts no concrete infrastructure — all IO goes through
  plain `File.*` calls, which are easy to swap later with `IFileSystem` if needed.

### KISS
- Prefer `sealed` classes over inheritance hierarchies.
- Prefer `static class` + factory methods over complex constructors.
- Prefer `record` for pure data (immutable, value semantics) — `SceneDefinition` is a record.
- Use `sealed class` for mutable models with behavior — `WorldState`, `SceneDocument`.
- No framework magic (no DI container yet) — wire things explicitly at the entry point.
- When adding a new property to `SceneDefinition`, also add it to `SceneDocument` and
  `SceneLoader.SceneDto.ToDefinition()` in the same commit.

---

## Key Data Format — `.scene.json`

```json
{
  "title":          "Chapter 1",
  "lines":          ["Line one.", "Line two."],
  "asciiArt":       ["  /\\  ", "══════", "▓▓▓▓▓▓"],
  "artColor":       "DarkGreen",
  "textColor":      "Gray",
  "spritePath":     null,
  "spriteWidth":    32,
  "spriteRows":     16,
  "promptContinue": true,
  "continuePrompt": null
}
```

- `artColor` / `textColor` accept any `System.ConsoleColor` name (case-insensitive).
- `spritePath` is relative to the project root or absolute.
- `continuePrompt: null` → falls back to `CK.Prompt.Continue` locale key.
- `SceneDocument` (editor) and `SceneDefinition` (runtime) must always have identical fields.
  If you add a field to one, add it to both.

---

## Editor (ConsoleEngine.Editor) — Current Status

**Implemented (Phase A partial):**
- Toolbar: Open Project, New Scene, Save, Reload, Play, Stop, Duplicate, Delete
- Left panel: scene file list (`*.scene.json`) with bin/obj artifact filtering
- Center panel: text-based terminal preview (live, re-renders on every keystroke)
- Right panel: title, text color, art color, continue-prompt toggle, narration lines, ASCII art
- Play launches `ConsoleEngine.SceneRunner.exe` (or `dotnet run` fallback) in Windows Terminal
- Stop kills the process tree
- Reload rescans the project folder and reselects the previously active scene
- Duplicate copies as `stem_copy.scene.json` (with incrementing suffix to avoid collision)
- Delete shows inline `ShowDialog` confirmation, then deletes file and removes from list

**Not yet implemented (Phase A remaining):**
- Embedded terminal panel (WebView2 + xterm.js — AI CLI integration)
- Sprite import and ANSI preview inside the editor
- Reorder scenes with drag & drop

**Phase B and beyond:**
See `docs/EDITOR_PLAN.md` for the full 20-module plan.

---

## Avalonia Conventions (Editor only)

- Compiled bindings: every `DataTemplate` has `x:DataType="..."`.
- ViewModel exposes `INotifyPropertyChanged` via `Set<T>` helper (no external MVVM framework).
- All UI state lives in `MainViewModel`; code-behind only routes events.
- Use `AvaloniaResource` (not `EmbeddedResource`) for assets (icons, images).
- Window icons: `Icon="avares://ConsoleEngine.Editor/Assets/Icon.png"`.
- No external dialog packages — build inline `Window` instances for lightweight modals.

---

## Build and Run

```bash
# Full solution
dotnet build ConsoleEngine.sln --configuration Release

# Editor only (also builds SceneRunner via MSBuild AfterBuild target)
dotnet build src/ConsoleEngine.Editor/ConsoleEngine.Editor.csproj

# Run editor
dotnet run --project src/ConsoleEngine.Editor/ConsoleEngine.Editor.csproj

# Run a scene directly
dotnet run --project src/ConsoleEngine.SceneRunner/ConsoleEngine.SceneRunner.csproj -- path/to/scene.scene.json
```

---

## Conventions

| Rule | Detail |
|---|---|
| Namespaces | `ConsoleEngine.<Module>` — matches project name |
| Classes | `sealed` by default; only unseal when inheritance is designed |
| Data models | `record` for immutable data, `sealed class` for mutable state |
| Interfaces | `I` prefix, narrow (≤6 members), in `ConsoleEngine.Core` |
| Locale keys | Defined in `CK.*` constants; never use raw strings |
| Colors | `ConsoleColor` enum for 16-color; ANSI truecolor via `PixelArtRenderer.Rgb` |
| File I/O | Always `UTF-8` encoding; `JsonSerializerOptions.AllowTrailingCommas = true` |
| Comments | Only when the WHY is non-obvious; no restating of what the code does |
| No warnings | All builds must produce 0 warnings — treat warnings as errors mentally |

---

## Context7 Usage

Use Context7 (`use_mcp_tool context7`) to look up:
- **Avalonia 12** — binding syntax, controls, themes, compiled bindings, `ShowDialog`
- **System.Text.Json** — `JsonSerializer`, source generators, custom converters
- **MSBuild** — `AfterTargets`, `ItemGroup`, `Copy` task, `Condition` attributes
- **.NET 8 APIs** — anything in `System.*` that may have changed since training data

Prefer Context7 over recalled training data for any Avalonia API — the framework
changes significantly between versions.

---

## What to Do Next

Run `/next-task` to get the next actionable item from `docs/EDITOR_PLAN.md`.
Run `/editor-status` for a full Phase A completion summary.
Run `/solid-audit <filepath>` before committing any non-trivial file.
