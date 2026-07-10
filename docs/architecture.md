# Architecture overview

This is a Windows-only HUD/API for Path of Exile. It runs as a separate process,
reads the game's memory read-only, interprets that memory as structured objects, and
draws an overlay on top of the game window with DirectX 11. Plugins consume the parsed
data and render whatever they want.

> Windows-only. The whole project targets `net10.0-windows`, uses Windows Forms for its
> host window and DirectX 11 for rendering, and reads a live PoE process. Nothing here
> works off Windows or without the game running.

## Solution layout

`ExileApi.sln` contains three core projects (plugins live under the git-ignored
`Plugins/Source/` and are loaded/compiled at runtime, not via the solution — see section 7):

| Project | Assembly | Role |
| --- | --- | --- |
| `GameOffsets` | `GameOffsets.dll` | Plain structs that map raw PoE memory (see [offsets.md](offsets.md)). No game logic. |
| `Core` | `ExileCore.dll` | The engine: memory reader, cache layer, memory-object model, rendering, plugin host. Root namespace `ExileCore`. |
| `Loader` | `Loader.exe` | Entry point. Hosts the Windows Forms / DX11 window and starts the engine. |

All three are SDK-style projects targeting `net10.0-windows`, x64, and build their output
into `..\PoeHelper\` (see [the README](../README.md) for the exact build commands).

## How the pieces fit together

```
PoE process memory
        │  (ReadProcessMemory, read-only)
        ▼
   Memory  ──────────────►  GameOffsets structs        (raw bytes → typed structs)
        │                        │
        ▼                        ▼
 RemoteMemoryObject  ◄──── pointers / FieldOffsets      (typed objects over addresses)
        │
        ├── Entity ──► Component(s)  (Life, Positioned, Render, ...)
        ├── IngameState / IngameData / IngameUi (elements, terrain, map stats, ...)
        └── FilesInMemory (static game data tables)
        │
        ▼
   Cache layer  (FrameCache / AreaCache / TimeCache / ...)
        │
        ▼
   Plugins  ──► Graphics (DX11 + ImGui overlay)
```

### 1. Memory reading

`Core/Memory.cs` (implementing `Shared/Interfaces/IMemory.cs`) opens the PoE process with
`VirtualMemoryRead` access via `ProcessMemoryUtilities` and exposes typed reads:

- `T Read<T>(long addr) where T : struct` copies a block of process memory straight onto a
  managed struct. This is how `GameOffsets` structs get populated — the struct's
  `[FieldOffset]` layout has to match the game's in-memory layout exactly.
- `ReadStructsArray<T>` / `ReadMem` / `ReadString*` cover arrays, raw buffers and the
  game's native string formats.

Base addresses are located once at startup by pattern scanning (`PoEMemory/Pattern.cs`,
`Offsets.DoPatternScans`).

### 2. Memory objects

`Core/PoEMemory/RemoteMemoryObject.cs` is the base for everything that lives at an address
in the game. A `RemoteMemoryObject` holds an `Address` and a shared `IMemory`, and offers
helpers (`GetObject<T>`, `ReadObject<T>`, `GetObjectAt<T>`) to follow pointers and
materialize other memory objects lazily. Concrete types live under
`PoEMemory/MemoryObjects/` (e.g. `IngameState`, `IngameData`, `Entity`, `EntityList`,
`ServerData`) and `PoEMemory/Elements/` (UI elements).

### 3. Components

`Core/PoEMemory/Component.cs` is a `RemoteMemoryObject` representing one ECS-style
component attached to an `Entity`. The concrete components live in
`PoEMemory/Components/` (e.g. `Life`, `Positioned`, `Render`, `Mods`, `Buff`). An `Entity`
caches its components (`Entity.GetComponent<T>()` / `HasComponent<T>()`), so a plugin asks
an entity for, say, its `Life` or `Positioned` component and reads health or grid position
from there.

### 4. Static game data

`PoEMemory/FilesInMemory/` parses the game's data tables (areas, mods, base item types,
monster varieties, etc.) so plugins can resolve ids and metadata that aren't carried on
the live entities themselves.

### 5. Caching

Repeated reads are expensive, so `Shared/Cache/` wraps values in caches keyed by lifetime:
`FrameCache` (one frame), `AreaCache` (until the area changes), `TimeCache`/`LatancyCache`
(time-boxed), etc. `IngameData`, `Entity` and friends use these so a value is read from the
process at most once per its cache window.

### 6. Rendering and the host window

`Loader/Program.cs` creates a `Loader`, which builds an `AppForm` (a SharpDX `RenderForm`)
and drives the engine in `Core/Core.cs`. Rendering goes through `Core/Graphics.cs`, which
exposes `DrawText`, `DrawLine`, `DrawBox`, `DrawImage`, etc., backed by the DX11 renderer
in `Core/RenderQ/` (`DX11.cs`) and an ImGui overlay (`ImGuiRender.cs`, using
`ImGui.NET.dll` + `cimgui.dll` from `deps/`). The overlay is drawn on top of the PoE window
each frame.

### 7. Plugins

`Core/Shared/PluginManager.cs` discovers, loads and initializes plugins, wires them to game
events (entity added/removed, area change) and gives them the `GameController` + `Graphics`
API. Plugins ship either as pre-compiled DLLs in `Plugins/Compiled/<name>/` or as source in
`Plugins/Source/<name>/` that is compiled at runtime with Roslyn. See
[plugin-compiler.md](plugin-compiler.md) for the compilation path.

## Notes / caveats

- Memory reads are read-only; the engine never writes to the game process.
- `GameOffsets` are tied to a specific PoE build and must be re-checked after game patches
  (see [offsets.md](offsets.md)).
- This document describes the code as it currently stands in the repository. It has not been
  built or run in this environment (no .NET toolchain / no Windows / no live game here).
