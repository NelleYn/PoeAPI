# GameOffsets

The `GameOffsets` project (`GameOffsets.dll`) is a collection of plain C# structs that
mirror the in-memory layout of Path of Exile's own data structures. It contains no game
logic — just the field-by-field map the engine uses to interpret raw bytes it reads out of
the live game process.

> **Build-specific.** These offsets describe a *particular* PoE build. Game patches move
> fields around, so after a patch the structs here can be wrong and must be re-verified
> against an up-to-date reference. None of this can be checked in this environment (no
> Windows, no running game).

## How a struct maps memory

Each offset struct uses explicit layout so its fields land at exact byte positions:

```csharp
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct IngameDataOffsets
{
    [FieldOffset(0x60)] public long CurrentArea;
    [FieldOffset(0x78)] public byte CurrentAreaLevel;
    [FieldOffset(0xE0)] public NativePtrArray MapStats;
    [FieldOffset(0xDC)] public uint CurrentAreaHash;
    [FieldOffset(0x408)] public long LocalPlayer;
    [FieldOffset(0x11C)] public long LabDataPtr;
    [FieldOffset(0x490)] public long EntityList;
    [FieldOffset(0x498)] public long EntitiesCount;
    [FieldOffset(0x748)] public TerrainData Terrain;
}
```

- `[StructLayout(LayoutKind.Explicit, Pack = 1)]` means the runtime does **not** choose
  field positions; each `[FieldOffset(...)]` is the exact byte offset within the game's
  structure. `Pack = 1` removes any added padding.
- The engine reads one of these structs with `M.Read<T>(address)` (see
  [architecture.md](architecture.md)), which copies the bytes at `address` directly onto the
  managed struct. Because the layout is explicit, the fields then line up with the game's
  data — *provided the offsets are correct for the running build.*
- Pointer-like fields (`long`) hold addresses that the engine follows to build further
  memory objects. Helper native types live under `GameOffsets/Native/` (e.g.
  `NativePtrArray`, which models a `First`/`Last`/`End` native vector, and `Vector2i`).

## TerrainData and the walkable grid

`GameOffsets/TerrainData.cs` describes the walkable-terrain grid for the current area,
which is embedded inside `IngameDataOffsets`:

```csharp
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TerrainData
{
    [FieldOffset(0x08)] public NativePtrArray LayerMelee;
    [FieldOffset(0x20)] public NativePtrArray LayerRanged;
    [FieldOffset(0x38)] public int NumCols;
    [FieldOffset(0x3C)] public int NumRows;
    [FieldOffset(0x40)] public int BytesPerRow;
}
```

- `LayerMelee` and `LayerRanged` are native `std::vector<byte>` buffers (modeled as
  `NativePtrArray` with `First`/`Last`/`End`). Each byte packs two 4-bit cells, and
  `BytesPerRow` is the row stride in bytes; `NumCols` / `NumRows` give the grid dimensions.
- The per-field layout above (the relative offsets `0x08`, `0x20`, `0x38`, `0x3C`, `0x40`)
  is noted in the source as stable across the ExileApi lineage.
- **However**, the *position of `TerrainData` inside `IngameDataOffsets`* —
  `[FieldOffset(0x748)] public TerrainData Terrain` — is game-build specific. The source for
  both `IngameDataOffsets.Terrain` and `TerrainData` carries an explicit note to verify that
  position against your ExileApi-Compiled reference for the PoE patch you run. Treat `0x748`
  as a value to confirm, not a guarantee.

### Reaching it from the engine

`Core/PoEMemory/MemoryObjects/IngameData.cs` reads the whole `IngameDataOffsets` struct (via
an `AreaCache`) and exposes the terrain through a simple property:

```csharp
public TerrainData Terrain => _cacheStruct.Value.Terrain;
```

So consumers get the walkable grid as `gameController.IngameState.Data.Terrain`, reading the
layer vectors and `NumCols` / `NumRows` / `BytesPerRow` from there.

## Updating offsets after a patch

Because the structs are just a layout map, they can be edited and re-applied without a full
rebuild: the `offset` / `offsets` / `loader_offsets` commands recompile the `GameOffsets/`
folder into `GameOffsets.dll` at runtime with Roslyn and load it into the process (see
[plugin-compiler.md](plugin-compiler.md#recompiling-gameoffsets)). After a game patch, the
typical loop is: correct the `[FieldOffset]` values against a current reference, then trigger
a recompile.

## Notes

- Windows-only and tied to a live PoE process; offsets are meaningless without the game.
- This document describes the structs as they currently appear in the repository and has not
  been verified against any specific live game build here.
