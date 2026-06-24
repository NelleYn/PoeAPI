# Types reconstructed from `ExileCore.dll`

This directory's `ExileCore.*` types under `Core/` were extended with **389 types**
reconstructed from a compiled, **net10** `ExileCore.dll` (assembly version 10.0.14.7603)
to close the gap between the modernized source and the full engine surface.

## How it was done

- The DLL's **metadata is intact** (clean type/member signatures, exact enum values),
  so type definitions, fields, properties, method signatures and enums are faithful.
- The DLL is **hardened**: ~17% of method bodies are protected (invalid-at-rest IL,
  restored at runtime via the module initializer). Those bodies are **not present in
  static IL** and cannot be decompiled by any tool.
- Recoverable logic was decompiled with **ICSharpCode.Decompiler (ILSpy master)** built
  from source for full net10 support. Protected/unrecoverable method bodies were replaced
  with explicit stubs.

## What is real vs. stubbed

| Status | Count | Meaning |
|---|---|---|
| Fully recovered | 128 | every member decompiled cleanly |
| Partially stubbed | 236 | structure + most logic real; protected methods stubbed |
| Signature-only | 25 | only signatures recoverable (see note) |

Total protected method bodies stubbed: **610** (search markers below).

Find the work that still needs real implementations:

```
grep -rn "Body protected in source DLL" Core/   # stubbed individual methods
grep -rln "signature-only stub" Core/             # types recovered as signatures only
```

## Signature-only types (need the most attention)

- `ExileCore.ActionOverlay`
- `ExileCore.BackgroundTask`
- `ExileCore.ControllerInput`
- `ExileCore.DelegateCompiler`
- `ExileCore.IInputManager`
- `ExileCore.ImGuiHelpers`
- `ExileCore.Limits`
- `ExileCore.PagedMemoryBackend`
- `ExileCore.PoEMemory.FilesInMemory.GemEffects`
- `ExileCore.PoEMemory.MemoryObjects.EnvironmentSettingValue`
- `ExileCore.PoEMemory.MemoryObjects.TypedEnvironmentData`
- `ExileCore.PoEMemory.StructuredRemoteMemoryObject`
- `ExileCore.ProcessPicker`
- `ExileCore.Shared.NextFrameTask`
- `ExileCore.Shared.Nodes.ContentNode`
- `ExileCore.Shared.Nodes.ContentNodeConverter`
- `ExileCore.Shared.Nodes.HotkeyNodeV2`
- `ExileCore.Shared.PluginAssemblyLoadContext`
- `ExileCore.Shared.PluginCompiler`
- `ExileCore.Shared.SyncAwaiter`
- `ExileCore.Shared.SyncTaskMethodBuilder`
- `ExileCore.Shared.TaskUtils`
- `ExileCore.Shared.WaitFunctionTimed`
- `ExileCore.SnapshotBuilder`
- `ExileCore.StatCollector`

## Nested types restored as `partial` extensions

20 nested types were missing from existing parents; they were added as `partial`
extension files (e.g. `CachedValue.CacheUpdateEvent.cs`) and the corresponding parent
declarations were marked `partial`.

## Caveats

- This project targets **net10.0-windows** (WinForms/DX11, live PoE process); it cannot
  be built or run off Windows, so these files were validated for **syntax only** (Roslyn,
  0 errors across `Core/`), not compiled.
- Stubbed methods throw `NotImplementedException`; they must be reimplemented for runtime use.
- A few constructors/initializers may be incomplete where the decompiler could not fully
  reconstruct them.

## Known external-reference gaps

The DLL was built against a slightly different dependency set than this fork. Three
reconstructed files reference packages not in `Core.csproj` and will need either a
package reference or adaptation to this fork's equivalents:

- `Core/Shared/PluginCompiler.cs`, `Core/Shared/MsBuildLogger.cs` — reference
  `Microsoft.Build.*` (upstream compiled plugins via MSBuild; this fork uses Roslyn,
  see `RoslynCompiler`). These two are effectively superseded by this fork's design.
- `Core/ImguiVariadic.cs` — references `SharpGen.Runtime`.

Everything else resolves against packages already declared in `Core.csproj`
(SharpDX, ImGui.NET, GameOffsets, Newtonsoft.Json, Serilog, morelinq, …).
