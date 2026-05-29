# ConsoleEngine — Investigación Arquitectónica

> Análisis comparativo de game engines modernos aplicado al diseño de ConsoleEngine.
> Cada sección concluye con implicaciones concretas para este proyecto.

**Motores investigados**: Unity 6, Unreal Engine 5.x, GameMaker Studio 2, Flax Engine, Godot 4, Stride (Xenko), MonoGame, FNA  
**Fuentes**: Documentación oficial, changelogs, benchmarks, postmortems de producción  
**Fecha**: 2026-05-28

---

## 1. Propósito

Este documento no es un catálogo de características de motores gráficos. Es un mapa de decisiones técnicas extraídas de la experiencia acumulada de esos motores, aplicadas directamente a ConsoleEngine.

El objetivo es triple:
1. Confirmar decisiones ya tomadas que son correctas
2. Identificar deuda técnica activa antes de que escale
3. Guiar decisiones futuras con evidencia real en vez de intuición

---

## 2. Análisis por motor

### 2.1 Unity 6 — ECS/DOTS, Asset Pipeline, Lessons

**Fuentes**: [docs.unity3d.com/Packages/com.unity.entities@1.0], [docs.unity3d.com/6000.2/Manual/job-system-overview], [docs.unity3d.com/Packages/com.unity.addressables@1.20]

#### Lo que hace bien

**Archetype + Structure of Arrays (SoA)**

Unity ECS agrupa entidades con exactamente los mismos componentes en "archetypes". Cada archetype almacena sus datos en chunks de 16 KB de memoria contigua — un tamaño elegido para ajustarse a la caché L1 del CPU. Dentro del chunk, los componentes se almacenan como arrays separados (SoA), no como structs entrelazados (AoS):

```
Chunk 16KB [Position, Velocity, Health]:
  Array[Position]  = [p0, p1, p2, ...]
  Array[Velocity]  = [v0, v1, v2, ...]
  Array[Health]    = [h0, h1, h2, ...]
```

Cuando un sistema itera sobre `Position` y `Velocity`, lee dos arrays contiguos. El CPU prefetcher carga el siguiente bloque antes de necesitarlo. En AoS tradicional, los campos de diferentes tipos están intercalados en memoria → cache miss en cada acceso.

**Job System con safety system**

El pool de workers tiene exactamente N threads (N = núcleos lógicos del CPU). El Safety System detecta race conditions en tiempo de compilación mediante tracking de lectura/escritura en `NativeContainer`. Si dos jobs intentan escribir el mismo `NativeArray`: excepción con mensaje claro, no corrupción silenciosa.

**Asset Pipeline v2: Importer → Artifact → Cache**

Los assets procesados se almacenan como "artifacts" por plataforma. En builds sucesivos, si el source asset no cambió, el artifact se reutiliza directamente. El Unity Accelerator permite compartir artifacts entre máquinas del equipo.

**Addressables: dirección desacoplada de ruta física**

```
"player_sprite" → Catalog → Location (local o remota) → Asset + dependencies
```

Reference counting automático: el asset se descarga cuando ningún sistema lo referencia. Resuelve el problema de `Resources.Load("path/hardcodeado")`.

**ScriptableObject como data-driven design**

Datos de configuración como assets serializables en disco, editables sin recompilar. Separación explícita entre definición (ScriptableObject) y estado mutable (MonoBehaviour). Este patrón es exactamente lo que ConsoleEngine implementa con `.scene.json` y `.world.json`.

#### Lo que falló (lecciones de producción)

**MonoBehaviour event overhead**

Cada `Update()` cruza el bridge managed C# ↔ native C++ aunque el método esté vacío. Con 10.000 MonoBehaviours, el overhead acumulado de dispatch es medible en ms de frame time. Unity introdujo DOTS precisamente para eliminar este overhead con procesamiento batch.

> **Para ConsoleEngine**: No introducir callbacks por-frame como `IUpdatable.Update()` en interfaces del engine. Si se necesita actualización por frame en el futuro, usar el patrón Job/batch, no callbacks individuales por entidad.

**UGUI Canvas full rebuild**

Cualquier cambio en un elemento del Canvas (texto, visibilidad, posición) desencadena un rebuild completo del Canvas: recalcula layout, regenera batches, actualiza VBO. Si un juego tiene 50 elementos de UI cambiando por frame, el CPU hace 50 rebuilds completos donde solo debería hacer 1 o 0.

> **Para ConsoleEngine**: El preview del editor (`RebuildPreview()` en `MainViewModel`) actualmente reconstruye el buffer completo en cada keystroke. Añadir dirty flag: marcar dirty en `OnPropertyChanged`, rebuildar una sola vez por frame de UI.

**Binary serialization fragility**

Agregar un campo en v1.1 rompe la deserialización de datos guardados en v1.0. Unity aprendió esto con su formato binario de prefabs.

> **Para ConsoleEngine**: Ya usamos JSON ✅ pero falta campo `"schemaVersion"` en `.scene.json` y `.world.json`. Sin él, cuando añadamos campos en 0.7.0, los archivos creados en 0.5.0/0.6.0 no tendrán esos campos — el loader debe manejar ese caso con defaults explícitos.

**AssetDatabase v1 performance**

`AssetDatabase.FindAssets()` era O(n) sin caché; escanear todo el proyecto en cada llamada. `ForceReserializeAssets` podía bloquear el editor 1+ hora en proyectos grandes.

> **Para ConsoleEngine**: El editor hace `Directory.GetFiles("*.scene.json", SearchOption.AllDirectories)` en `LoadProject()` y `ReloadProject()`. Para proyectos con miles de archivos, este escaneo debe estar cacheado. Implementar un índice de archivos en `MainViewModel` que se invalida solo cuando hay cambios en disco (FileSystemWatcher).

**DOTS: curva de aprendizaje sin fallback**

Unity no ofreció un camino de migración gradual desde MonoBehaviour. Muchos developers escriben código DOTS que no aprovecha los beneficios de caché (mezclan SoA con referencias managed, crean archetypes fragmentados).

> **Para ConsoleEngine**: No adoptar ECS como modelo principal. Para juegos narrativos, el modelo node-tree de Godot es más apropiado. Si se añade un sistema de entidades (NPCs, items), hacerlo opcional y sobre una interfaz estable.

---

### 2.2 Unreal Engine 5.x — Mass Entity, World Partition, Módulos

**Fuentes**: [dev.epicgames.com/documentation/en-us/unreal-engine/mass-entity-in-unreal-engine], [dev.epicgames.com/documentation/en-us/unreal-engine/world-partition-in-unreal-engine], [dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-modules]

#### Lo que hace bien

**Módulos con separación Public/Private**

Cada módulo UE5 tiene carpeta `Public/` (headers que dependientes pueden incluir) y `Private/` (implementación oculta). Cambiar implementación privada no recompila módulos dependientes. La dependencia cruzada entre módulos es explícita en `.Build.cs`.

> **Para ConsoleEngine**: Los proyectos `.csproj` ya proporcionan separación equivalente ✅. La regla complementaria: las interfaces en `ConsoleEngine.Core` son la única dependencia cruzada permitida. Ningún módulo debe referenciar implementaciones concretas de otro módulo directamente.

**Modular Game Features**

El core del juego no sabe que los features existen. Los features se registran en el sistema de plugins y pueden cargarse/descargarse sin modificar código central. El game core expone puntos de extensión abstractos.

> **Para ConsoleEngine**: `IEditorPlugin` (Module 16) debe seguir exactamente este patrón. `ConsoleEngine.Editor` no debe referenciar ningún plugin concreto. Los plugins se descubren via reflection sobre un directorio `Plugins/` en startup.

**World Partition: streaming por grid**

El mundo se divide en celdas de 12.800 unidades. Cada celda tiene configurada su distancia de streaming (landmarks grandes: 2km, props pequeños: 200m). El sistema carga automáticamente las celdas dentro del radio del player. "One File Per Actor" (OFPA) permite que múltiples developers trabajen en la misma región sin conflictos de merge.

> **Para ConsoleEngine**: `WorldMap` actual carga todas las `LocationDefinition` en memoria al inicio. Para mundos pequeños (<30 locations) esto es correcto. Para mundos grandes (100+ locations), implementar carga por regiones en 0.9.0: definir "región" como un archivo `.world.json` parcial; `WorldMap` carga solo las regiones adyacentes a la posición actual.

**GAS: abilities que esperan eventos**

El Gameplay Ability System usa `WaitGameplayEvent()` — una ability puede pausar su ejecución hasta que ocurra un evento externo (animación termina, input recibido, condición cumplida). Esto permite acciones complejas sin polling continuo.

> **Para ConsoleEngine**: `IExplorationAction.Execute()` actualmente retorna `ExplorationOutcome` de forma síncrona. En 0.9.0, evaluar retornar `Task<ExplorationOutcome>` para acciones que requieren input adicional del jugador (ej: "usar objeto" → mostrar submenu → esperar selección).

#### Lo que falló

**Blueprint VM overhead**

Blueprint ejecuta bytecode en una VM. Benchmark real: mover 500 actores vía Blueprint tick ≈ 1ms/frame; mismo código en C++ nativo ≈ 0.0-5ms total. Para gameplay crítico en performance, Blueprint es inaceptable.

> **Para ConsoleEngine**: No usar `Activator.CreateInstance()`, `MethodInfo.Invoke()`, o reflection en el game loop. El dispatch de `IExplorationAction` y `ScenePlayer` debe ser estático y compilado.

**UnrealBuildTool build times**

En proyectos grandes, UnrealHeaderTool parsea todos los headers buscando macros `UPROPERTY/UFUNCTION`. Cada include innecesario aumenta el tiempo de parseo. Builds full pueden tardar 10-30 minutos.

> **Para ConsoleEngine**: `Directory.Build.props` centralizado previene este problema ✅. Los proyectos `.csproj` solo declaran lo que es único. No añadir referencias globales que todos los proyectos hereden innecesariamente.

**Sistema de macros (UPROPERTY, UFUNCTION)**

No es C++ estándar. UHT lo parsea con reglas propias. Confunde a IDEs y herramientas que no conocen UE5. Barrera de entrada alta para nuevos contributors.

> **Para ConsoleEngine**: Mantener C# puro. No crear atributos custom que requieran un paso de generación de código separado. Source generators de .NET son aceptables si agregan valor real (ej: serialización sin reflection).

---

### 2.3 GameMaker Studio 2 — Room System, Event Loop, Scaling Failures

**Fuentes**: [manual.gamemaker.io/lts/en], [generalistprogrammer.com/tutorials/gamemaker-studio-2-complete-development-guide-2025]

#### Lo que hace bien

**Room/Layer system**

Rooms organizan el contenido en capas independientes: tiles, instances, backgrounds, UI — cada capa con su propio depth y transform. Los diseñadores manipulan capas sin tocar código.

> **Para ConsoleEngine**: El editor debería mostrar texto, sprite y ASCII art como layers visuales independientes, no como campos de texto mezclados en el panel derecho. En Phase B, el viewport debe tener capa de background, capa de ASCII art, y capa de texto superpuestos con z-order configurable.

**API accesible**

Prototype funcional en horas. GML tiene funciones de nombre descriptivo (`draw_sprite_ext`, `collision_rectangle`, `instance_create_layer`). La curva de aprendizaje es la más baja de todos los motores analizados.

> **Para ConsoleEngine**: `CL.Get()`, `AnimationEngine.DrawAt()`, `ScenePlayer.Play()` mantienen este principio ✅. Cada API pública debe ser discoverable sin leer el código fuente.

#### Lo que falló

**GML sin OOP real**

No hay clases, herencia, o encapsulamiento. El estado global se acumula en variables de instancia y scripts globales. En proyectos medianos-grandes, el rastreo de dependencias se vuelve imposible.

> **Para ConsoleEngine**: `FlagStore` (planeado en 0.7.0) NO debe ser un diccionario de strings globales. Debe ser un objeto tipado, serializable, con acceso controlado. Compáralo con Unity ScriptableObject: datos separados del comportamiento, con identidad clara.

**Sin testing infrastructure**

Es prácticamente imposible unit-testear lógica GML. Los bugs solo se detectan durante el juego, no durante desarrollo.

> **Para ConsoleEngine**: `ConsoleEngine.Tests` en 0.8.0 es crítico. Esta es la diferencia más importante entre un engine amateur y uno profesional. Todos los engines maduros (Unity Test Framework, Unreal Automation Tests) tienen suites de tests robustas.

**Sin modularización**

Todo el código del juego vive en un namespace plano. Añadir features crea conflictos de nombres; eliminarlos requiere búsqueda manual.

> **Para ConsoleEngine**: La estructura `ConsoleEngine.*` por proyecto previene este anti-patrón ✅. Cada módulo es un assembly separado con namespace propio.

---

### 2.4 Flax Engine — Hot Reload, Prefabs, Plugin System

**Fuentes**: [docs.flaxengine.com/manual/scripting/cpp/index.html], [flaxengine.com/blog/flax-facts-16-scripts-hot-reload/], [docs.flaxengine.com/manual/get-started/prefabs/index.html]

#### Lo que hace bien

**Hot reload en ~154ms**

Flax usa un fork customizado de Mono que permite unloading selectivo de assemblies de usuario sin recargar los assemblies del editor. El editor serializa el estado de la escena activa, descarga los assemblies modificados, los recarga, y restaura el estado en ~154ms.

Unity requería un "domain reload" completo (todos los assemblies, incluyendo el editor) — esto podía tomar 10-30 segundos en proyectos grandes.

> **Para ConsoleEngine**: `ReloadProject()` en el editor ya reescanea sin cerrar ✅. Si en el futuro se implementa scripting dinámico para juegos (donde el juego puede cargar scripts custom), usar `AssemblyLoadContext` de .NET 8 para unloading parcial. No usar el AppDomain API legado.

**Prefab property overrides**

Las instancias de prefab heredan todos los valores del template pero pueden sobreescribir campos específicos. El editor muestra en color qué campos están sobreescritos. "Revert" restaura el valor del template.

> **Para ConsoleEngine**: `sealed record SceneDefinition` con `with` expressions ya implementa esto ✅:
> ```csharp
> var variant = baseScene with { artColor = ConsoleColor.Red, lines = newLines };
> ```
> `SceneSequencer` debe formalizar este patrón: cada nodo de la secuencia puede especificar overrides sobre una escena base.

**Separación game plugins ↔ editor plugins**

Los plugins que van con el runtime del juego son distintos de los que extienden el editor. Un plugin de juego no puede acceder a APIs del editor en runtime.

> **Para ConsoleEngine**: Cuando se implemente Module 16 (Plugin System), establecer esta separación desde el diseño:
> - `IGamePlugin` — plugins que se cargan en el runtime del juego
> - `IEditorPlugin` — plugins que extienden el editor Avalonia
> - Los assemblies de editor no se incluyen en la build del juego

#### Lo que falló

**Dependencia de Mono custom**

El hot reload de Flax depende de un fork propio de Mono, no del runtime estándar de .NET. Esto crea una divergencia con el ecosistema oficial de Microsoft. Migrar a .NET modern requiere reescribir el sistema de hot reload.

> **Para ConsoleEngine**: Usar solo APIs estándar de .NET 8. No depender de comportamientos no documentados del runtime. `AssemblyLoadContext` es la API oficial para loading/unloading dinámico.

---

### 2.5 Godot Engine — Node Tree, Signals, Editor = Engine

**Fuentes**: [docs.godotengine.org/en/stable/getting_started/step_by_step/nodes_and_scenes.html], [docs.godotengine.org/en/stable/engine_details/architecture/index.html]

#### Lo que hace bien

**Node-tree: "todo es una escena"**

Cada elemento del juego es un nodo. Los nodos se organizan en árbol jerárquico. Una escena es una sub-árbol de nodos guardada como archivo `.tscn`. Cualquier escena puede instanciarse dentro de otra escena (composición).

La misma semántica para objetos simples (un sprite) y complejos (un nivel completo): ambos son "escenas" instanciables.

> **Para ConsoleEngine**: `SceneSequencer` debe modelar una secuencia narrativa como un árbol donde cada nodo es una `SceneDefinition` con ramas condicionales:
> ```
> IntroScene
>   ├── NamePromptScene (always)
>   ├── [if flag "veteran"] VeteranScene
>   └── [else] NewPlayerScene
> ```
> Serializar este árbol como JSON, no como código.

**Signals (Observer desacoplado)**

Un nodo emite señales (`button.Pressed`, `player.Died`) sin conocer quién escucha. Múltiples listeners pueden conectarse sin modificar el emisor.

> **Para ConsoleEngine**: Añadir eventos C# a interfaces donde el cambio de estado debe notificar a otros sistemas:
>
> ```csharp
> // En ILocalizationService
> event Action<string> LanguageChanged;
>
> // En IGameConfigRepository
> event Action ConfigSaved;
> ```
>
> Sin `LanguageChanged`, cambiar el idioma en runtime mientras una escena está activa requiere reiniciar la escena completa. El `ScenePlayer` podría suscribirse y re-renderizar automáticamente.

**Editor construido sobre el mismo engine**

El editor de Godot es un `EditorPlugin` masivo construido sobre el sistema UI del propio Godot. Cuando se mejora el renderer de texto del engine, el editor automáticamente se beneficia. No hay código duplicado.

> **Para ConsoleEngine**: El preview del editor (`MainViewModel.RebuildPreview()`) actualmente reimplementa la lógica de `ScenePlayer.Play()` pero retornando un string en vez de escribir en consola. Esto es **deuda técnica activa**: si `ScenePlayer` cambia su comportamiento de renderizado, `RebuildPreview()` no se actualiza automáticamente.
>
> La solución es el overload `PixelArtRenderer.ToAnsiString()` + un modo "dry-run" de `ScenePlayer` que retorna string en vez de escribir en `Console`. El preview debe invocar el mismo código que el runtime.

**Streaming por tiles (para mundos grandes)**

Godot recomienda cargar por tiles bajo demanda. Cargar el tile en el que está el jugador + los 8 adyacentes (3×3 grid). Cuando el jugador se mueve, cargar los nuevos tiles adyacentes y descargar los lejanos.

> **Para ConsoleEngine**: `WorldMap.TryGet(id)` carga todo en memoria en el constructor. Implementar en 0.9.0:
> ```csharp
> // Actual: todo en memoria
> var map = WorldLoader.Load("world.world.json");
>
> // Futuro: chunk-based
> var map = new ChunkedWorldMap("world/");
> // Carga solo la región actual + adyacentes
> ```

#### Lo que falló

**Rendimiento 3D en mundos grandes**

El frustum culling de Godot es ineficiente para escenas con 5.000+ objetos estáticos; renderiza contenido invisible. El motor aplica HLOD y LOD automático pero la arquitectura base no tiene spatial hashing eficiente.

> **Para ConsoleEngine** (contexto terminal): La lección aplicable es que el renderer debe solo procesar el área visible de la pantalla. Si el futuro trae un "mundo persistente" con tiles fuera de la pantalla, calcular solo los tiles dentro del `Console.WindowWidth × Console.WindowHeight` actual.

---

### 2.6 Stride / MonoGame / FNA — Patrones .NET

**Fuentes**: [doc.stride3d.net/latest/en/manual/engine/entity-component-system/index.html], [deepwiki.com/MonoGame/MonoGame/5-content-pipeline], [flibitijibibo.com/xnacontent.html], [arch-ecs.gitbook.io/arch]

#### Stride — ECS puro en C#

`EntityComponent` contiene solo datos. `EntityProcessor` contiene toda la lógica. El atributo `[DefaultEntityComponentProcessor(typeof(MyProcessor))]` registra automáticamente el processor cuando el componente aparece en la escena.

> **Para ConsoleEngine**: Si `ConsoleEngine.Animation` necesita procesar muchos elementos (partículas, efectos de pantalla), seguir este patrón: `ParticleComponent` (datos: posición, color, vida) + `ParticleProcessor` (sistema: actualizar, render).

#### Arch-ECS — ECS de alto rendimiento en .NET

Chunks de 16KB para L1 cache. Benchmarks indican rendimiento comparable a implementaciones C++ de ECS. Usado en producción en Space Station 14 y proyectos MonoGame+Arch. Compatible con .NET 8.

> **Para ConsoleEngine**: Referencia técnica concreta si `VfxEngine` (0.7.0) necesita actualizar >1000 partículas por frame. La alternativa simple (listas planas iteradas) es aceptable para efectos de alcance de terminal; adoptar Arch-ECS solo si los benchmarks muestran necesidad real.

#### FNA — Filosofía del content pipeline

FNA rechaza explícitamente reimplementar el content pipeline de XNA. El autor argumenta que un content pipeline genérico optimiza para el caso general, no para las necesidades específicas de cada equipo. Los sistemas custom optimizados para el dominio son superiores.

> **Para ConsoleEngine**: `SceneLoader`, `WorldLoader`, `MarkdownLocalizationLoader` son exactamente la filosofía de FNA aplicada. No reemplazarlos con un content pipeline genérico. Si en el futuro se necesita un pipeline más sofisticado (pre-compilar escenas, cachear assets), construirlo específicamente para las necesidades del proyecto.

---

## 3. Patrones a adoptar

### 3.1 `schemaVersion` en todos los schemas JSON

**Motor que aprendió esto**: Unity (binary serialization fragility en prefabs/saves)

**Problema**: Al agregar `spritePath` en 0.5.0 a `SceneDefinition`, los archivos `.scene.json` creados antes de ese campo no lo tenían. `JsonSerializer` retorna `null` para campos ausentes — esto funciona por coincidencia, no por diseño.

En 0.6.0 añadiremos `FlagStore` a los saves. En 0.7.0 cambiaremos la firma de `IExplorationAction`. Sin `schemaVersion`, es imposible saber si un archivo fue creado con la versión correcta del schema.

**Implementación**:
```json
{
  "schemaVersion": 1,
  "title": "Intro",
  "lines": ["..."],
  ...
}
```

```csharp
// En SceneLoader.SceneDto
public int SchemaVersion { get; init; } = 1;

// Validación
if (dto.SchemaVersion < 1)
    throw new InvalidDataException($"Unsupported schema version: {dto.SchemaVersion}");
```

**Versión**: 0.6.0 — antes de cualquier publicación en NuGet.

---

### 3.2 `LanguageChanged` event en `ILocalizationService`

**Motor que lo hace**: Godot (signals), Unreal (GAS event dispatch)

**Problema**: `CL.SetLanguage("es")` cambia la tabla de strings interna pero no notifica a nadie. Una escena activa sigue mostrando texto en el idioma anterior hasta que se reinicia.

**Implementación**:
```csharp
public interface ILocalizationService
{
    string Get(string key, params object[] args);
    void SetLanguage(string languageCode);
    string CurrentLanguage { get; }
    IReadOnlyList<string> AvailableLanguages { get; }
    IReadOnlyDictionary<string, string> AllEntries { get; }
    event Action<string> LanguageChanged; // nuevo
}
```

`ScenePlayer` puede suscribirse y re-renderizar si el idioma cambia durante la escena (útil para el editor con Language Preview button, Module 2).

**Versión**: 0.7.0

---

### 3.3 Dirty flag en preview del editor

**Motor que falló sin esto**: Unity UGUI Canvas full-rebuild

**Problema**: `SyncAndPreview()` → `RebuildPreview()` se llama en cada keystroke, en cada cambio de propiedad. Para textos largos con ASCII art compleja, esto es trabajo redundante.

**Implementación**:
```csharp
// En MainViewModel
private bool _previewDirty;

private void MarkDirty() => _previewDirty = true;

// Llamado por el timer de UI (cada 100ms max)
public void FlushPreviewIfDirty()
{
    if (!_previewDirty) return;
    _previewDirty = false;
    RebuildPreview();
}
```

**Versión**: 0.6.0

---

### 3.4 `PixelArtRenderer.ToAnsiString()` — editor usa mismo código que runtime

**Motor que lo hace**: Godot (editor construido sobre el engine)

**Problema**: `RebuildPreview()` en `MainViewModel` reimplementa la lógica de layout de `ScenePlayer.Play()`. Si `ScenePlayer` cambia el comportamiento de renderizado (nuevo manejo de `continuePrompt`, nuevo layout de art), el preview del editor no se actualiza.

**Implementación**:
```csharp
// Nuevo overload en PixelArtRenderer
public static string ToAnsiString(string[] sprite, int width, int rows) { ... }

// Nuevo modo en ScenePlayer
public static string RenderToString(SceneDefinition scene, int previewWidth = 54)
{
    var sb = new StringBuilder();
    // Misma lógica que Play() pero escribe en StringBuilder en vez de Console
    return sb.ToString();
}

// En MainViewModel.RebuildPreview()
PreviewText = ScenePlayer.RenderToString(currentDoc.ToDefinition(), previewWidth: 54);
```

**Versión**: 0.6.0

---

### 3.5 `DialogueLoader` — patrón simétrico a `SceneLoader`

**Referente**: FNA philosophy (custom loaders > generic frameworks)

**Problema**: `DialoguePlayer` existe y funciona pero no hay forma de cargar un `DialogueDefinition` desde un archivo `.dialogue.json`. Los juegos construyen `DialogueDefinition` a mano en código.

**Implementación**:
```csharp
// Mismo patrón que SceneLoader
public static class DialogueLoader
{
    public static DialogueDefinition Load(string path) { ... }
    public static bool TryLoad(string path, out DialogueDefinition definition, out string error) { ... }

    private sealed class DialogueDto
    {
        public string SchemaVersion { get; init; } = "1";
        public string Title { get; init; } = "";
        public string[] LeftSpeakerLines { get; init; } = [];
        public string[] RightSpeakerLines { get; init; } = [];
        // ...
        public DialogueDefinition ToDefinition() { ... }
    }
}
```

**Versión**: 0.6.0

---

### 3.6 `SceneSequencer` — node-tree para narrativa

**Referente**: Godot node-tree, Unity SubScene ordering

**Problema**: Para jugar una secuencia de escenas, los juegos actualmente llaman `ScenePlayer.Play()` en bucle manualmente. No hay forma declarativa de definir un flujo narrativo con ramificaciones.

**Implementación**:
```csharp
public sealed class SceneSequencer : IScenePlayer
{
    private readonly List<SceneNode> _nodes;

    public void Play()
    {
        foreach (var node in _nodes)
        {
            if (!node.Condition()) continue;
            var scene = SceneLoader.Load(node.ScenePath);
            ScenePlayer.Play(scene with node.Overrides);
        }
    }
}

public sealed record SceneNode(
    string ScenePath,
    Func<bool>? Condition = null,
    SceneDefinition? Overrides = null
);
```

**Versión**: 0.6.0

---

### 3.7 `IInputProvider` — abstracción del input

**Referente**: Stride (ECS permite mocking), Unity TestRunner (necesita input simulado)

**Problema**: `Console.ReadKey()` directo en cada módulo (`ExplorationPlayer`, `ScenePlayer`, `DialoguePlayer`) hace imposible:
- Tests unitarios sin interacción del teclado
- Rebind de teclas
- Soporte futuro de gamepads

**Implementación**:
```csharp
// En ConsoleEngine.Core
public interface IInputProvider
{
    ConsoleKeyInfo ReadKey(bool intercept = true);
    bool KeyAvailable { get; }
}

// En ConsoleEngine.Input (nuevo módulo 0.9.0)
public sealed class ConsoleInputProvider : IInputProvider
{
    public ConsoleKeyInfo ReadKey(bool intercept = true) => Console.ReadKey(intercept);
    public bool KeyAvailable => Console.KeyAvailable;
}

// En tests
public sealed class MockInputProvider : IInputProvider
{
    private readonly Queue<ConsoleKeyInfo> _keys = new();
    public void Enqueue(ConsoleKey key) => _keys.Enqueue(new ConsoleKeyInfo(key, ...));
    public ConsoleKeyInfo ReadKey(bool intercept = true) => _keys.Dequeue();
    public bool KeyAvailable => _keys.Count > 0;
}
```

**Versión**: 0.9.0

---

## 4. Errores a evitar

| Error | Motor que lo cometió | Por qué ocurrió | Cómo evitarlo en ConsoleEngine |
|---|---|---|---|
| Callbacks por-frame individuales | Unity MonoBehaviour `Update()` | Diseño OOP; cada objeto maneja su tick | Nunca añadir `IUpdatable.OnFrame()` a interfaces del engine |
| Rebuild total en cada cambio | Unity UGUI Canvas | Simplicidad inicial sobre performance | Dirty flag en preview; invalidar solo lo que cambió |
| Sin `schemaVersion` en serialización | Unity (saves binarios), GameMaker | "Añadiremos migración después" — nunca pasa | Añadir en 0.6.0 antes de NuGet |
| God interfaces | Unity `IUnityAdsInitializationListener` | Acumular conveniencias en la interfaz existente | Interfaces ≤ 6 métodos; extensión en extension methods |
| VM/reflection en game loop | GameMaker GML interpreter, UE5 Blueprint | Rapid prototyping sin pensar en perf | Dispatch estático; sin `Activator.CreateInstance` en hot paths |
| Código de editor duplicando lógica de runtime | Unity versión antigua (pre-PlayableDirector) | Editor desarrollado separado del runtime | `ScenePlayer.RenderToString()` compartido entre editor y runtime |
| Sin testing infrastructure | GameMaker, versiones antiguas de Unity | "Es un juego, no software empresarial" | `ConsoleEngine.Tests` en 0.8.0 es no-negociable |
| Macros/DSL no estándar | UE5 `UPROPERTY/UFUNCTION` | Necesidad real (reflection), solución compleja | Usar atributos estándar de C#; source generators si es necesario |
| MediatR/event bus overcomplejo | Proyectos Unity con MediatR | Pattern de empresarial aplicado a games | C# `event Action` es suficiente para single-threaded |
| Sin separación game plugins ↔ editor plugins | Versiones antiguas de Unity packages | Todo mezclado en un Assembly-CSharp | `IGamePlugin` vs `IEditorPlugin` desde el diseño |

---

## 5. Auditoría de ConsoleEngine — Estado actual

### Fortalezas (confirmadas por investigación)

| Decisión | Por qué es correcta | Referente que lo confirma |
|---|---|---|
| `sealed record SceneDefinition` | Immutable data + `with` para variantes | Unity ScriptableObject (separación data/behavior) |
| `ILocalizationService` narrow (5 members) | ISP; sin god-interface | Contraste: Unity `IUnityAdsInitializationListener` 26 members |
| `CL.Get(CK.*)` — sin strings hardcodeadas | FText pattern; gatherable by automation tools | UE5 localization dashboard requiere exactamente esto |
| `SceneLoader` / `WorldLoader` custom | Domain-specific > generic pipeline | FNA philosophy: "build your own content system" |
| `Directory.Build.props` centralizado | Previene drift entre proyectos | Contraste: UE5 `.Build.cs` por módulo → inconsistencias |
| Separación Editor ↔ Runtime via JSON | Authoring/runtime separation | Unity SubScenes (editor bake → runtime load) |
| `sealed` por defecto | Previene herencia no diseñada | UGUI `Canvas` class hierarchy = deuda técnica |
| `System.Text.Json` (sin Newtonsoft) | Zero external dependencies en Core | MonoGame migró a reducir deps; FNA tiene zero deps por política |
| JSON con `AllowTrailingCommas` | UX amigable para edición manual | Asset files editables por humanos es un principio de diseño |

### Debilidades (deuda técnica activa)

| Debilidad | Riesgo si no se corrige | Versión para corregir |
|---|---|---|
| `RebuildPreview()` duplica lógica de `ScenePlayer` | Preview diverge de runtime silenciosamente; bugs en producción | 0.6.0 |
| Sin `schemaVersion` en `.scene.json`/`.world.json` | Migración imposible al añadir campos en 0.7.0+ | 0.6.0 |
| Sin `LanguageChanged` event | Cambiar idioma en runtime no actualiza escenas activas | 0.7.0 |
| `Console.ReadKey()` directo en módulos | No testeable; no rebindable; imposible mocking | 0.9.0 |
| `WorldMap` carga todo en memoria | Lentitud con mundos de 100+ locations | 0.9.0 |
| Sin dirty flag en preview | Rebuild completo en cada keystroke | 0.6.0 |
| Sin `DialogueLoader` | Datos de diálogo hardcoded en código de juego | 0.6.0 |
| Sin events en `IGameConfigRepository` | Config cambia sin notificar sistemas dependientes | 0.7.0 |
| Sin `ConsoleEngine.Tests` | No detectar regresiones antes de publicar en NuGet | 0.8.0 |
| CI sin `dotnet test` | CI puede publicar paquetes con código roto | 0.8.0 |

### Riesgos técnicos por probabilidad × impacto

| Riesgo | P | I | P×I | Mitigación |
|---|---|---|---|---|
| Publicar código roto por falta de tests | Alta | Crítico | **Crítico** | Tests + CI en 0.8.0 |
| `schemaVersion` ausente → migración imposible | Media | Alto | **Alto** | Añadir en 0.6.0 |
| Preview ≠ runtime → bugs no detectados | Media | Medio | **Medio** | Unificar rendering en 0.6.0 |
| `WorldMap` O(n) inicial → lentitud | Baja | Medio | **Bajo** | Chunk-loading en 0.9.0 |
| Plugin system mal diseñado → acoplamiento | Baja | Alto | **Medio** | Diseñar `IEditorPlugin` antes de Module 16 |

---

## 6. Decisiones técnicas por versión

### 0.6.0 — Correcciones de deuda + Editor Phase A

Decisiones a tomar e implementar:

1. **Añadir `schemaVersion: 1` a `.scene.json` y `.world.json`**  
   `SceneLoader` y `WorldLoader` leen el campo y lanzan `InvalidDataException` si el schema es de una versión futura desconocida. Para versiones pasadas (sin el campo), asumir `schemaVersion: 0` y aplicar migración no-op.

2. **`ScenePlayer.RenderToString()` — modo dry-run**  
   Nuevo método estático que retorna `string` en vez de escribir en `Console`. `MainViewModel.RebuildPreview()` llama este método. Elimina la duplicación de lógica de layout.

3. **Dirty flag en `MainViewModel`**  
   Separar "marcar como dirty" de "ejecutar rebuild". El rebuild se dispara solo si dirty, a través de un timer de UI.

4. **`DialogueLoader.Load()` / `TryLoad()`**  
   Patrón idéntico a `SceneLoader`. Archivo `.dialogue.json` con `schemaVersion`.

5. **`SceneSequencer`**  
   Lista ordenada de `SceneNode` con condiciones opcionales y overrides. Implementa `IScenePlayer`.

6. **Roslyn Analyzers activos**  
   `<AnalysisLevel>latest-recommended</AnalysisLevel>` en `Directory.Build.props`. Corregir todos los warnings antes de mergear.

### 0.7.0 — Animation + FlagStore + Events

Decisiones a tomar e implementar:

1. **`ILocalizationService.LanguageChanged` event**  
   Añadir a la interface y a `InMemoryLocalizationService`. `CL.SetLanguage()` dispara el evento.

2. **`IGameConfigRepository.ConfigSaved` event**  
   Añadir para que sistemas que dependen de la config (volumen de audio, display mode) se actualicen sin polling.

3. **`FlagStore` — tipado y serializable**  
   No un `Dictionary<string, object>` genérico. Usar backing store JSON-serializable con método `Set<T>` y `Get<T>` tipado.

4. **`ConsoleEngine.Animation` — módulo nuevo**  
   `AnimationTimeline` + `Keyframe` + `VfxEngine`. Si se necesita procesar >1000 partículas, evaluar Arch-ECS para el inner loop. Para la mayoría de los casos, `List<Particle>` es suficiente.

### 0.8.0 — Launcher + Audio + Tests + CI

Decisiones a tomar e implementar:

1. **`ConsoleEngine.Tests` con xUnit**  
   Criterio mínimo: 1 test por método público de módulos críticos (SceneLoader, WorldLoader, InMemoryLocalizationService, GameSettingsCatalog, SaveRepository).

2. **`IAudioPlayer` interface en Core**  
   `NullAudioPlayer` (implementación vacía) como default. `NAudioPlayer` como implementación opcional. Los 5 canales de `GameConfig` conectados.

3. **CI: `dotnet test` antes de `dotnet pack`**  
   Sin tests verdes, no hay artifact de NuGet.

### 0.9.0 — Input + Editor Phase B

Decisiones a tomar e implementar:

1. **`IInputProvider` en Core**  
   Refactorizar `ExplorationPlayer`, `ScenePlayer`, `DialoguePlayer` para recibir `IInputProvider` via constructor injection. No breaking change: el parámetro tiene default `new ConsoleInputProvider()`.

2. **Chunk-based WorldMap**  
   `ChunkedWorldMap` que carga locations de múltiples archivos bajo demanda. La interfaz `IWorldMap` ya existe en Core — la implementación es un detalle.

3. **Editor Phase B**  
   Decisión de librería de node graph: `NodeNetwork` o `Avalonia.NodeEditor`. Evaluar antes de implementar — esta decisión es difícil de revertir.

### 1.0.0 — API Freeze

Antes de hacer API freeze:
- Todas las interfaces de Core revisadas con los ojos de "¿puede cambiar esto en 2.0?"
- Documentación XML completa en toda la superficie pública
- AkashicEnd consume paquetes NuGet publicados (prueba de que la API es usable externamente)
- `dotnet new consoleengine` template generado y probado

---

## 7. Registro de Decisiones Arquitectónicas (ADR)

> Nota: Este registro complementa la tabla en `ENGINE_PLAN.md §10`.

### ADR-001 — `sealed record` para SceneDefinition (v0.2.0)

**Contexto**: Necesitamos representar datos de escena inmutables que puedan variar por instancia (ej: inyectar líneas dinámicas en una escena JSON base).

**Decisión**: `sealed record SceneDefinition` en vez de `sealed class`.

**Alternativas rechazadas**: `sealed class` con constructor copia manual; `SceneDefinition Clone()`.

**Razón**: `record` proporciona igualdad estructural, `ToString()` automático, y `with` expressions para crear variantes sin mutar el original. Patrón equivalente a Unity's ScriptableObject + ScriptableObject.Instantiate().

**Consecuencias**: `SceneLoader.SceneDto.ToDefinition()` retorna un record. `SceneSequencer` puede crear variantes con `baseScene with { artColor = ConsoleColor.Red }` sin clonar manualmente.

---

### ADR-002 — JSON + `schemaVersion` para todos los schemas (v0.6.0)

**Contexto**: Los archivos `.scene.json`, `.world.json`, y futuros `.dialogue.json` evolucionarán. Los archivos creados con v0.5.0 no tendrán campos añadidos en v0.7.0+.

**Decisión**: Añadir `"schemaVersion": 1` a todos los schemas. Los loaders validan y aplican migraciones por versión.

**Alternativas rechazadas**: YAML (más verboso, sin ventaja para este caso); MessagePack (no human-readable, fragile al agregar campos).

**Razón**: Unity aprendió esto con binary saves — un campo nuevo en el código rompe archivos existentes silenciosamente. JSON con `schemaVersion` explícito permite detectar el problema y aplicar migración.

---

### ADR-003 — `IInputProvider` abstraction (v0.9.0)

**Contexto**: `Console.ReadKey()` está hardcodeado en `ExplorationPlayer`, `ScenePlayer`, `DialoguePlayer`. Esto imposibilita tests unitarios, rebind de teclas, y soporte de gamepads.

**Decisión**: Interface `IInputProvider` en Core, implementación `ConsoleInputProvider` en ConsoleEngine.Input (nuevo módulo), `MockInputProvider` en ConsoleEngine.Tests.

**Alternativas rechazadas**: Framework de input externo (sobrecomplejo); cambiar solo para tests con `#if TEST` (anti-patrón).

**Razón**: Stride/Unity demuestran que la abstracción del input es requisito para cualquier suite de tests de game engine. El coste es bajo (interface de 2 métodos); el beneficio es alto (testing + extensibilidad).

---

### ADR-004 — C# events nativos en vez de MediatR (v0.7.0)

**Contexto**: Necesitamos notificaciones cuando cambia el idioma, cuando se guarda la config, cuando termina una transición.

**Decisión**: Usar `event Action<T>` de C# en interfaces afectadas.

**Alternativas rechazadas**: MediatR (IoC container, async pipeline); Reactive Extensions (complejidad de Observable); custom EventBus class (reinventar lo que C# ya provee).

**Razón**: ConsoleEngine es single-threaded. MediatR agrega un contenedor de inyección de dependencias, async/await overhead, y una abstracción innecesaria para un caso de uso que C# `event` maneja perfectamente. UE5 usa un bus de eventos (GAS) porque necesita cross-thread dispatch; ConsoleEngine no.

---

### ADR-005 — Node-tree para `SceneSequencer` (v0.6.0)

**Contexto**: Los juegos necesitan encadenar escenas con condiciones y ramificaciones, sin escribir código repetitivo.

**Decisión**: `SceneSequencer` con lista de `SceneNode` (path + condition + overrides). Serializable como JSON.

**Alternativas rechazadas**: ECS puro para narrativa (Unity DOTS: curva alta, poco beneficio para texto); código imperativo en el juego (GameMaker: no testeable, no declarativo).

**Razón**: Godot demuestra que el modelo node-tree es más apropiado que ECS para lógica narrativa. `SceneSequencer` es análogo a Godot's AnimationPlayer pero para secuencias narrativas. La serialización JSON permite editar flujos narrativos sin recompilar.

---

### ADR-006 — Chunk-based WorldMap loading (v0.9.0)

**Contexto**: `WorldMap` carga todas las locations en memoria al inicio. Para mundos pequeños (<30 locations) es correcto. Para mundos grandes (100+) es innecesario.

**Decisión**: Implementar `ChunkedWorldMap` que carga regions bajo demanda. La interfaz `IWorldMap` existente en Core no cambia.

**Alternativas rechazadas**: Carga lazy de locations individuales (granularidad demasiado fina, muchas operaciones de disco); streaming continuo en background (complejidad innecesaria para single-thread).

**Razón**: UE5 World Partition y Godot tile loading demuestran que la unidad correcta de streaming no es el objeto individual sino la región/chunk. Para ConsoleEngine, una "región" es un archivo `.world-region.json` que contiene un subconjunto de locations.

---

## 8. Fuentes

### Unity 6
- [Entities Package Manual v1.0.16](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)
- [Archetype Concepts](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/concepts-archetypes.html)
- [Job System Overview](https://docs.unity3d.com/6000.2/Documentation/Manual/job-system-overview.html)
- [Addressables System v1.20.5](https://docs.unity3d.com/Packages/com.unity.addressables@1.20/manual/AddressableAssetsOverview.html)
- [ScriptableObjects and Data-Driven Design](https://unity.com/how-to/architect-game-code-scriptable-objects)
- [Scene Streaming Overview](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/streaming-overview.html)
- [Optimizing Unity UI](https://learn.unity.com/tutorial/optimizing-unity-ui)

### Unreal Engine 5
- [Mass Entity Framework](https://dev.epicgames.com/documentation/en-us/unreal-engine/mass-entity-in-unreal-engine)
- [World Partition](https://dev.epicgames.com/documentation/en-us/unreal-engine/world-partition-in-unreal-engine)
- [Unreal Engine Modules](https://dev.epicgames.com/documentation/en-us/unreal-engine/unreal-engine-modules)
- [GameplayAbilitySystem](https://dev.epicgames.com/documentation/en-us/unreal-engine/understanding-the-unreal-engine-gameplay-ability-system)
- [Blueprint vs C++ Performance](https://www.spongehammer.com/unreal-engine-5-blueprint-vs-cpp-performance/)

### GameMaker Studio 2
- [Rooms Documentation](https://manual.gamemaker.io/lts/en/GameMaker_Language/GML_Reference/Asset_Management/Rooms/Rooms.htm)
- [GameMaker 2025 Development Guide](https://generalistprogrammer.com/tutorials/gamemaker-studio-2-complete-development-guide-2025)

### Flax Engine
- [C++ Scripting](https://docs.flaxengine.com/manual/scripting/cpp/index.html)
- [Hot Reload](https://flaxengine.com/blog/flax-facts-16-scripts-hot-reload/)
- [Prefabs](https://docs.flaxengine.com/manual/get-started/prefabs/index.html)
- [Plugins](https://docs.flaxengine.com/manual/scripting/plugins/index.html)

### Godot Engine
- [Nodes and Scenes](https://docs.godotengine.org/en/stable/getting_started/step_by_step/nodes_and_scenes.html)
- [Engine Architecture](https://docs.godotengine.org/en/stable/engine_details/architecture/index.html)
- [GDExtension](https://docs.godotengine.org/en/stable/tutorials/scripting/gdextension/what_is_gdextension.html)
- [3D Performance Limitations](https://docs.godotengine.org/en/3.0/tutorials/3d/3d_performance_and_limitations.html)

### .NET Ecosystem
- [Stride ECS](https://doc.stride3d.net/latest/en/manual/engine/entity-component-system/index.html)
- [Arch-ECS](https://arch-ecs.gitbook.io/arch)
- [MonoGame Content Pipeline](https://deepwiki.com/MonoGame/MonoGame/5-content-pipeline)
- [FNA: The XNA Content Pipeline is Bad](https://flibitijibibo.com/xnacontent.html)
- [Terminal UI in .NET](https://dev.to/nikolaos_protopapas_d3bd6/building-terminal-uis-in-net-how-sharpconsoleui-complements-terminalgui-hb9)

### Arquitectura de mundos y localización
- [Asset Streaming Techniques for Open World Games](https://daydreamsoft.com/blog/asset-streaming-techniques-for-open-world-games-building-seamless-and-immersive-experiences)
- [Internationalization in Games](https://artlangs.com/news-detail/Internationalization-i18n-Building-Games-for-Global-Markets)
- [Unreal Engine Localization](https://i18nagent.ai/en/guides/unreal-i18n)

---

**Creado**: 2026-05-28  
**Autor**: AkashicEnd Development Team  
**Próxima revisión**: Al completar v0.8.0 (cuando `ConsoleEngine.Tests` tenga cobertura de los módulos principales)
