# MyPlugin — starter plugin template

> **EXPERIMENTAL — not compiled in this environment.** This template is written against this
> fork's real API (ExileApi/ExileCore, .NET 10, Windows-only) but it has **not** been built
> or run here. It targets a live game on Windows; verify it compiles and behaves correctly on
> a Windows machine with the game running before relying on it.

A minimal, fully-commented starter plugin adapted to **this fork** (NelleYn/PoeAPI). It is a
clean rewrite of the upstream [exApiTools/PluginTemplate](https://github.com/exApiTools/PluginTemplate)
that uses only API members confirmed to exist in this repository's `master` branch.

## Files

| File | What it shows |
| --- | --- |
| `MyPluginSettings.cs` | An `ISettings` class: the mandatory `ToggleNode Enable`, plus example `RangeNode<int>`, `ColorNode`, `HotkeyNode` and `TextNode` properties grouped with `[Menu]`. |
| `MyPlugin.cs` | A `BaseSettingsPlugin<MyPluginSettings>` overriding `Initialise`, `AreaChange`, `Tick`, `Render` and `EntityAdded`. Reads the player, iterates monsters, projects world→screen and draws frames + labels gated behind a hotkey. |
| `MyPlugin.csproj` | SDK-style project targeting `net10.0-windows`, referencing `ExileCore.dll` / `GameOffsets.dll` from the host output folder. Mirrors the source-plugin build convention. |
| `README.md` | This file. |

## Install & run

1. Copy this folder into the host's source-plugins directory and rename it to match the
   plugin name:

   ```
   Plugins/Source/MyPlugin/
       MyPlugin.cs
       MyPluginSettings.cs
       MyPlugin.csproj
   ```

   (See the project root [`README.md`](../../README.md): "Compiled plugins go in
   `Plugins\Compiled\<name>\`; source plugins go in `Plugins\Source\<name>\`.")

2. Point the `exapiPackage` environment variable (used in `MyPlugin.csproj`) at the host's
   build-output folder — for this fork that is the `PoeHelper` directory produced by
   `Core/Core.csproj`'s `<OutputPath>`, which contains `ExileCore.dll` and `GameOffsets.dll`.

3. Start the host. On startup `PluginManager` discovers `Plugins/Source/MyPlugin/`, gathers
   every `*.cs` in the folder, and compiles it **in memory** with the Roslyn compiler
   (`ExileCore.Shared.RoslynCompiler`). No DLL is written to disk on this path. If compilation
   fails, the errors are written to `Errors.txt` in the plugin folder (and that folder is then
   skipped on the next start until you delete the file).

   Alternatively, compile to a DLL on disk with the console command `compile_MyPlugin` (or
   `compile_plugins` for all source plugins) — this path **requires** the `*.csproj` to be
   present. The compiled DLL lands in `Plugins/Compiled/MyPlugin/`. Compiled plugins take
   priority over a same-named source plugin. Full details:
   [`docs/plugin-compiler.md`](../../docs/plugin-compiler.md).

4. Enable the plugin in the in-game settings menu and hold **F5** (the default
   `HighlightKey`) to draw markers over nearby monsters.

## Relevant API docs

- [`docs/api/plugins.md`](../../docs/api/plugins.md) — `IPlugin` / `BaseSettingsPlugin<T>`
  lifecycle and call order.
- [`docs/api/settings.md`](../../docs/api/settings.md) — `ISettings`, the node types and the
  `[Menu]` attribute.
- [`docs/api/graphics.md`](../../docs/api/graphics.md) — `Graphics.DrawText` / `DrawFrame`
  overloads.
- [`docs/api/coordinates.md`](../../docs/api/coordinates.md) — world/screen spaces and
  `Camera.WorldToScreen`.
- [`docs/api/entities.md`](../../docs/api/entities.md) — `Entity`, `EntityListWrapper`,
  `EntityType`.
- [`docs/api/input.md`](../../docs/api/input.md) — `Input` and `HotkeyNode` usage.
- [`docs/plugin-compiler.md`](../../docs/plugin-compiler.md) — Roslyn-based source-plugin
  compilation.

## API members exercised (all verified against `master`)

Lifecycle / base class — `Core/BaseSettingsPlugin.cs`, `Core/Shared/Interfaces/IPlugin.cs`:

- `ExileCore.BaseSettingsPlugin<TSettings>` with virtual `bool Initialise()`,
  `void AreaChange(AreaInstance)`, `Job Tick()`, `void Render()`, `void EntityAdded(Entity)`.
- Injected properties `GameController GameController`, `Graphics Graphics`,
  `TSettings Settings`, and `string Name`.

Settings nodes / attributes — `Core/Shared/Nodes/*`, `Core/Shared/Attributes/MenuAttribute.cs`,
`Core/Shared/Interfaces/IPlugin.cs`:

- `ExileCore.Shared.Interfaces.ISettings` (requires `ToggleNode Enable { get; set; }`).
- `ToggleNode(bool)`, `RangeNode<T>(T value, T min, T max)`, `ColorNode(Color)`,
  `HotkeyNode(Keys)`, `TextNode(string)` — all constructors confirmed in the node sources.
- `[Menu(string)]` / `[Menu(string, string tooltip)]` from
  `ExileCore.Shared.Attributes.MenuAttribute`.

Game state / entities — `Core/GameController.cs`, `Core/EntityListWrapper.cs`,
`Core/PoEMemory/MemoryObjects/Entity.cs`, `Core/Shared/Enums/EntityType.cs`:

- `GameController.Player` (an `Entity`), `GameController.IngameState.Camera`,
  `GameController.EntityListWrapper.ValidEntitiesByType` (a
  `Dictionary<EntityType, List<Entity>>`, populated for every enum value).
- `Entity.IsAlive`, `Entity.DistancePlayer`, `Entity.Pos` (SharpDX `Vector3`),
  `Entity.RenderName`; `EntityType.Monster`.

Rendering / coordinates / input — `Core/Graphics.cs`,
`Core/PoEMemory/MemoryObjects/Camera.cs`, `Core/Input.cs`:

- `Camera.WorldToScreen(Vector3)` → SharpDX `Vector2` (returns `Vector2.Zero` on failure).
- `Graphics.DrawText(string, Vector2, Color)` and
  `Graphics.DrawFrame(Vector2 p1, Vector2 p2, Color color, int thickness)`.
- `Input.GetKeyState(Keys)` (static).

> **Type note.** This fork's `Camera`, `Entity` and the `Graphics` draw overloads used here
> all use **SharpDX** `Vector2`/`Vector3`/`Color`, so the template stays in SharpDX types and
> passes the `WorldToScreen` result straight into `Graphics` with no conversion. Newer plugin
> code sometimes uses `System.Numerics` vectors (aliased `Vector2N` inside the engine) and
> converts at the boundary; see [`docs/api/coordinates.md`](../../docs/api/coordinates.md).

## Differences from the upstream template

- Targets `net10.0-windows` (not `net8.0-windows`) to match this fork.
- Does **not** use `ConfigDirectory` — that member does not exist on `BaseSettingsPlugin` in
  this fork. Use `DirectoryFullName` / `DirectoryName` if you need the plugin's folder.
- Drops the `ImGui.NET` package reference from the csproj (the template does no direct ImGui
  calls; the host supplies ImGui at runtime).
