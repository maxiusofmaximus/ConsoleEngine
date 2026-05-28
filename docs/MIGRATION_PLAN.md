# ![ConsoleEngine](../Icon.png) ConsoleEngine — Migration Plan

> Steps to extract the engine layer from AkashicEnd and establish the
> ConsoleEngine / AkashicEnd separation described in `ENGINE_PLAN.md`.

---

## Goal

Transform the current monolithic structure:

```
AkashicEnd/
  Assets/Scripts/         ← game logic + engine logic mixed
  Tools/OpenLauncher/     ← launcher + rendering + locale + config mixed
```

Into a clean two-repository (or two-solution) structure:

```
ConsoleEngine/            ← reusable framework
AkashicEnd/               ← game project, references ConsoleEngine
```

The game must **build and run identically** before and after the migration.
No features are added or removed during the migration — it is purely structural.

---

## Current State Inventory

### What lives in `Tools/OpenLauncher/` today

| File | Engine or Game? | Target module |
|---|---|---|
| `PixelArtRenderer.cs` | Engine | `ConsoleEngine.Rendering` |
| `ConsoleArt.cs` | Engine | `ConsoleEngine.Rendering` |
| `CharacterArt.cs` | Engine (base arrays) + Game (Kim, Allen) | Split: base → Engine, characters → Game |
| `EmbeddedSprites.cs` | Game | `AkashicEnd.Content` |
| `ConsoleLocale.cs` (CL) | Engine | `ConsoleEngine.Locale` |
| `ConsoleLocalizationKeys.cs` (CK) | Engine (base) + Game (game keys) | Split: base → Engine, game keys → Game |
| `AttackAnimation.cs` | Game (AkashicEnd-specific) | `AkashicEnd.Content` |
| `IntroScene.cs` | Game (AkashicEnd-specific) | `AkashicEnd.Content` |
| `WorldMovement.cs` | Engine (framework) + Game (content) | Split: framework → Engine, content → Game |
| `AnimationEngine.cs` | Engine | `ConsoleEngine.Animation` |
| `Program.cs` | Launcher (mostly engine) | `ConsoleEngine.Launcher` (base) + Game override |
| `OpenGameArguments.cs` | Engine | `ConsoleEngine.Launcher` |

### What lives in `Assets/Scripts/` today

| Namespace | Engine or Game? | Target module |
|---|---|---|
| `AkashicEnd.Core.Models` (GameConfig, GameSettingsCatalog, GameSettingsCommands) | Engine | `ConsoleEngine.Config` |
| `AkashicEnd.Core.Models` (GameConfig.LanguageLabels, SupportedLanguages) | Engine | `ConsoleEngine.Config` |
| `AkashicEnd.Core.Models` (other game models) | Game | `AkashicEnd.Models` |
| `AkashicEnd.Gameplay.Session` | Game | `AkashicEnd.Mechanics` |
| `AkashicEnd.Persistence.Json` | Engine (generic pattern) | `ConsoleEngine.Persistence` |
| `AkashicEnd.Persistence.Markdown` | Engine | `ConsoleEngine.Persistence` |
| `AkashicEnd.Domain` | Game | `AkashicEnd.Models` |

### What lives in `Assets/StreamingAssets/GameData/` today

All content stays in the game project — the engine only defines the format:

| Folder | Stays in |
|---|---|
| `GameData/locale/en.md`, `es.md` | `AkashicEnd.Content` |
| `GameData/locations/*.md` | `AkashicEnd.Content` |
| `GameData/dialogue/*.md` | `AkashicEnd.Content` |
| `Saves/` | `AkashicEnd` (runtime) |
| `Config/config.json` | `AkashicEnd` (runtime) |

---

## Migration Steps

### Step 0 — Preparation

- [ ] Create a git branch: `feature/console-engine-extraction`
- [ ] Verify the current build is clean (0 warnings, 0 errors)
- [ ] Run all existing tests — record baseline pass count
- [ ] Snapshot the current `Tools/OpenLauncher/` and `Assets/Scripts/` as reference

---

### Step 1 — Create the ConsoleEngine solution

- [ ] Create `ConsoleEngine/` directory at the same level as `AkashicEnd/`
- [ ] Create `ConsoleEngine/ConsoleEngine.sln`
- [ ] Create empty projects (class libraries, .NET 8):
  - `ConsoleEngine.Core`
  - `ConsoleEngine.Rendering`
  - `ConsoleEngine.Locale`
  - `ConsoleEngine.Scenes`
  - `ConsoleEngine.Animation`
  - `ConsoleEngine.World`
  - `ConsoleEngine.Config`
  - `ConsoleEngine.Persistence`
  - `ConsoleEngine.Launcher`
- [ ] Add all projects to the solution
- [ ] Verify the empty solution builds

---

### Step 2 — Migrate ConsoleEngine.Config (lowest risk, no rendering dependencies)

Files to move from `Assets/Scripts/Core/Models/`:
- `GameConfig.cs` → `ConsoleEngine.Config`
- `GameSettingsCatalog.cs` → `ConsoleEngine.Config`
- `GameSettingsCommands.cs` → `ConsoleEngine.Config`

Actions:
- [ ] Copy files to `ConsoleEngine.Config/`
- [ ] Change namespace from `AkashicEnd.Core.Models` to `ConsoleEngine.Config`
- [ ] In `Tools/OpenLauncher/OpenLauncher.csproj`, replace the DLL reference with a project reference to `ConsoleEngine.Config`
- [ ] Fix any namespace imports in OpenLauncher
- [ ] Build and run — game must behave identically

---

### Step 3 — Migrate ConsoleEngine.Persistence

Files to move:
- `SaveRepository.cs` → `ConsoleEngine.Persistence`
- `GameConfigRepository.cs` → `ConsoleEngine.Persistence`
- `MarkdownLocalizationLoader.cs` → `ConsoleEngine.Locale` (see Step 4)
- `DialogueMarkdownParser.cs` → `ConsoleEngine.Persistence`

Actions:
- [ ] Copy files, update namespaces
- [ ] Replace DLL references with project references
- [ ] Build and run

---

### Step 4 — Migrate ConsoleEngine.Locale

Files to move from `Tools/OpenLauncher/`:
- `ConsoleLocale.cs` (CL) → `ConsoleEngine.Locale`
- `ILocalizationService.cs` → `ConsoleEngine.Core` (interface)
- `InMemoryLocalizationService.cs` → `ConsoleEngine.Locale`
- `MarkdownLocalizationLoader.cs` → `ConsoleEngine.Locale`

Split `ConsoleLocalizationKeys.cs` (CK):
- [ ] Create `ConsoleEngine.Locale/BaseLocalizationKeys.cs` with engine-level keys only (`cli.prompt.*`, `cli.status.*`, `cli.menu.*`, `cli.options.*`, `cli.error.*`, `cli.session.*`, `cli.walk.*`)
- [ ] Keep `ConsoleLocalizationKeys.cs` in AkashicEnd, extending the base class with game-specific keys (`intro.*`, `cli.location.*`)

Actions:
- [ ] Move and rename files, update namespaces
- [ ] AkashicEnd references `ConsoleEngine.Locale`
- [ ] Build and run

---

### Step 5 — Migrate ConsoleEngine.Rendering

Files to move from `Tools/OpenLauncher/`:
- `PixelArtRenderer.cs` → `ConsoleEngine.Rendering`
- `AnimationEngine.cs` → `ConsoleEngine.Animation`

Split `ConsoleArt.cs`:
- [ ] `ConsoleEngine.Rendering/ConsoleArtBase.cs` — engine-level rendering (status header skeleton, menu skeleton, prompts, error/success lines)
- [ ] `AkashicEnd/ConsoleArt.cs` — game-specific rendering (Morim shop layout, session header with AkashicEnd fields), extends `ConsoleArtBase`

Split `CharacterArt.cs`:
- [ ] `ConsoleEngine.Rendering/CharacterArt.cs` — empty registry + `Register(name, art[])` + `Get(name)`
- [ ] `AkashicEnd/Content/Characters/CharacterArt.cs` — registers Kim, Allen, Forest, SeoulCity art arrays on startup

Actions:
- [ ] Move, split, update references
- [ ] Build and run

---

### Step 6 — Migrate ConsoleEngine.World

Split `WorldMovement.cs`:
- [ ] `ConsoleEngine.World/WorldMovementBase.cs` — rendering engine (HUD, ground rows, terminal layout, movement loop)
- [ ] `AkashicEnd/WorldMovement.cs` — passes AkashicEnd location names, sprites, session state to the base class

Actions:
- [ ] Extract the pure rendering/loop logic to engine
- [ ] Keep AkashicEnd content references in the game project
- [ ] Build and run

---

### Step 7 — Migrate ConsoleEngine.Launcher

Split `Program.cs`:
- [ ] `ConsoleEngine.Launcher/LauncherBase.cs` — `RunOptionsLoop`, `ApplySharedSetting`, `ResolveLanguageShorthand`, `FindRepositoryRoot`, `LaunchUi`
- [ ] `AkashicEnd/Launcher/Program.cs` — game entry point: wires `GameDataRepository`, `GameCommandProcessor`, calls `LauncherBase.Run()`

Move `OpenGameArguments.cs` → `ConsoleEngine.Launcher`

Actions:
- [ ] Split and move
- [ ] Build and run

---

### Step 8 — Migrate ConsoleEngine.Scenes and AkashicEnd content

Create scene data model:
- [ ] `ConsoleEngine.Scenes/SceneDefinition.cs`
- [ ] `ConsoleEngine.Scenes/ScenePlayer.cs` (extracted from `IntroScene.Scene()` and `IntroScene.Dialogue()`)
- [ ] `ConsoleEngine.Scenes/TransitionEngine.cs`

Move AkashicEnd-specific scenes:
- [ ] `AkashicEnd/Content/Scenes/IntroScene.cs` — now uses `ScenePlayer` from engine, contains only AkashicEnd narrative content
- [ ] `AkashicEnd/Content/Animations/AttackAnimation.cs` — uses `AnimationPlayer` from engine

Actions:
- [ ] Extract rendering logic to engine
- [ ] Keep narrative content (lines, art references, name prompt) in game project
- [ ] Build and run

---

### Step 9 — Final verification

- [ ] Full build of `ConsoleEngine.sln` — 0 warnings, 0 errors
- [ ] Full build of `AkashicEnd.sln` — 0 warnings, 0 errors
- [ ] All existing tests pass
- [ ] Run the game: complete the intro, explore, options, language switch — all must work identically to pre-migration
- [ ] No AkashicEnd-specific references remain in any `ConsoleEngine.*` project
- [ ] No engine implementation files remain in `AkashicEnd` (only extensions and overrides)

---

### Step 10 — Cleanup and documentation

- [ ] Delete old `Tools/OpenLauncher/` files that were moved (not just copied)
- [ ] Update `AkashicEnd.sln` project references
- [ ] Update `CLAUDE.md` to mention `ConsoleEngine`
- [ ] Add a `README.md` to `ConsoleEngine/`
- [ ] Update `GDD.md` and `ARCHITECTURE.md` to reflect the new structure
- [ ] Merge `feature/console-engine-extraction` branch

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Unity DLL references break when namespaces change | High | Medium | Update `Library/ScriptAssemblies` references; Unity rebuilds on next open |
| `CharacterArt` split breaks intro scene rendering | Medium | High | Test intro scene specifically after Step 5 |
| `CK` key constants split causes missing keys at runtime | Medium | High | Run locale validation test after Step 4 |
| `WorldMovement` base/override split misses a case | Low | Low | The rendering is well-tested visually; run walk mode after Step 6 |
| Build order issues between ConsoleEngine projects | Low | Low | Set explicit `ProjectReference` dependencies in `.csproj` files |

---

## Post-Migration: What AkashicEnd.csproj looks like

```xml
<ItemGroup>
  <!-- Engine modules -->
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Core\ConsoleEngine.Core.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Rendering\ConsoleEngine.Rendering.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Locale\ConsoleEngine.Locale.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Scenes\ConsoleEngine.Scenes.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Animation\ConsoleEngine.Animation.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.World\ConsoleEngine.World.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Config\ConsoleEngine.Config.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Persistence\ConsoleEngine.Persistence.csproj" />
  <ProjectReference Include="..\..\ConsoleEngine\src\ConsoleEngine.Launcher\ConsoleEngine.Launcher.csproj" />
</ItemGroup>
```

When the engine API is stable, replace `ProjectReference` with `PackageReference` pointing to the NuGet packages.

---

**Created**: 2026-05-26
**Author**: AkashicEnd Development Team
**Prerequisite**: `ENGINE_PLAN.md` reviewed and approved
**Status**: Planning — awaiting Phase 1 start
