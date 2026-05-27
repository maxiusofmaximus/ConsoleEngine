# Contributing to ConsoleEngine

Thank you for your interest in contributing! This guide explains how to set up a
development environment, the branch and PR workflow, and the coding standards expected
of all contributors.

---

## Table of Contents

1. [Getting started](#getting-started)
2. [Project structure](#project-structure)
3. [Development workflow](#development-workflow)
4. [Coding standards](#coding-standards)
5. [Tests and CI](#tests-and-ci)
6. [Submitting a pull request](#submitting-a-pull-request)
7. [Versioning and releases](#versioning-and-releases)

---

## Getting started

**Prerequisites**

| Tool | Minimum version |
|---|---|
| .NET SDK | 8.0 |
| Git | any recent |
| Python 3 | 3.8+ (only for the `scan.py` doc script) |

**Clone and build**

```bash
git clone https://github.com/maxiusofmaximus/ConsoleEngine.git
cd ConsoleEngine
dotnet restore ConsoleEngine.sln
dotnet build  ConsoleEngine.sln --configuration Release
```

**Run the samples**

```bash
cd samples/HelloConsoleEngine && dotnet run
cd samples/DinoGame           && dotnet run
cd samples/WorldDemo          && dotnet run
```

**Run the visual editor**

```bash
cd src/ConsoleEngine.Editor && dotnet run
```

---

## Project structure

```
ConsoleEngine/
  src/
    Directory.Build.props          ← shared MSBuild / NuGet metadata (version lives here)
    ConsoleEngine.Core/
    ConsoleEngine.Locale/
    ConsoleEngine.Rendering/
    ConsoleEngine.Config/
    ConsoleEngine.Persistence/
    ConsoleEngine.Scenes/
    ConsoleEngine.World/
    ConsoleEngine.Editor/          ← Avalonia desktop app — not published as NuGet
  samples/
    HelloConsoleEngine/
    DinoGame/
    WorldDemo/
  assets/
    icon.png                       ← 128×128 NuGet package icon
  .github/
    workflows/
      ci.yml                       ← build + pack on every push/PR to main
      nuget-publish.yml            ← publish to NuGet.org on v* tag
  ConsoleEngine.sln
  README.md
  CHANGELOG.md
  CONTRIBUTING.md
  LICENSE
```

---

## Development workflow

1. **Fork** the repository (external contributors) or **create a branch** (maintainers).
2. Branch naming convention:
   - `feat/<short-description>` — new feature
   - `fix/<short-description>` — bug fix
   - `chore/<short-description>` — tooling, CI, dependency bumps
   - `docs/<short-description>` — documentation only
3. Commit early and often. Keep commits small and focused.
4. All commits must compile and pass `dotnet build --configuration Release`.
5. Open a pull request against `main` when ready for review.

---

## Coding standards

ConsoleEngine follows **DRY · SOLID · KISS · Clean Code** strictly.

### General

- **No hardcoded game data** in compiled code. All content (scenes, world definitions, locale
  strings) belongs in JSON or Markdown files under `GameData/`.
- **Prefer immutable types.** Use `sealed record` for data models so `with`-expressions are
  available. Use `init`-only properties.
- **Single responsibility.** Each class, method, and module does one thing. If a method is over
  ~30 lines, consider splitting it.
- **Small, expressive methods.** Extract helpers early. Name them after what they do, not how.
- **Explicit is better than implicit.** Prefer `ArgumentNullException.ThrowIfNull` over silent
  null-checks; throw with a descriptive message.

### Naming

| Construct | Convention | Example |
|---|---|---|
| Namespace | `ConsoleEngine.<Module>` | `ConsoleEngine.Scenes` |
| Class / record / struct | PascalCase | `SceneDefinition` |
| Interface | `I` + PascalCase | `ILocalizationService` |
| Method | PascalCase | `LoadMostRecent()` |
| Property | PascalCase | `CurrentLanguage` |
| Private field | `_camelCase` | `_slotIdSelector` |
| Constant | PascalCase or ALL_CAPS (no preference) | `MaxSlots` |
| Local variable | camelCase | `slotId` |

### XML documentation

- Every `public` member in a library project **must** have an XML doc comment (`<summary>`).
- Use `<paramref>`, `<typeparamref>`, `<see cref="…"/>` liberally.
- Avoid restating the obvious: `/// <summary>Gets the ID.</summary>` is not useful.
- Build with `GenerateDocumentationFile = true` (already set in `Directory.Build.props`).
  Zero doc-related warnings are required.

### JSON files

- Scene files: `*.scene.json` under `GameData/scenes/`
- World files: `*.world.json` under `GameData/world/`
- Locale files: `*.md` under `GameData/locale/`
- Indent with 2 spaces. Use `string.Empty` (`""`) rather than null for optional string fields.

### Dependencies

- Do not add new third-party NuGet packages to library projects without discussion.
  The current footprint is zero external runtime dependencies (System.Text.Json is built into .NET 8).
- `ConsoleEngine.Editor` (Avalonia) is an exception — it is a desktop tool, not a library.

---

## Tests and CI

There is currently no automated test project. If you add tests:

- Place them in `tests/ConsoleEngine.<Module>.Tests/`.
- Use `xUnit` + `FluentAssertions`.
- Add a `dotnet test` step to `ci.yml`.

The CI pipeline (`ci.yml`) runs on every push and pull request to `main`:

1. `dotnet restore`
2. `dotnet build --configuration Release`
3. `dotnet pack --configuration Release --no-build` (dry-run, for all 7 library projects)

Pull requests must pass CI before they can be merged.

---

## Submitting a pull request

1. Ensure your branch is up to date with `main` (`git pull --rebase origin main`).
2. Run a full Release build locally: `dotnet build ConsoleEngine.sln -c Release` — zero errors,
   zero warnings.
3. Update `CHANGELOG.md` under an `[Unreleased]` section.
4. Open the PR with a clear title and description:
   - **What** changed.
   - **Why** the change is needed.
   - **How** to test it manually (if no automated tests).
5. Link any related issues.
6. A maintainer will review and either approve or request changes.

---

## Versioning and releases

ConsoleEngine follows [Semantic Versioning 2.0.0](https://semver.org/).

| Bump | When |
|---|---|
| **PATCH** `x.y.Z` | Bug fixes only, fully backward-compatible |
| **MINOR** `x.Y.0` | New backward-compatible features |
| **MAJOR** `X.0.0` | Breaking API changes |

**To cut a release:**

1. Update `<Version>` in `src/Directory.Build.props`.
2. Add a dated section to `CHANGELOG.md`.
3. Commit: `git commit -m "chore: bump version to X.Y.Z"`.
4. Tag: `git tag vX.Y.Z`.
5. Push tag: `git push origin vX.Y.Z`.

The `nuget-publish.yml` workflow picks up the tag and publishes all 7 packages automatically.
A `NUGET_API_KEY` secret must be set in the repository's GitHub settings.

---

*Made with ❤️ by [maxiusofmaximus](https://github.com/maxiusofmaximus)*
