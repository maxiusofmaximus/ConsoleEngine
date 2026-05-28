# ConsoleEngine.Editor — Plan

> Avalonia-based visual editor for projects built on ConsoleEngine.
> Zero-code scene authoring, drag-and-drop asset management, visual scripting,
> embedded AI terminal, and full localization workflow.

---

## Technology

- **Framework**: .NET 8 + Avalonia (cross-platform, Windows-first)
- **Embedded terminal**: WebView2 + xterm.js (for the AI CLI panel)
- **Node graph**: `NodeNetwork` or `Avalonia.NodeEditor`
- **Shared models**: direct project reference to `ConsoleEngine.Core`
- **Data format**: reads/writes the same JSON + Markdown files the game runtime uses
- **No game engine required**: standalone desktop app, no Unity or Godot needed

---

## Module 1 — Scene Management

- [x] Create new scene (name, type: narration / dialogue / animation / transition)
- [x] List all project scenes with order, type, and health status
- [ ] Reorder scenes with drag & drop
- [x] Duplicate scene
- [x] Delete scene with confirmation
- [x] Preview single scene in terminal simulator
- [ ] Preview full sequence (playback from any starting scene)
- [x] **Play mode** — run the sequence from the active scene in the terminal simulator
- [x] **Stop mode** — halt execution and return to the editing frame
- [x] **Reload mode** — hot-reload all data files without closing the editor
- [ ] A/B comparator — two versions of a scene side by side for visual comparison
- [ ] Scene tagging and filtering (tags: `act-1`, `intro`, `combat`, `dialogue`, etc.)
- [ ] Scene metadata panel — notes, author, last-edited date

---

## Module 2 — Text and Dialogue Editor

- [x] Edit narration lines (free text)
- [ ] Edit dialogue lines (speaker + line)
- [ ] Move text: select starting row and column
- [x] Configure colour per line or block (ConsoleColor values)
- [ ] Bind text to a localisation key (`CK.*`) instead of hardcoding
- [ ] Side-by-side locale editor (e.g. en.md | es.md)
- [x] Live preview as you type
- [ ] Language preview button — switch the preview language and see the scene translated in real time
- [ ] **Locale lantern mode** — highlight in red any hardcoded string not going through `CL.Get(CK.*)`

---

## Module 3 — No-Code / Zero-Code — Asset Panel and Viewport

- [ ] Side asset panel with drag & drop onto the scene viewport
- [ ] Select and move any element in the viewport with the mouse (sprite, text, background, VFX)
- [ ] Visual position handles (gizmos: translate, like Unity's move tool)
- [ ] Configurable grid snap (e.g. snap to terminal column, every 2 chars)
- [ ] Properties inspector sidebar — when an element is selected, its parameters appear without touching code
- [ ] Asset catalog organised by type (sprites, backgrounds, ASCII art, effects, audio cues)
- [ ] Sprite thumbnails in the asset panel
- [ ] Drag a sprite onto the timeline to add it at a specific frame
- [ ] Drag an effect from the catalog onto the viewport to instance it
- [ ] Duplicate elements with Alt + drag
- [ ] Multi-selection with Ctrl + click or selection rectangle

---

## Module 4 — Visual Scripting Node Graph

- [ ] Node canvas where each node is an action: `ShowSprite`, `PlayAnimation`, `ShowText`, `Delay`, `Transition`, `TriggerVFX`, `PlaySound`
- [ ] Connect nodes with wires (execution sequence)
- [ ] Condition nodes: `if HP < 0`, `if language == es`, etc.
- [ ] Variable nodes: read/write scene properties
- [ ] Template library: "character entrance", "cut to black", "impact with blood", "typewriter text"
- [ ] Save a subgraph as a reusable template (like Blueprint macros in Unreal)
- [ ] Export graph to serialised JSON consumed by the engine at runtime
- [ ] Preview the graph sequence in the terminal simulator without leaving the editor
- [ ] Zoom and pan on the canvas
- [ ] Minimap for navigating large graphs
- [ ] Implement `IAnimationNode` interface to add custom node types via plugins

---

## Module 5 — 2D Pixel Art Sprites

- [ ] Import PNG and preview as ANSI half-block (`▀`) exactly as `PixelArtRenderer` renders it
- [ ] Project sprite catalog
- [ ] Position sprite with drag & drop or numeric col/row fields
- [ ] Render size adjustment (height in terminal rows)
- [ ] Split spritesheet into individual sprites (crop by region)
- [ ] Preview sprite over the active scene background
- [ ] **Spritesheet generator** — combine individual sprites into a single PNG ready for Unity

---

## Module 6 — Background Editor

- [ ] Create ASCII background (character grid editor)
- [ ] Import PNG and convert to ANSI half-block mosaic background
- [ ] Layer management: background / midground / foreground
- [ ] Terminal colour palette (16 colours + truecolor)
- [ ] Full-width backgrounds adapted to `Console.WindowWidth`
- [ ] Export backgrounds as C# arrays or standalone data files

---

## Module 7 — Animations and VFX

- [ ] Keyframe timeline per scene
- [ ] Frame-by-frame character movement
- [ ] Darkening / flash effects (like the darkness close in `AttackAnimation`)
- [ ] ASCII particles: blood, sparks, text explosions
- [ ] Screen shake (row shift per frame)
- [ ] Transitions: fade-in, fade-out, horizontal wipe
- [ ] Timing control: frame duration in ms, basic easing curves
- [ ] Copy and paste frames
- [ ] Loop preview for reviewing a single animation
- [ ] Audio cue markers on the timeline ("music starts here", "impact sound at frame 8")
- [ ] **Export to GIF or video** — record the animation in the terminal simulator and export as a GIF for trailers or documentation

---

## Module 8 — ASCII Character Art Editor

- [ ] Edit ASCII art arrays (the `CharacterArt.cs` arrays)
- [ ] Live colour preview with terminal colours
- [ ] Export directly to `string[]` C# format
- [ ] Import existing C# code to edit without rewriting from scratch

---

## Module 9 — Embedded CLI Terminal (AI Integration)

- [ ] Terminal panel embedded in the editor (open/close with Tab or tilde — like ARK / CS2)
- [ ] Runs a real terminal (WebView2 + xterm.js / ConPTY on Windows)
- [ ] Wrapper script `editor-ai.cmd` automatically injects context on launch:
  ```
  claude --context GAME_CONTEXT.md --context EDITOR_STATE.json
  ```
- [ ] `EDITOR_STATE.json` written by the editor in real time: active scene, assets in use, current validator errors
- [ ] `GAME_CONTEXT.md` maintained manually: GDD summary, conventions, data formats
- [ ] Support for any CLI AI: `claude`, `opencode`, `agy`, Gemini CLI, or any future tool
- [ ] Custom slash commands pre-defined (`.claude/commands/`): `/add-scene`, `/move-sprite`, `/translate-key`, `/create-vfx`, `/generate-animation`
- [ ] Validator error log automatically visible in the terminal — no copy-paste needed
- [ ] Persistent conversation history per editing session (not per scene — one continuous session)
- [ ] Three-layer context system:
  - **Layer 1** — `GAME_CONTEXT.md`: permanent project context, loaded every session
  - **Layer 2** — `SESSION.md`: session log, resets on editor close, persists across scene switches
  - **Layer 3** — `DECISIONS.md`: important decisions archived via `/remember` command
- [ ] "Apply suggestion" path: AI writes a JSON patch file, editor detects it and offers one-click apply

---

## Module 10 — Narrative Flow Graph

> Different from the animation node graph. This is the macro story view.

- [ ] High-level canvas where each node is a **scene** (not an animation action)
- [ ] Arrows between scenes represent transitions
- [ ] Labels on arrows: conditions, choices, or "always"
- [ ] Acts / chapters shown as visual groupings
- [ ] Click a scene node to open it in the scene editor
- [ ] Orphan scenes highlighted (no incoming connections)
- [ ] Dead-end scenes highlighted (no outgoing connections unless intentional)
- [ ] Import from existing `SceneSequencer` definitions to bootstrap the graph

---

## Module 11 — Real Terminal Test

- [x] One-click **"Test in real terminal"** button (▶ Play — launches `ConsoleEngine.SceneRunner.exe` in Windows Terminal or cmd.exe)
- [ ] Windows Terminal, cmd, and PowerShell terminal profiles supported (wt.exe auto-detected; cmd.exe fallback)
- [ ] After the test window closes, the editor shows a diff of any data files that changed

---

## Module 12 — Asset Dependency Map

- [ ] Bidirectional graph: from an asset, see every scene that uses it
- [ ] From a scene, see every asset it depends on
- [ ] Rename / delete safety check: "this sprite is used in 4 scenes — rename all references?"
- [ ] Powers the validator and the health dashboard
- [ ] Visualised as a tree or graph view in the editor

---

## Module 13 — Localisation Workflow

- [ ] **Auto-draft translations** — select untranslated keys, call AI from the terminal, fill the draft column
- [ ] Status per key: `draft` / `reviewed` / `approved`
- [ ] Filter locale editor by status: "show only approved in English but unreviewed in Spanish"
- [ ] Side-by-side view: canonical language (en) on the left, target language on the right
- [ ] Bulk approve / bulk mark as draft
- [ ] Export untranslated keys as a plain text list for a human translator

---

## Module 14 — Project Health Dashboard

Landing screen shown when the editor opens. Shows at a glance:

- [ ] Scenes with missing or empty locale keys
- [ ] Sprites referenced in scenes but not found on disk
- [ ] Animation nodes with broken connections
- [ ] Scenes not previewed in more than N days
- [ ] Orphan assets (on disk but not referenced by any scene)
- [ ] Locale keys with status `draft` (not yet reviewed)
- [ ] Scene notes marked as TODO
- [ ] Click any issue to jump directly to the affected scene or asset

---

## Module 15 — Animation Debugger

- [ ] **Frame breakpoints** — right-click any frame in the timeline to set a breakpoint
- [ ] When play mode hits a breakpoint, execution pauses
- [ ] **Step-frame** — advance one frame at a time with a key (like `F10` in a code debugger)
- [ ] State inspector panel when paused: position of each element, colour, visibility, timing accumulated
- [ ] Resume from breakpoint

---

## Module 16 — Plugin System

- [ ] `Plugins/` folder — drop a DLL that implements engine interfaces to extend the editor
- [ ] `IEditorPlugin` — entry point: registers node types, export formats, validators, panels
- [ ] Custom node types for the visual scripting graph
- [ ] Custom export formats (e.g. export scenes to a Unity ScriptableObject format)
- [ ] Custom validators (e.g. "warn if any dialogue line is longer than 60 chars")
- [ ] Plugin manager panel: list installed plugins, enable/disable, show version
- [ ] Documentation for building plugins, with a starter template

---

## Module 17 — Modder Mode

- [ ] **Export for modding** — produces a zip with only data files (JSON, Markdown, sprites); no source code
- [ ] Auto-generated format documentation from the engine schema
- [ ] Mod validator: check that a community mod is valid before loading it into the game
- [ ] Mod loader: the game runtime can load mods from a `Mods/` folder at startup
- [ ] Modder getting-started guide auto-generated from `GAME_CONTEXT.md`

---

## Module 18 — Multilingual Layout Stress Test

- [ ] Show all 11 supported translations of a scene simultaneously as miniature previews
- [ ] Highlight in red any string that overflows its allocated area in any language
- [ ] Especially useful for UI labels where German or Russian strings can be 2× the length of English
- [ ] One-click jump to the offending locale key

---

## Module 19 — Scene Notes and Comments

- [ ] Sticky notes per scene (not visible in the game — production notes only)
- [ ] Note types: `TODO`, `DECISION`, `QUESTION`, `AUDIO`
- [ ] All open TODOs visible in the Health Dashboard
- [ ] Notes survive scene duplication (copied as `TODO: review in new scene`)
- [ ] Searchable across all scenes

---

## Module 20 — Editor Utilities

- [ ] Terminal simulator with window size presets (80×24, 120×30, 160×40…)
- [ ] Global undo / redo with a visual history panel (scrub through session changes like Photoshop history)
- [ ] Editor colour theme: light / dark
- [ ] All operations accessible via keyboard (keyboard-first workflow)
- [ ] Configurable keyboard shortcuts
- [ ] Autosave with configurable interval
- [ ] Multi-monitor support: detach panels to separate monitors
- [ ] **Import from code** — parse `IntroScene.cs` and `AttackAnimation.cs` and convert them to editor data files to bootstrap the project without starting from scratch
- [ ] **Accessibility preview** — switch the terminal simulator to 16-colour mode to test how the scene looks without truecolor

---

## Delivery Phases

### Phase A — Foundation (with engine extraction) — IN PROGRESS

- [x] Scene management (create, list, duplicate, delete) — reorder drag&drop pending
- [x] Text editor with live preview
- [x] Play / Stop / Reload modes (`ConsoleEngine.SceneRunner.exe` launched in real terminal)
- [ ] Terminal simulator panel (80×24, embedded in editor — currently text-only preview)
- [ ] Sprite import and preview
- [ ] Embedded terminal panel (AI CLI, WebView2 + xterm.js)

### Phase B — Visual authoring

- [ ] Drag & drop asset panel
- [ ] Background editor
- [ ] Keyframe animation timeline
- [ ] No-code properties inspector
- [ ] Node graph (basic: ShowSprite, ShowText, Delay, Transition)
- [ ] Narrative flow graph

### Phase C — Quality and workflow

- [ ] Health dashboard
- [ ] Asset dependency map
- [ ] Localisation workflow (auto-draft + status)
- [ ] Animation debugger (breakpoints + step-frame)
- [ ] Real terminal test button
- [ ] Multilingual layout stress test

### Phase D — Power features

- [ ] Plugin system
- [ ] Modder mode
- [ ] Scene notes and comments
- [ ] Undo history scrubber
- [ ] GIF / video export
- [ ] Full node template library

---

**Created**: 2026-05-26
**Author**: AkashicEnd Development Team
**Status**: Phase A — in progress (scene mgmt, text editor, play/stop/reload done)
