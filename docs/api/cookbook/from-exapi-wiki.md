# Guidance distilled from the official exApiWiki

> Plugin-authoring guidance mined from the official exApiTools sources and mapped onto this fork's API. [API reference index](../README.md) · [cookbook index](README.md)

The canonical "how to write an ExileApi plugin" guidance lives with the exApiTools project. This page distills it and points each topic at the matching page in this fork's reference. Where the upstream guidance assumes an API name that differs (or is missing) here, the row links the porting reference: [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

> **Note on the source.** The `exApiTools/exApiWiki` repository is currently an empty stub (a single `stub.txt`, no markdown). The genuine canonical guidance ships as inline comments in `exApiTools/PluginTemplate` — the `dotnet new exApiPlugin` scaffold. This page summarizes that template (and its build/setup conventions) and attributes each point. Nothing below is copied verbatim; it is summarized and mapped to our fork's verified API.

---

## 1. Project setup

The template scaffolds a class-library project (`OutputType=Library`, `PlatformTarget=x64`) that references `ExileCore.dll` and `GameOffsets.dll` as `<Private>False</Private>` (do **not** copy the engine DLLs into your output). It pulls `ImGui.NET`, `Newtonsoft.Json` and `SharpDX.Mathematics` from NuGet.

| Template convention | How it maps here | Notes / gotchas |
| --- | --- | --- |
| References resolved via an `$(exapiPackage)` env var (`$(exapiPackage)\ExileCore.dll`) | Same idea: point references at your HUD folder via an env var rather than hard-coded paths | Keeps the `.csproj` portable across machines |
| `<TargetFramework>net8.0-windows</TargetFramework>` | **This fork targets .NET 10, Windows-only** ([../README.md](../README.md)) — build against the framework your engine ships | The template TFM is just the upstream default; bump it to match the engine you load into |
| Engine refs use `<Private>False</Private>` | Same — never ship `ExileCore.dll`/`GameOffsets.dll` in your plugin folder | The host already has them; duplicating risks load conflicts |
| Drop source in `Plugins/Source/<Name>/`; HUD sets the output path for you | Matches our discovery rules: `Plugins/Source` (compiled at startup) and `Plugins/Compiled/<Name>/<Name>.dll` | See *Discovery: compiled vs source plugins* in [../plugins.md](../plugins.md). A source folder is skipped if it left an `Errors.txt`. |
| Folder name = namespace = DLL name | Same requirement here | The compiled DLL name must match its folder; `InternalName` is `GetType().Namespace` |
| `dotnet pack` produces the release package (CI on tag) | Build convention only; not engine API | The template's GitHub Action packs on release |

The plugin is two classes — a settings class implementing `ISettings` and a plugin class deriving from `BaseSettingsPlugin<TSettings>`. Identical to our [first-plugin walkthrough](../README.md#your-first-plugin).

---

## 2. The plugin lifecycle

The template's scaffold overrides exactly the five hooks most plugins need: `Initialise`, `AreaChange`, `Tick`, `Render`, `EntityAdded`. Each carries a short distilled rule:

| Hook (template guidance) | Distilled rule | Our reference |
| --- | --- | --- |
| `Initialise()` | One-time init. Load any custom config here. Return `true`; **return `false` to abort** (plugin gets disabled). | [../plugins.md](../plugins.md) — `IPlugin.Initialise`; on `false` the engine sets `Enable.Value = false`. |
| `AreaChange(AreaInstance area)` | Once-per-zone work (e.g. Radar rebuilds its map texture here). | [../plugins.md](../plugins.md); also fired right after a successful `Initialise()` so you see the current zone. |
| `Tick()` | Non-render work (position math, decisions). Still runs every frame on the main thread — keep it fast. | [../plugins.md](../plugins.md) — *Tick returning a `Job`*. |
| `Render()` | All ImGui/`Graphics` drawing. Runs **after** `Tick` in the same frame. | [../graphics.md](../graphics.md) for drawing. |
| `EntityAdded(Entity entity)` | Process each gameplay entity exactly once. The template advises queueing the entity and doing real work in `Tick`. | [../entities.md](../entities.md) — only entities with `(int)Type >= 100` reach `EntityAdded`. |

Wiki-attributed ordering fact the template implies and our engine confirms: in a frame, **every plugin's `Tick()` runs before any `Render()`**, and `AreaChange`/`EntityAdded` are event hooks delivered independently to enabled plugins. Full verified call order (construction → `SetApi` → `_LoadSettings` → `OnLoad` → `Initialise` → per-frame `Tick`/`Render`) is in [../plugins.md](../plugins.md).

The base class supplies no-op virtuals for every hook, so you override only what you use — exactly what the template demonstrates.

---

## 3. Settings

Template guidance, condensed:

- **Always declare `ToggleNode Enable`** (the template seeds `new ToggleNode(false)`). This is the master on/off switch the engine checks before ticking. Required by `ISettings`.
- **Prefer the built-in node types** over hand-rolled UI. The template explicitly says: nested menus, ready-made nodes and custom callbacks are all supported — *"if you want to override `DrawSettings` instead, you better have a very good reason."*

Our fork honors this exactly. The auto-built menu reflects over your node-typed properties; you should almost never override `DrawSettings()`. The full node catalogue (`ToggleNode`, `RangeNode<T>`, `HotkeyNode`, `ColorNode`, `ButtonNode`, `ListNode`, `TextNode`, `FileNode`, `StashTabNode`, `EmptyNode`), the `[Menu]`/`[IgnoreMenu]` attributes and the nesting pattern are in [../settings.md](../settings.md).

> Default toggle: the template ships `Enable = new ToggleNode(false)` (off by default) so a freshly dropped plugin does nothing until the user opts in. The fork's own first-plugin example uses `new(true)`; pick whichever fits your plugin.

---

## 4. Drawing

The template's `Render()` is a one-liner: `Graphics.DrawText($"Plugin {GetType().Name} is working.", new Vector2(100, 100), Color.Red);`. Distilled lessons:

- All drawing goes through the injected `Graphics` facade, **only from `Render()`**.
- `Color` is `SharpDX.Color`; the position can be SharpDX `Vector2` or `System.Numerics.Vector2` (the template aliases `using Vector2 = System.Numerics.Vector2;`). Most `Graphics` methods are overloaded for both.

Maps directly to [../graphics.md](../graphics.md): `DrawText`/`DrawLine`/`DrawBox`/`DrawFrame`/`DrawImage`, `MeasureText`, fonts and atlas textures. Screen-space conversion of world positions is in [../coordinates.md](../coordinates.md) (`Camera.WorldToScreen`).

---

## 5. Reading entities & components

The template's `EntityAdded` comment captures the core idea: entities arrive via hooks; do per-entity bookkeeping once and process in `Tick`. The fork's model is the same ECS one — an `Entity` is a bag of components you fetch by type.

| Task | API here | Reference |
| --- | --- | --- |
| Iterate gameplay objects | `GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]` (every enum key has a list — safe to index) | [../entities.md](../entities.md) |
| Get a component | `entity.GetComponent<Life>()` (returns `null` if absent) | [../entities.md](../entities.md), [../components-combat.md](../components-combat.md) |
| Check a component | `entity.HasComponent<Chest>()` | [../entities.md](../entities.md) |
| World position → screen | `GameController.IngameState.Camera.WorldToScreen(entity.Pos)` | [../coordinates.md](../coordinates.md) |
| Static game data | `GameController.Files` (`BaseItemTypes`, `Mods`, …) | [../files-in-memory.md](../files-in-memory.md) |

---

## 6. Performance tips

The template's `Tick()` comment is the headline performance lesson, distilled:

> Doing heavy work in `Tick` (or `Render`) only beats "throw everything in `Render`" if you return a **custom `Job`** — that moves the work off the main thread.

It shows the pattern: `return new Job($"{nameof(Plugin_Name)}MainJob", () => { /* work */ });`. Mapped to our fork:

- Return a `Job` from `Tick()` to offload heavy computation to a worker thread; return `null` (the default) to run inline. The engine runs all jobs, then waits for them before the render pass. A job that overruns its budget gets skipped for rendering that frame. See *Tick returning a `Job`* in [../plugins.md](../plugins.md).
- This is an **advanced** technique (the template says so). Most plugins just do the work in `Tick` and `return null`.
- Complementary, fork-specific lever the template predates: wrap expensive scans/filters in a cache (`TimeCache`, `FrameCache`, `FramesCache`) and read `.Value`. This is how the reference plugins throttle work — see [../caching.md](../caching.md).

---

## 7. Gotchas

| Gotcha (template / upstream) | What to do on this fork |
| --- | --- |
| **`ConfigDirectory` for custom config.** The template's `Initialise()` comment loads a custom file via `Path.Join(ConfigDirectory, "custom_config.txt")`. | **This fork does not expose a `ConfigDirectory` member on `BaseSettingsPlugin`** (verified absent from `Core/`). Use `DirectoryFullName` (the plugin's folder, used for the `textures/` atlas) to locate plugin-relative files, or persist via the settings system. See `DirectoryFullName` / `GetAtlasTexture` in [../plugins.md](../plugins.md) and the porting notes in [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). |
| "Only load custom config if builtin settings are inadequate." | Strong advice here too — prefer node-typed settings in [../settings.md](../settings.md); they get the menu and JSON persistence for free. |
| Template TFM is `net8.0-windows`. | Build against **.NET 10** to match this fork's engine ([../README.md](../README.md)). |
| `System.Numerics.Vector2` vs `SharpDX.Vector2` mix (the template aliases one of them). | Both appear across the API. `Entity.Pos`/`GridPos` are SharpDX; `Graphics.DrawText` returns `System.Numerics`. Coordinate-space and type details: [../coordinates.md](../coordinates.md). |
| Upstream `Entity.PosNum`/`GridPosNum`, `TryGetComponent<T>(out T)`, `Entity.Bounds`. | **Not present in this fork.** Use SharpDX `Pos`/`GridPos`, the `var c = GetComponent<T>(); if (c != null)` pattern, and `Render.Bounds`. Mappings: [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md), [../entities.md](../entities.md). |
| Returning a `Job` is "a bit of an advanced technique." | Confirmed: `Job` runs only when worker threads are configured; a runaway job is dropped from that frame's render. Keep `Tick` itself cheap. [../plugins.md](../plugins.md). |
| Don't set the output path in `.csproj`. | Correct here — the host sets it when source lives under `Plugins/Source`. [../plugins.md](../plugins.md). |

---

## Source

- `exApiTools/exApiWiki` — the official wiki repository. As of this writing it contains only `stub.txt` (no markdown content); attributed as an empty stub.
- `exApiTools/PluginTemplate` — the `dotnet new exApiPlugin` scaffold, which carries the canonical inline guidance distilled above:
  - `templates/exApiPlugin/Plugin_Name.cs` — lifecycle hooks (`Initialise`, `AreaChange`, `Tick`, `Render`, `EntityAdded`) and their commentary, including the `Job` and `ConfigDirectory` notes.
  - `templates/exApiPlugin/Plugin_NameSettings.cs` — the mandatory `Enable` toggle and the "prefer built-in nodes over `DrawSettings`" guidance.
  - `templates/exApiPlugin/Plugin_Name.csproj` — project/reference/NuGet conventions (`$(exapiPackage)`, `<Private>False</Private>`, `net8.0-windows`).
  - `.github/workflows/build.yml`, `pack.csproj` — `dotnet pack` release convention.

Cross-checked against this fork's `Core/` (`BaseSettingsPlugin.cs`, `Shared/Interfaces/IPlugin.cs`, `EntityListWrapper.cs`) and the reference pages linked above.
