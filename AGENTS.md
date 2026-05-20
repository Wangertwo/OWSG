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
- Use `-testFilter` with the full test name (supports regex or semicolon lists): `Unity.exe -batchmode -runTests -projectPath "<path>" -testPlatform EditMode -testFilter "Namespace.ClassName.TestName" -testResults "<path>/TestResults.xml" -quit`
- Filter examples: exact `MyTestClass.MyMethod`, partial `MyTest`, multiple `TestA;TestB` (OR logic)
- Run by category: `-testCategory "Fast;Smoke"` (supports `!` for negation)
- Run by assembly: `-assemblyNames "MyTests.Assembly"`

### Notes
- No `-testPlatform` defaults to EditMode; `-runSynchronously` is EditMode-only (excludes multi-frame tests)
- Test results are NUnit XML; store outside `Assets/` to avoid extra meta changes
- Exit code 0 = all tests passed, non-zero = failures occurred

## Lint / Format
- No lint/format tooling or root `.editorconfig` was found.
- Use IDE analyzers (Rider/VS) and Unity warnings as the default quality bar.
- If you introduce a formatter or analyzer, update this file with commands and config locations.

## Unity Packages
- Key packages: HDRP, URP, Shader Graph, Timeline, TextMeshPro, Visual Scripting
- Avoid upgrading versions; keep rendering changes coordinated with scene/pipeline assets
- Use Package Manager, do not edit `Packages/` directly

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
- Braces on next line (Allman), indent with 4 spaces, blank lines between logical blocks
- Keep methods focused; extract helpers; comments only for non-obvious logic

### Imports (using directives)
- Order groups from general to specific:
  1. `System.*`
  2. `UnityEngine` and `UnityEditor` (Editor-only code)
  3. Third-party namespaces (for example, `TMPro`)
  4. Project-specific namespaces
- Remove unused `using` statements.

### Types and Collections
- Prefer explicit types for clarity with Unity components; `var` when type is obvious
- Use `List<T>` and `Dictionary<TKey, TValue>` from `System.Collections.Generic`

### Serialization and Inspector Usage
- Follow existing patterns; many fields are public for Inspector assignment
- Cache component references in `Awake`/`Start`; avoid per-frame `GetComponent`
- Resources.Load uses item names without extensions; expects `_Model` suffix for equippables

### Unity Lifecycle Patterns
- `Awake`: singleton setup, caching; `Start`: runtime initialization; `OnEnable`/`OnDisable`: events
- Minimize per-frame allocations in `Update`/`FixedUpdate`; use `Time.deltaTime`

### Singleton Pattern
- Managers use `Instance` static property; in `Awake`: if `Instance != null && Instance != this`, `Destroy(this)`, else `Instance = this`
- Use `get; set;` or `get; private set;` consistently within each file (both exist in codebase)

### Input and UI
- Legacy Input Manager in use (`Input.GetAxis`, `Input.GetButtonDown`); migrate to new Input System consistently if updating
- UI uses TextMeshPro; prefer `TextMeshProUGUI` components

### Error Handling and Logging
- Guard against `null` for referenced objects (especially Inspector-assigned fields)
- Use `Debug.LogWarning` for recoverable issues, `Debug.LogError` for critical ones
- Prefer early returns; avoid per-frame logging unless debugging

### Performance Considerations
- Avoid `Camera.main` in tight loops (cache refs e.g., `SelectionManager.cs:48`)
- Prefer object pooling; avoid LINQ in per-frame code

## Assets and Metadata
- Keep `.meta` files in sync; follow folder conventions under `Assets/`
- Do not modify `Library/`, `Temp/`, or `Logs/`

## Scenes and Prefabs
- Prefer editing prefab assets over scene-only overrides; keep changes localized
- Verify serialized fields remain assigned when changing scene references
- Avoid editing imported demo assets

## Source Control Hygiene
- Do not add/modify `Library/`, `Temp/`, or `Logs/`
- Keep `.meta` files paired with assets; avoid large binaries unless necessary

## Editor-Only Code
- Wrap editor code in `#if UNITY_EDITOR` with `using UnityEditor` inside; place under `Editor/` folder
- No editor-only scripts currently exist

## Cursor / Copilot Rules
- No Cursor rules found (`.cursor/rules/` or `.cursorrules`).
- No Copilot rules found (`.github/copilot-instructions.md`).

## When Unsure
- Prefer minimal, localized changes; follow patterns in nearby scripts
- Update this file when adding tooling or new conventions

## Debugging Tips
- Use `Debug.Break()` to pause editor; `[Conditional("DEBUG")]` for debug code
- `Debug.LogAssertion` for unexpected non-breaking conditions

## Additional Patterns Observed
- `DestroyImmediate` is used in some contexts (e.g., `EquipSystem.cs`, `ConstructionManager.cs`); prefer `Destroy` for runtime unless immediate cleanup is required.
- `Time.time` is used for cooldowns and debouncing (e.g., sound cooldowns in `SoundManager.cs`).
- Input checking is often combined with state checks before action execution (e.g., inventory/menu state).
