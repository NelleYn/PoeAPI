# ExileApi private fork of PoeHud

# Current Status
Basic version with all important(my vision) plugins in release tab.
Current version can lags and crash because i didn't test that a lot time. Maybe on next week found time for test and publish source code.


Dirty copy of my private fork.
Difference with main fork:
* Read memory  like structs (better for CPU, but used more memory)
* New cache system
* New rendering with DX11
* Plugins can compile from source
* A lot diagnostic information for easy found performance problem
* No hooks mouse and keyboard for prevent lag when debug
* All "standard" plugins cut. (Now they should be like another plugins)

# Requirements:
* .NET 10 Desktop Runtime (x64) — https://dotnet.microsoft.com/download/dotnet/10.0

# For developers:

This fork targets **net10.0-windows** with SDK-style projects (`Core`, `GameOffsets`,
`Loader`). All NuGet dependencies are restored via `PackageReference`; there is no
`packages.config` and no Fody. Source plugins are compiled at runtime with the **Roslyn**
APIs (`Microsoft.CodeAnalysis.CSharp`) rather than the legacy CodeDom provider.

This is **Windows-only**: it hosts a Windows Forms / DirectX 11 window and reads a live
Path of Exile process. It does not build or run off Windows or without the game.

Build requirements:
* .NET 10 SDK (x64) on Windows. (End users only need the .NET 10 Desktop Runtime above.)
* SharpDX 4.2 + the other NuGet packages are restored automatically. `ImGui.NET.dll` /
  `cimgui.dll` ship in `deps/`; swap those two binaries if you run a different
  ExileApi-Compiled build that needs a matching version.

Building:
* Clone this repo (e.g. into `HUD\ExileApi`).
* `dotnet build -c Release ExileApi.sln` (or build just `Loader/Loader.csproj`).
* Every project sets `OutputPath` to `..\..\PoeHelper\`, so the output
  (`Loader.exe`, `ExileCore.dll`, `GameOffsets.dll`, `cimgui.dll`, textures, ...) lands in
  `PoeHelper\` next to the repo. Run `Loader.exe` from there with PoE running.

Plugins:
* Compiled plugins go in `Plugins\Compiled\<name>\` (a `<name>*.dll` per folder); source
  plugins go in `Plugins\Source\<name>\`. At startup `PluginManager` loads the compiled
  ones and compiles any source plugins in memory with Roslyn (compiled take priority over a
  same-named source folder).
* You can also compile a source plugin to a DLL on disk at runtime via the
  `compile_<name>` / `compile_plugins` commands. See
  [docs/plugin-compiler.md](docs/plugin-compiler.md) for the full reference-gathering and
  compilation flow.

Notes:
* Memory offsets (`GameOffsets`) are tied to the current PoE build. `IngameData.Terrain` is
  wired up, but the `TerrainData` position inside `IngameDataOffsets` must be verified against
  your reference for the patch you run (see the note in `GameOffsets/TerrainData.cs` and
  [docs/offsets.md](docs/offsets.md)).

## Developer docs

* [docs/architecture.md](docs/architecture.md) — modules and how memory reading →
  components → memory objects → rendering fit together.
* [docs/plugin-compiler.md](docs/plugin-compiler.md) — how the Roslyn-based plugin (and
  `GameOffsets`) compilation works.
* [docs/offsets.md](docs/offsets.md) — how `GameOffsets` structs map live memory via
  `[FieldOffset]` and expose the walkable terrain grid.

## Troubleshooting

* Download problems:

When download your `7z` from releases maybe this comes with screwed permissions.

> For those who can't launch (Close as soon as it's opened) :
> Right click on the Zip of the HUD (The one you get from the link in the first post) and Right click > Properties > Unlock, then you can unzip it where you want. Otherwise all the files extracted will be security locked...
> Worked for a friend, it's a security from Windows that deny access to files from another PC, once done everything was good for him

* Rendering problems

Big visual offsets in rendering minion dots and everything other:

Windows Display options-> Scale and layout -> set to 100%
