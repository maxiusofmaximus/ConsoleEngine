# ConsoleEngine — Visión 2D Game Engine

> Este documento define a qué se convierte ConsoleEngine cuando escala más allá del renderer de terminal.
> Es la guía de dirección para decisiones de arquitectura, roadmap y features.

---

## 1. Qué es hoy vs. qué será

### Hoy (v0.6.0) — Framework de juegos de terminal

ConsoleEngine escribe caracteres en `System.Console`. La "ventana" que ve el usuario es
el terminal emulator del sistema operativo (Windows Terminal, cmd.exe). El engine no
controla el canvas — el OS lo hace.

```
Engine → System.Console → OS Terminal Emulator → pantalla
```

**Capacidades actuales:**
- Texto con `ConsoleColor` (16 colores paleta ANSI)
- Sprites PNG vía `PixelArtRenderer` (ANSI truecolor 24-bit, carácter `▀` = 2 px)
- ASCII art animado
- Input bloqueante (`ReadKey`, `ReadLine`)
- Sin game loop real (sin delta time, sin Update a 60 fps)
- Resolución = filas/columnas del terminal (típicamente 80-220 × 24-60)

---

### Target (v1.0.0+) — Motor gráfico 2D con editor visual

ConsoleEngine abre su propia **ventana nativa del OS** y dibuja píxeles directamente
a ella con aceleración de GPU. El terminal sigue siendo un modo soportado pero el
engine ya no depende de él.

```
Engine → Renderer Backend (OpenGL/Vulkan/SkiaSharp) → ventana nativa → GPU → pantalla
Engine → ConsoleRenderer (legacy) → System.Console → terminal
```

**Lo que habilita:**
- Cualquier resolución (720p, 1080p, 4K, ultrawide)
- Game loop real: `Update(double delta)` a 60/120/144 fps
- Sprites hardware-acelerados con cualquier dimensión
- Tilemaps, layers, parallax
- Física 2D (Box2D bindings)
- Input en tiempo real: teclado + mouse (posición exacta en píxeles) + gamepad
- Shaders GLSL propios por sprite/capa
- Partículas y VFX hardware-acelerados
- Audio 3D posicional
- Viewports múltiples (split-screen, picture-in-picture)

---

## 2. Qué es un renderer con ventana nativa

### El problema con `System.Console`

`Console.Write()` envía caracteres a un **proceso de terminal externo** (el host del OS).
El engine no tiene acceso a los píxeles — solo puede enviar texto y secuencias ANSI.
La ventana, el tamaño de fuente, el framerate, el input, todo está controlado por el
terminal, no por el engine.

### Ventana nativa

El engine llama directamente a la API del OS para crear una ventana:

```csharp
// Con Silk.NET + GLFW (multiplataforma):
var window = Window.Create(WindowOptions.Default with
{
    Title  = "Mi Juego",
    Size   = new Vector2D<int>(1280, 720),
    API    = GraphicsAPI.Default  // OpenGL 4.6
});
```

Desde ese momento:
- Cada píxel de esa ventana lo controla el engine
- El framerate lo determina el game loop, no el terminal
- El input llega en tiempo real (no hay `ReadKey()` bloqueante)
- Se puede usar OpenGL/Vulkan para dibujar con la GPU

### Stack recomendado para ConsoleEngine

| Capa | Librería | Función |
|---|---|---|
| Window + Input | **Silk.NET.Windowing** + GLFW | Crear ventana, recibir input, swap buffers |
| Rendering 2D | **SkiaSharp** sobre OpenGL | Sprites, tilemaps, texto, shaders 2D |
| Audio | **OpenAL via Silk.NET** | Sonido posicional, música, efectos |
| Física | **Box2D.NetStandard** | Colisiones, rigidbodies, triggers |
| Terminal (legacy) | `System.Console` + ANSI | Mantener compatibilidad con modo ASCII |

**Por qué Silk.NET + SkiaSharp:**
- Silk.NET es el binding .NET más activo para OpenGL/Vulkan/OpenAL/GLFW — mantenido por la comunidad .NET
- SkiaSharp es el mismo engine gráfico 2D que usan Chrome, Flutter, y Android internamente
- Ambos son multiplataforma (Windows, Linux, macOS)
- Sin agregar runtimes externos — pure .NET

---

## 3. Comparativa: ConsoleEngine ahora vs. engines del mercado

| Feature | CE hoy | CE target | Godot 4 | Unity | GameMaker |
|---|---|---|---|---|---|
| Renderer | Terminal ANSI | OpenGL/Vulkan | Vulkan/OpenGL | D3D/Metal/Vulkan | D3D/OpenGL |
| Resolución | Chars (80-220×24-60) | Cualquier px | Cualquier px | Cualquier px | Cualquier px |
| Sprites | PNG→▀ blocks | GPU sprites | GPU sprites | GPU sprites | GPU sprites |
| Tilemaps | No | Sí | TileMap node | Tilemap | Room editor |
| Física 2D | No | Box2D | GodotPhysics2D | Box2D | YYPhysics |
| Input real-time | No (bloqueante) | Sí (60fps poll) | Sí | Sí | Sí |
| Gamepad | No | Sí | Sí | Sí | Sí |
| Shaders | ANSI escape | GLSL | GDShader | HLSL/GLSL | GLSL |
| Audio | No | OpenAL | OpenAL | FMOD/WWise | FM FMOD |
| Visual Scripting | Planeado | Sí (nodo graph) | VisualScript | Shader Graph | Drag-and-drop |
| Editor integrado | Avalonia (parcial) | Avalonia (completo) | Godot editor | Unity editor | GMS editor |
| AI terminal | Planeado (Tab) | Sí | No | Copilot (externo) | No |
| Lenguaje de scripting | C# | C# | GDScript/C# | C# | GML/JS |
| Modo terminal legacy | Sí | Sí | No | No | No |

---

## 4. Visual Scripting — Node Graph

El sistema de Visual Scripting de ConsoleEngine (Module 4 del EDITOR_PLAN) permite
programar comportamiento de juego sin escribir C#, usando nodos conectados por cables
en el editor visual.

### Tipos de nodo

```
┌─────────────┐    ┌──────────────────┐    ┌────────────────┐
│  ON START   │───▶│  SHOW SPRITE     │───▶│  WAIT 500ms    │
│  (evento)   │    │  hero.png        │    │                │
└─────────────┘    └──────────────────┘    └───────┬────────┘
                                                   │
                                           ┌───────▼────────┐
                                           │  PLAY SOUND    │
                                           │  impact.wav    │
                                           └───────┬────────┘
                                                   │
                                  ┌────────────────▼──────────────┐
                                  │  IF HP < 0                    │
                                  │  condición                    │
                                  └───────┬───────────────┬───────┘
                               TRUE ──────┘               └────── FALSE
                                   │                              │
                          ┌────────▼──────┐              ┌───────▼───────┐
                          │  SHOW SCENE   │              │  CONTINUE     │
                          │  game_over    │              │               │
                          └───────────────┘              └───────────────┘
```

### Nodos disponibles (target)

**Eventos:** `OnStart`, `OnInput`, `OnCollision`, `OnTimer`, `OnSceneLoad`
**Acción:** `ShowSprite`, `HideSprite`, `MoveSprite`, `PlayAnimation`, `PlaySound`, `ShowScene`, `ShowDialogue`
**Control:** `Delay`, `If`, `While`, `ForEach`, `Sequence`, `Parallel`
**Datos:** `GetFlag`, `SetFlag`, `GetPlayerStat`, `SetPlayerStat`, `RandomInt`
**VFX:** `ScreenShake`, `Flash`, `Particle`, `Transition`

### Implementación en el editor

```
Nodo visual → serializado como JSON → cargado por VisualScriptPlayer en runtime
```

El nodo graph no requiere compilación — el engine ejecuta el JSON directamente.
Implementa `IScenePlayer` para ser intercambiable con `ScenePlayer` y `SceneSequencer`.

Ver **EDITOR_PLAN.md § Module 4** para el plan de UI detallado.

---

## 5. AI Terminal — Tab para abrir Claude / opencode / agy

El editor tiene un **terminal embebido** que se abre/cierra con `Tab` (como en ARK
Survival Evolved, CS2, o Quake), desde el cual el desarrollador puede ejecutar cualquier
herramienta de IA CLI con el contexto del juego inyectado automáticamente.

### Cómo funciona

```
Tab ───▶ Terminal panel se abre (WebView2 + xterm.js / ConPTY)
         │
         ├── editor-ai.cmd se ejecuta automáticamente:
         │     claude --context GAME_CONTEXT.md --context EDITOR_STATE.json
         │
         ├── GAME_CONTEXT.md (permanente, escrito por el dev):
         │     GDD summary, convenciones, formatos de datos, mecánicas
         │
         ├── EDITOR_STATE.json (en tiempo real, escrito por el editor):
         │     escena activa, assets en uso, errores del validator, versión
         │
         └── SESSION.md (por sesión, escrito por la IA/dev):
               decisiones de esta sesión, contexto acumulado
```

### Comandos de IA disponibles

```bash
# Cualquiera de estos funciona en el terminal:
claude          # Claude Code (Anthropic)
opencode        # OpenCode (OSS)
agy             # Agy AI
gemini          # Gemini CLI
copilot         # GitHub Copilot CLI
```

### Slash commands predefinidos para el motor

```
/add-scene <nombre>          — crea una nueva escena con el template correcto
/move-sprite <sprite> <x> <y>— mueve un sprite en la escena activa
/translate-key <key>         — draft de traducción para todas las locales
/create-vfx <tipo>           — genera un VFX en el node graph
/generate-animation <desc>   — genera un AnimationTimeline desde descripción
/scene-to-node-graph         — convierte una escena a nodo graph visual
/fix-validator               — aplica los arreglos sugeridos por el validator
```

### Por qué Tab como tecla

La convención de juegos (Quake, CS2, ARK, Unreal Editor) es `~` (tilde) o `Tab`.
`Tab` se eligió porque:
- No interfiere con atajos del sistema (Alt+Tab alterna ventanas, pero Tab solo no)
- Es el mismo key que usan herramientas de IDE para "siguiente sugerencia"
- No requiere shift (a diferencia de `~` en teclados no-US)

Ver **EDITOR_PLAN.md § Module 9** para el plan de implementación detallado.

---

## 6. Scene Graph — El cambio arquitectónico central

Para ser un motor 2D real, ConsoleEngine necesita un **scene graph**: una jerarquía
de nodos donde cada nodo puede tener hijos, hereda transformaciones del padre, y el
engine los actualiza/renderiza en orden.

Godot lo llama "Node tree". Unity lo llama "Hierarchy". ConsoleEngine lo llamará
`SceneGraph` o `NodeTree` (por definir).

```
SceneRoot
  ├── Camera2D
  ├── Background (TilemapLayer)
  ├── Player (Entity)
  │     ├── Sprite2D  (hero.png)
  │     ├── Collider2D (BoxCollider)
  │     └── AnimationPlayer
  ├── Enemies (EntityGroup)
  │     ├── Enemy_01 (Entity)
  │     └── Enemy_02 (Entity)
  └── UI (CanvasLayer)
        ├── HPBar
        └── DialogueBox
```

### API tentativa (C#)

```csharp
// Definir un nodo de juego:
public class Player : Entity2D
{
    public override void OnStart()
    {
        AddComponent<Sprite2D>(new Sprite2D("hero.png"));
        AddComponent<BoxCollider2D>(new BoxCollider2D(32, 48));
    }

    public override void OnUpdate(double delta)
    {
        if (Input.IsPressed(Key.Right)) Position.X += 200 * delta;
        if (Input.IsPressed(Key.Left))  Position.X -= 200 * delta;
    }

    public override void OnCollision(Entity2D other)
    {
        if (other.HasTag("enemy")) TakeDamage(10);
    }
}
```

---

## 7. Roadmap expandido hacia motor 2D

> Este roadmap extiende el que está en ENGINE_PLAN.md. Los primeros hitos
> (v0.6.0 – v0.8.0) no cambian — son prerequisitos del renderer.

### v0.9.0 — Renderer nativo + game loop

- [ ] `ConsoleEngine.Rendering.Native` — nueva librería con Silk.NET + SkiaSharp
- [ ] `NativeWindow` — crea ventana OS con OpenGL context
- [ ] `GameLoop` — Update/Render a framerate configurable (60/120/144 fps)
- [ ] `IRenderer` — abstracción: `ConsoleRenderer` y `NativeRenderer` intercambiables
- [ ] `Sprite2D` — sprite GPU con position, scale, rotation, tint
- [ ] `Camera2D` — viewport que sigue al jugador, zoom
- [ ] `InputManager` — teclado + mouse en tiempo real, sin bloqueo
- [ ] DinoGame portado a `NativeRenderer` como proof of concept

### v0.10.0 — Tilemap + Physics

- [ ] `TilemapLayer` — grid de tiles con tile atlas PNG
- [ ] `TilemapCollider` — colisiones contra tiles sólidos
- [ ] `PhysicsWorld` — Box2D integrado: gravity, rigidbody, triggers
- [ ] `AnimationStateMachine` — idle/walk/run/attack con transiciones

### v0.11.0 — Audio + Visual Scripting runtime

- [ ] `AudioEngine` — reproducir WAV/OGG, volumen, pitch, fade, loop
- [ ] `SpatialAudio` — volumen basado en distancia al listener
- [ ] `VisualScriptPlayer` — ejecuta grafos JSON del node graph del editor
- [ ] `IAnimationNode` interface — plugins de nodo custom

### v0.12.0 — Editor integrado al native renderer

- [ ] Viewport del editor = ventana del juego embebida en Avalonia (como Godot)
- [ ] Gizmos en el viewport: mover, rotar, escalar sprites con mouse
- [ ] Play mode dentro del editor sin abrir terminal externa
- [ ] AI terminal (Tab) — Module 9 del EDITOR_PLAN implementado

### v1.0.0 — API stable release

- [ ] API freeze de todos los módulos públicos
- [ ] Documentación completa de cada módulo
- [ ] `dotnet new consoleengine` — template instalable
- [ ] ConsoleRenderer y NativeRenderer con paridad de features
- [ ] AkashicEnd en producción sobre ConsoleEngine v1.0.0

---

## 8. Lo que ConsoleEngine NO será

Para mantener el scope manejable, ConsoleEngine no intentará competir con:

| Feature | Motivo para no incluir |
|---|---|
| 3D rendering | Scope diferente — usar Godot/Unity/Stride para 3D |
| Editor en C++ | El editor en C# + Avalonia es una ventaja competitiva, no una desventaja |
| Shader visual editor | Muy complejo para el scope actual; GLSL escrito a mano primero |
| Multiplayer networking | Capa de aplicación, no de engine — usar SignalR/ENet externamente |
| Consola/mobile port | Windows-first; otros plataformas en v2.x si aplica |

---

## 9. Por qué tiene sentido que ConsoleEngine sea un motor 2D

**C# es suficientemente rápido.** Unity, Stride y MonoGame son motores 2D/3D escritos
en C# con rendimiento de producción real. El lenguaje no es la limitación.

**Avalonia ya está.** El editor ya tiene una ventana nativa GPU-acelerada. El siguiente
paso es exponer eso como el viewport del juego.

**El terminal es un superpoder, no una prisión.** Ningún otro motor 2D tiene modo
terminal ASCII. ConsoleEngine puede ser el único motor que corra igual en una
terminal y en una ventana nativa — útil para juegos roguelike, narrativa, y SSH gaming.

**La IA terminal es única.** Ningún motor del mercado tiene un terminal de IA integrado
con contexto del proyecto inyectado automáticamente. Ese Tab → Claude/opencode/agy
es una feature que diferencia ConsoleEngine de todos los demás.

---

*Última actualización: 2026-05-29*
*Versión engine: 0.6.0 → target 1.0.0*
