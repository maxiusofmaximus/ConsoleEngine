# 👾 ConsoleEngine

> A .NET 8 framework for building terminal / CLI games with pixel-art sprites,
> Markdown-driven localisation, animation, and player-editable content.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blueviolet)](https://dotnet.microsoft.com)
[![Version](https://img.shields.io/badge/version-0.3.0-blue)](CHANGELOG.md)

---

## What is ConsoleEngine?

ConsoleEngine is the engine that powers [AkashicEnd](https://github.com/maxiusofmaximus/AkashicEnd) — a tactical auto-battle RPG.
It handles everything that is not game-specific:

| Module | What it does |
|---|---|
| `ConsoleEngine.Core` | Shared contracts and interfaces |
| `ConsoleEngine.Locale` | Markdown locale files, 11 languages, hot-reload |
| `ConsoleEngine.Rendering` | PNG → ANSI half-block pixel art, animation primitives |
| `ConsoleEngine.Config` | Shared game config (language, audio, display) |
| `ConsoleEngine.Persistence` | JSON save slots and config persistence |
| `ConsoleEngine.Scenes` | Scene player, dialogue player, transition effects |
| `ConsoleEngine.World`  | Location graph, time system, exploration HUD loop |

---

## Quick Start

### 1. Reference the projects

```xml
<!-- YourGame.csproj -->
<ItemGroup>
  <ProjectReference Include="path/to/ConsoleEngine.Core/ConsoleEngine.Core.csproj" />
  <ProjectReference Include="path/to/ConsoleEngine.Locale/ConsoleEngine.Locale.csproj" />
  <ProjectReference Include="path/to/ConsoleEngine.Rendering/ConsoleEngine.Rendering.csproj" />
  <ProjectReference Include="path/to/ConsoleEngine.Config/ConsoleEngine.Config.csproj" />
  <ProjectReference Include="path/to/ConsoleEngine.Persistence/ConsoleEngine.Persistence.csproj" />
</ItemGroup>
```

### 2. Create your locale file

`GameData/locale/en.md`:

```markdown
# Locale: en

## Entries

| Key              | Value              |
|------------------|--------------------|
| menu.title       | My Terminal Game   |
| menu.new_game    | 1. New Game        |
| menu.exit        | 2. Exit            |
| world.forest     | Dark Forest        |
```

### 3. Initialise and use

```csharp
using ConsoleEngine.Locale;
using ConsoleEngine.Rendering;

// Enable ANSI truecolor (Windows only, safe on other platforms)
PixelArtRenderer.EnableAnsi();

// Load locale from GameData/locale/
CL.Initialize("GameData", language: "en");

// Render a pixel-art sprite (no PNG needed — char-map palette)
var palette = new Dictionary<char, PixelArtRenderer.Rgb>
{
    ['#'] = new(255, 200, 50),  // gold
    ['.'] = new(0, 0, 0),       // transparent
};
string[] map = { ".###.", "#####", "#####", ".###.", "..#.." };
var sprite = PixelArtRenderer.BuildSprite(map, palette, transparent: new(0,0,0));
PixelArtRenderer.RenderRgb(sprite, col: 4, row: 2);

// Localised text
Console.SetCursorPosition(0, 6);
Console.WriteLine(CL.Get("menu.title"));
```

### 4. Run the samples

```bash
# Minimal pixel-art + locale demo
cd samples/HelloConsoleEngine && dotnet run

# Chrome-Dino-style terminal game (v0.2.0)
cd samples/DinoGame && dotnet run

# Five-room interactive exploration (v0.3.0)
cd samples/WorldDemo && dotnet run
```

---

## Localisation

ConsoleEngine uses Markdown tables as locale files — human-readable, diff-friendly, and editable without tools.

```markdown
| Key            | Value              |
|----------------|--------------------|
| menu.title     | My Game            |
| greeting       | Hello, {0}!        |
```

**Supported languages out of the box:** English, Japanese, Simplified Chinese, Spanish, Portuguese,
German, Russian, Italian, French, Catalan, Korean.

Add a new language by creating `GameData/locale/{code}.md`. Only override the keys you need —
missing keys fall back to English automatically.

**Hot-reload:** call `CL.Initialize(gameDataRoot, newLanguage)` at any time to switch language
without restarting the game.

---

## Pixel Art Rendering

ConsoleEngine renders PNG sprites in the terminal using Unicode `▀` half-block characters and
ANSI truecolor escape sequences.

```
PNG 32×32 pixels  →  32 chars wide × 16 terminal rows
PNG 16×16 pixels  →  16 chars wide × 8 terminal rows
```

**From a PNG file:**
```csharp
PixelArtRenderer.RenderPng("Sprites/hero.png", col: 10, row: 5);
```

**From a char-map (no external file):**
```csharp
var sprite = PixelArtRenderer.BuildSprite(rows, palette, transparent);
PixelArtRenderer.RenderRgb(sprite, col: 10, row: 5);
```

---

## Animation

```csharp
AnimationEngine.HideCursor();

for (int frame = 0; frame < frameCount; frame++)
{
    AnimationEngine.ClearRect(x, y, width, height);
    AnimationEngine.DrawSprite(frames[frame], x, y, ConsoleColor.White);
    AnimationEngine.Sleep(frameDurationMs: 80);
}

AnimationEngine.ShowCursor();
```

---

## Configuration

ConsoleEngine persists a shared `GameConfig` as JSON — consumed by both the CLI runtime and any
Unity/Godot front-end.

```csharp
var repo   = new GameConfigRepository("Config");
GameConfig config = repo.Load();   // creates default config.json if absent

config.Language = "es";
config.MusicVolume = 60;
repo.Save(config);
```

**Settings:** language, master/music/SFX/ambience/UI volumes, focus-loss mute,
display mode (windowed/borderless/fullscreen), aspect ratio (4:3 / 16:9 / 16:10), resolution.

---

## World Movement & Exploration

```csharp
using ConsoleEngine.World;

// ── Build a world map ──────────────────────────────────────────────────────────
var map = new WorldMap(new[]
{
    new LocationDefinition
    {
        Id          = "town",
        Name        = "Market Town",
        Description = new[] { "Merchants call out their wares.", "Children chase a dog." },
        AsciiArt    = new[] { "  ╔═╗  ╔═╗  ", "  ║ ║  ║ ║  ", "──╚═╝──╚═╝──" },
        ArtColor    = ConsoleColor.DarkYellow,
        Exits       = new Dictionary<string, string> { ["north"] = "forest" },
    },
    new LocationDefinition
    {
        Id          = "forest",
        Name        = "Dark Forest",
        Description = new[] { "The trees close in.", "Something watches." },
        Exits       = new Dictionary<string, string> { ["south"] = "town" },
    },
});

// ── Start exploring ────────────────────────────────────────────────────────────
var state  = new WorldState { CurrentLocationId = "town", Day = 1 };
var result = ExplorationPlayer.Run(map, state, new ExplorationOptions
{
    MoveTransition = TransitionType.WipeDown,
});

// result == ExplorationResult.ReturnToMenu or ExplorationResult.Quit
```

**Built-in commands the player can type:** `north / n`, `south / s`, `east / e`, `west / w`,
`up / u`, `down / d`, `in`, `out`, `go <dir>`, `look`, `exits`, `wait`, `help`, `menu`, `quit`.

**`IExplorationAction`** lets you add custom commands per-location (talk to an NPC, buy from a shop, rest, etc.).

---

## Scenes, Dialogue & Transitions

```csharp
using ConsoleEngine.Scenes;

// ── Narrative scene ─────────────────────────────────────────────────────────
ScenePlayer.Play(new SceneDefinition
{
    Title    = "Chapter 1 — The Forest",
    AsciiArt = new[]
    {
        "    /\\  /\\  /\\   ",
        "   /  \\/  \\/  \\  ",
        "══════════════════",
        "▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓",
    },
    Lines    = new[] { "The forest was silent.", "Too silent." },
    TextColor = ConsoleColor.Gray,
    ArtColor  = ConsoleColor.DarkGreen,
});

// ── Two-character dialogue ────────────────────────────────────────────────────
DialoguePlayer.Play(new DialogueDefinition
{
    LeftLabel  = "KIM",
    LeftColor  = ConsoleColor.Cyan,
    LeftArt    = new[] { " o ", "/|\\", "/ \\" },
    RightLabel = "SCOUT",
    RightColor = ConsoleColor.Yellow,
    RightArt   = new[] { " o ", "\\|/", "\\ /" },
    Lines      = new[] { "KIM: Did you hear that?", "SCOUT: ... yeah." },
});

// ── Transition out before drawing the next scene ─────────────────────────────
TransitionEngine.Out(TransitionType.WipeDown);
ScenePlayer.Play(nextScene);
```

---

## Save System

```csharp
// Define a save model
record MySession(string SaveSlotId, string PlayerName, int Day);

// Create the repository
var saves = new SaveRepository<MySession>("Saves", s => s.SaveSlotId);

// Save
saves.Save(new MySession("slot-001", "Kim", 3));

// Load most recent
MySession loaded = saves.LoadMostRecent();
```

---

## Project Structure

```
ConsoleEngine/
  src/
    ConsoleEngine.Core/         ← interfaces, LocalizationTable, EngineVersion
    ConsoleEngine.Locale/       ← CL, CK, MarkdownLocalizationLoader, InMemoryLocalizationService
    ConsoleEngine.Rendering/    ← PixelArtRenderer, AnimationEngine
    ConsoleEngine.Config/       ← GameConfig, GameSettingsCatalog, GameSettingsCommands
    ConsoleEngine.Persistence/  ← SaveRepository<T>, GameConfigRepository
    ConsoleEngine.Scenes/        ← ScenePlayer, DialoguePlayer, TransitionEngine
    ConsoleEngine.World/         ← LocationDefinition, WorldMap, ExplorationPlayer
  samples/
    HelloConsoleEngine/         ← minimal working example (locale + pixel art)
    DinoGame/                   ← Chrome-Dino-style game (v0.2.0 showcase)
    WorldDemo/                  ← five-room exploration demo (v0.3.0 showcase)
  ConsoleEngine.sln
```

---

## Roadmap

| Version | Planned features |
|---|---|
| **0.1.0** ✅ | Core, Locale, Rendering, Config, Persistence |
| **0.2.0** ✅ | Scene system, dialogue player, transition engine, DinoGame sample |
| **0.3.0** ✅ | World movement framework, exploration HUD, WorldDemo sample |
| 0.4.0 | ConsoleEngine.Editor (Avalonia) — visual scene editor |
| 1.0.0 | Stable API, NuGet packages, full documentation |

---

## License

MIT — see [LICENSE](LICENSE).
Forks are welcome. Please retain the original author credit (`maxiusofmaximus`) in your LICENSE file.

---

## Built with ConsoleEngine

- **[AkashicEnd](https://github.com/maxiusofmaximus/AkashicEnd)** — Tactical auto-battle RPG. The game this engine was extracted from.

---

*Made with ❤️ by [maxiusofmaximus](https://github.com/maxiusofmaximus)*
