Scaffold a new ConsoleEngine module named: $ARGUMENTS

If no name is given, ask the user for the module name (e.g. "Animation", "Audio").

Steps:
1. Confirm the module name and what it will be responsible for. Ask if unclear.

2. Create the project file at:
   `src/ConsoleEngine.$NAME/ConsoleEngine.$NAME.csproj`

   Follow the existing pattern (use `ConsoleEngine.Rendering` as template):
   - `<OutputType>` omitted (library)
   - `<TargetFramework>net8.0</TargetFramework>`
   - `<Nullable>enable</Nullable>`
   - `<ImplicitUsings>enable</ImplicitUsings>`
   - Add `<PackageReference>` only if strictly needed
   - Add `<ProjectReference>` to `ConsoleEngine.Core` (always)
   - Add `<ProjectReference>` to `ConsoleEngine.Locale` or `ConsoleEngine.Rendering` only if needed

3. Add the project to `ConsoleEngine.sln`:
   Run: `dotnet sln ConsoleEngine.sln add src/ConsoleEngine.$NAME/ConsoleEngine.$NAME.csproj`

4. Create a primary interface in `ConsoleEngine.Core` if this module introduces a new
   abstraction used by multiple modules. Place it at:
   `src/ConsoleEngine.Core/I$NAME.cs`
   Keep it narrow (≤6 methods — ISP).

5. Create a minimal concrete implementation as a starting point:
   `src/ConsoleEngine.$NAME/$NAMEEngine.cs` (or appropriate name)
   - Mark it `public static class` if it has no state, or `public sealed class` if it does
   - Add XML doc to the class and every public member
   - Add one working method stub (not `NotImplementedException` — at minimum a no-op with a TODO comment)

6. Run `dotnet build ConsoleEngine.sln` to confirm the new project compiles.

7. Report what was created and what the developer should implement next.

SOLID reminders for new modules:
- SRP: one module = one concern
- ISP: interface in Core must be narrow
- DIP: the module should depend on Core interfaces, not concrete classes from other modules
- KISS: start with the simplest thing that works; don't design for hypothetical future requirements
