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

Build requirements:
* .NET 10 SDK (x64) and a Windows machine with ExileApi assets.
* SharpDX 4.2 (NuGet) + `ImGui.NET.dll` / `cimgui.dll` (shipped in `deps/`). If you run a
  different ExileApi-Compiled build, swap those two binaries for the matching version.

Compilation:
* Create a HUD folder, clone this repo into it (e.g. `HUD\ExileApi`).
* `dotnet build -c Release ExileApi.sln` (or build `Loader/Loader.csproj`).
* Output (`ExileCore.dll`, `GameOffsets.dll`, `Loader.exe`, `cimgui.dll`, textures) is copied
  to `..\PoeHelper\`. Run `Loader.exe` from there.

Building plugins against this fork:
* Point a consumer plugin's `ExileApiDir` at the `PoeHelper` output, e.g.
  `dotnet build -c Release -p:ExileApiDir=C:\HUD\PoeHelper`. The plugin resolves
  `ExileCore.dll` / `GameOffsets.dll` / `SharpDX*.dll` from there and drops its compiled DLL
  into `PoeHelper\Plugins\Compiled\<name>\`.

Notes:
* Memory offsets (`GameOffsets`) are tied to the current PoE build. `IngameData.Terrain` is
  wired up, but the `TerrainData` position inside `IngameDataOffsets` must be verified against
  your reference for the patch you run (see the note in `GameOffsets/TerrainData.cs`).

## Troubleshooting

* Download problems:

When download your `7z` from releases maybe this comes with screwed permissions.

> For those who can't launch (Close as soon as it's opened) :
> Right click on the Zip of the HUD (The one you get from the link in the first post) and Right click > Properties > Unlock, then you can unzip it where you want. Otherwise all the files extracted will be security locked...
> Worked for a friend, it's a security from Windows that deny access to files from another PC, once done everything was good for him

* Rendering problems

Big visual offsets in rendering minion dots and everything other:

Windows Display options-> Scale and layout -> set to 100%
