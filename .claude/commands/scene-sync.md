Check that the editor model and the runtime model are in sync.

Read both files:
- `src/ConsoleEngine.Editor/Models/SceneDocument.cs`   (editor serialization model)
- `src/ConsoleEngine.Scenes/SceneDefinition.cs`         (runtime data model)
- `src/ConsoleEngine.Scenes/SceneLoader.cs`             (DTO → SceneDefinition mapping)

Then verify:
1. Every property in `SceneDefinition` has a matching property in `SceneDocument`
   with the same JSON name and compatible type.
2. Every property in `SceneDocument` is mapped inside `SceneLoader.SceneDto.ToDefinition()`.
3. Default values in `SceneDocument.Empty()` are reasonable starting values.

Output:
- A table: Property | In SceneDefinition | In SceneDocument | In SceneDto | Mapped in ToDefinition
- List any MISSING or MISMATCHED entries as ⚠ warnings
- List any property in the editor that has no counterpart in the runtime as ❌ errors

If everything is in sync, say "All models in sync — no drift detected."

Do not make any changes. Report only.
