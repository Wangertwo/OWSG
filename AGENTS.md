# Agent Guide (Unity Project)

This file is for automated coding agents working in this repo. It captures how to build/test and the code style to follow.

## Project Snapshot
- Unity editor version: 2022.3.62f3 (`ProjectSettings/ProjectVersion.txt`).
- Main code lives under `Assets/` (scripts in `Assets/Scripts`).
- Solution file: `open world project.sln`.
- Generated folders: `Library/`, `Temp/`, `Logs/`, `obj/`, `Build/` (do not edit).
- Render pipeline packages include both HDRP and URP; confirm the active pipeline before changing settings.

## Build / Run
- Preferred: open the project in Unity Hub and use the Editor build flow.
- No custom build scripts found in the repo.
- For batchmode, use absolute `-projectPath` values and quote paths with spaces.
- Batchmode template (use when adding a build method):
  - Windows:
    - "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -quit -projectPath "<path>" -executeMethod <Namespace.Class.Method>
  - macOS:
    - "/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity" -batchmode -quit -projectPath "<path>" -executeMethod <Namespace.Class.Method>
- If you add build automation, document the exact method name and required arguments here.

## Tests
### Unity Test Runner (Editor UI)
- Window: **Window > General > Test Runner**.
- Use **EditMode** or **PlayMode** tabs depending on test type.

### Command Line (Batchmode)
Uses the Unity Test Framework (`com.unity.test-framework` 1.1.33).

- Run all EditMode tests:
  - Unity.exe -batchmode -runTests -projectPath "<path>" -testPlatform EditMode -testResults "<path>/TestResults.xml" -quit
- Run all PlayMode tests:
  - Unity.exe -batchmode -runTests -projectPath "<path>" -testPlatform PlayMode -testResults "<path>/TestResults.xml" -quit

### Run a Single Test
- Use `-testFilter` with the full test name (supports regex or semicolon lists):
  - Unity.exe -batchmode -runTests -projectPath "<path>" -testPlatform EditMode -testFilter "Namespace.ClassName.TestName" -testResults "<path>/TestResults.xml" -quit
- Run by category:
  - -testCategory "Fast;Smoke" (supports `!` for negation)
- Run by assembly:
  - -assemblyNames "MyTests.Assembly"
- Filter examples:
  - Exact match: `MyTestClass.MyMethod`
  - Partial match: `MyTest` (matches any test containing "MyTest")
  - Multiple: `TestA;TestB` (matches tests containing TestA OR TestB)

### Notes
- If no `-testPlatform` is set, Unity defaults to EditMode.
- `-runSynchronously` is only supported for EditMode and excludes multi-frame tests.
- Test results are NUnit XML; inspect the XML for failures.
- No tests or test assemblies were found under `Assets/`. Add `Tests/` and/or `.asmdef` files if you introduce new tests.
- Store test result XML outside `Assets/` to avoid extra meta changes.
- Exit code 0 = all tests passed, non-zero = failures occurred.

## Lint / Format
- No lint/format tooling or root `.editorconfig` was found.
- Use IDE analyzers (Rider/VS) and Unity warnings as the default quality bar.
- If you introduce a formatter or analyzer, update this file with commands and config locations.

## Unity Packages
- Key packages in use: HDRP, URP, Shader Graph, Timeline, TextMeshPro, Visual Scripting.
- Avoid upgrading package versions unless required; document any changes here.
- Keep project-wide rendering changes coordinated with scene/pipeline assets.
- Do not edit content under `Packages/` directly; use the Package Manager.

## Code Style Guidelines (C# / Unity)
### File and Type Organization
- One primary public class per file; filename matches class name.
- Keep MonoBehaviour classes in `Assets/Scripts` unless a feature folder exists.
- Avoid placing project code in `Library/PackageCache` or `Packages/`.
- For Editor-only scripts, place them under an `Editor/` folder.

### Naming
- Types, methods, properties: PascalCase.
- Local variables and parameters: camelCase.
- Unity event methods use Unity names (`Awake`, `Start`, `Update`, `FixedUpdate`).
- Constants: use PascalCase or UPPER_SNAKE, but keep it consistent per file.
- Existing fields sometimes include underscores (for example, `interaction_Info_UI`); keep consistency within a file.

### Formatting
- Braces on next line (Allman) to match existing scripts.
- Indent with 4 spaces.
- Put a blank line between logical blocks in methods.
- Keep methods focused; extract helpers when logic grows.
- Add comments only when needed to explain non-obvious logic.

### Imports (using directives)
- Order groups from general to specific:
  1. `System.*`
  2. `UnityEngine` and `UnityEditor` (Editor-only code)
  3. Third-party namespaces (for example, `TMPro`)
  4. Project-specific namespaces
- Remove unused `using` statements.

### Types and Collections
- Prefer explicit types when it improves clarity for Unity components.
- `var` is acceptable when the type is obvious from the right-hand side.
- Use `List<T>` and `Dictionary<TKey, TValue>` from `System.Collections.Generic`.

### Serialization and Inspector Usage
- Prefer `private` fields with `[SerializeField]` over `public` when practical.
- Cache component references in `Awake` or `Start` instead of calling `GetComponent` every frame.
- Keep `Resources.Load` paths stable if used; avoid heavy per-frame loads.

### Unity Lifecycle Patterns
- Use `Awake` for caching/setup and `Start` for runtime initialization.
- Use `OnEnable`/`OnDisable` for event subscriptions.
- Minimize per-frame allocations in `Update` or `FixedUpdate`.
- Use `Time.deltaTime` for frame-rate independent movement.

### Input and UI
- Legacy Input Manager is in use (`Input.GetAxis`, `Input.GetButtonDown`).
- If migrating to the new Input System, do so consistently and update this doc.
- UI uses TextMeshPro; prefer TMP components (`TextMeshProUGUI`) when touching UI.

### Error Handling and Logging
- Guard against `null` for referenced objects (especially Inspector-assigned fields).
- Use `Debug.LogWarning` for recoverable issues and `Debug.LogError` for critical ones.
- Prefer early returns to reduce nesting when handling invalid state.
- Avoid logging every frame unless needed for debugging.

### Performance Considerations
- Avoid `Camera.main` calls in tight loops; cache references when possible.
- Prefer object pooling for frequently created objects.
- Avoid LINQ in per-frame code paths.

## Assets and Metadata
- Keep `.meta` files in sync; never delete them without intent.
- When adding assets, follow existing folder conventions under `Assets/`.
- Do not modify contents under `Library/`, `Temp/`, or `Logs/`.

## Scenes and Prefabs
- Prefer editing prefab assets over scene-only overrides when changes are reusable.
- Minimize large-scale scene refactors unless requested; keep changes localized.
- When changing scene references, verify serialized fields remain assigned.
- Avoid editing imported demo assets unless explicitly needed.

## Source Control Hygiene
- Do not add or modify files in `Library/`, `Temp/`, or `Logs/`.
- Keep `.meta` files paired with their assets in commits.
- Avoid large binary assets unless necessary; note sizes in the PR/summary.
- If you add tooling, document its commands in this file.

## Editor-Only Code
- Wrap editor-only code in `#if UNITY_EDITOR` and keep `using UnityEditor` inside the guard.
- Place editor scripts under an `Editor/` folder to avoid player builds including them.

## Cursor / Copilot Rules
- No Cursor rules found (`.cursor/rules/` or `.cursorrules`).
- No Copilot rules found (`.github/copilot-instructions.md`).

## When Unsure
- Prefer minimal, localized changes.
- Follow patterns in nearby scripts (naming and lifecycle methods).
- Update this file if you add tooling or new conventions.

## Debugging Tips
- Use `Debug.Break()` to pause the editor during play mode.
- Use `[Conditional("DEBUG")]` attribute to conditionally include debug code.
- Use `Debug.LogAssertion` for unexpected conditions that shouldn't happen but don't break gameplay.
- Inspect `UnityEngine.Debug` for available logging methods.
