# Plugin compilation (Roslyn)

Plugins can be shipped as source and compiled at runtime. Since the .NET 10 port, that
compilation is done with the **Roslyn** APIs (`Microsoft.CodeAnalysis.CSharp`), not the
legacy CodeDom provider. The single entry point is
`ExileCore.Shared.RoslynCompiler` in `Core/Shared/RoslynCompiler.cs`; the rest of this
document describes how the engine drives it.

## `RoslynCompiler.Compile`

```csharp
RoslynCompiler.Compile(string assemblyName,
                       IEnumerable<string> sourceFiles,
                       IEnumerable<string> referencePaths) -> CompileResult
```

What it does, exactly as written:

1. Parses each source file with `CSharpSyntaxTree.ParseText`, using
   `CSharpParseOptions(LanguageVersion.Latest)` and passing the file path so diagnostics
   point at the right file.
2. Builds a `CSharpCompilation` for the given assembly name with:
   - `OutputKind.DynamicallyLinkedLibrary`
   - `OptimizationLevel.Release`
   - `allowUnsafe: true`
   - `platform: Platform.X64`
3. Emits the assembly and its PDB into in-memory streams (`compilation.Emit`).
4. On failure, returns a `CompileResult` whose `Errors` is the list of `Error`-severity
   diagnostics (as strings) and whose `Dll` is null.
5. On success, returns a `CompileResult` with the emitted `Dll` and `Pdb` byte arrays.

`CompileResult.Success` is `true` only when there are no errors **and** `Dll` is non-null.

### How references are gathered (`BuildReferences`)

References are collected into a dictionary keyed by file name (case-insensitive), so each
assembly is added at most once:

1. First, every path in the runtime's trusted platform assembly list —
   `AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")`, split on `Path.PathSeparator` — is
   added. This is what pulls in the BCL / framework references for the running .NET 10
   runtime.
2. Then every caller-supplied `referencePaths` entry is added.

`Add` skips blank paths, skips duplicate file names, skips files that don't exist, and
swallows any exception from `MetadataReference.CreateFromFile` (a bad reference is ignored
rather than aborting the compile). The result is the de-duplicated list of metadata
references.

## Two ways plugins get compiled

### 1. Hot-compile from source at startup (`PluginManager`)

`Core/Shared/PluginManager.cs` owns the startup path. On construction it sets up the
`Plugins/`, `Plugins/Compiled/`, `Plugins/Source/` and `Plugins/Temp/` directories (creating
any that are missing) and then:

- `SearchPlugins()` lists the compiled plugin directories and the source plugin
  directories. Source directories that are hidden, or that already contain an `Errors.txt`
  from a previous failed compile, are skipped. By default compiled plugins take priority:
  a source directory whose name also exists under `Compiled/` is excluded
  (`ExceptBy(... info.Name)`).
- If there are any source plugins, they are compiled on a background `Task` while the
  already-compiled plugins are loaded in parallel.

For each source plugin, `CompilePlugin(info, dllFiles)`:

1. Collects every `*.cs` under the plugin directory (recursively).
2. Starts the reference list from `dllFiles` — the `*.dll` files in the engine's root
   (output) directory. That root scan (in `CompilePluginsFromSource`) excludes `cimgui.dll`
   and any file whose name contains exactly five `-`/`_` characters (a heuristic that skips
   native runtime libraries named like `api-ms-win-...`).
3. If the plugin has a `libs/` subfolder, adds every `*.dll` from it as an extra reference.
4. Calls `RoslynCompiler.Compile(info.Name, csFiles, references)`.
5. On failure, logs each error and writes them to `Errors.txt` in the plugin folder (which
   is what makes `SearchPlugins` skip the plugin next time), then returns null.
6. On success, loads the assembly in-process with `Assembly.Load(result.Dll, result.Pdb)`
   (or `Assembly.Load(result.Dll)` when there is no PDB) — the bytes are loaded directly, no
   DLL is written to disk in this path.

A loaded assembly is then handed to `TryLoadPlugin`, which reflects over its types, requires
an `ISettings` implementation and instantiates each concrete `IPlugin`, wrapping it in a
`PluginWrapper` and registering it.

There is also a file-watch hot-reload path (`HotReloadDll`): when a compiled plugin DLL
changes on disk, the manager reloads that assembly and swaps the plugin instance (guarded
against double-firing within ~2 seconds).

### 2. Compile source to a DLL on disk (`CommandExecutor`)

`Core/CommandExecutor.cs` exposes console-style commands that compile to files on disk:

- `compile_plugins` — `CompilePluginsIntoDll()` compiles every non-hidden directory under
  `Plugins/Source/` in parallel.
- `compile_<name>` — compiles just the matching directory under `Plugins/Source/`.
- `offset` / `offsets` / `loader_offsets` — recompile `GameOffsets` (see below).

The actual work is `CompileSourceIntoDll(info)`:

1. Gathers all `*.cs` under the plugin directory and requires a `*.csproj` to exist (if not,
   it shows an error message box and stops).
2. Computes the output directory by swapping `\Source\` for `\Compiled\` in the path and
   creates it if needed.
3. Builds the reference list from `GetAllDllFilesFromRootDirectory()` — the same root `*.dll`
   scan and exclusions as above (cached in `_dllFiles`) — plus any `libs/*.dll`.
4. Calls `RoslynCompiler.Compile(info.Name, csFiles, references)`.
5. On failure, writes `Errors.txt` and shows an error message box.
6. On success, writes `<name>.dll` (and `<name>.pdb` when present) into the `Compiled/`
   directory and shows a success message box.

### Recompiling `GameOffsets`

`CreateOffsets` (triggered by `offset` / `offsets` / `loader_offsets`) recompiles the
`GameOffsets/` source folder into `GameOffsets.dll` using the same `RoslynCompiler.Compile`
plus the root-directory references. Unless `force` is set, it only recompiles when at least
one `GameOffsets/**/*.cs` file is newer than the existing `GameOffsets.dll`. On success it
writes the DLL and loads it into the current process. This lets you edit offsets and have
them re-applied without a full rebuild.

## Notes

- All of the above runs only on Windows against a live game; some failure paths use Windows
  Forms message boxes.
- This document strictly describes the code as written in `RoslynCompiler.cs`,
  `PluginManager.cs` and `CommandExecutor.cs`. It has not been built or executed in this
  environment.
