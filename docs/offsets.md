# GameOffsets

The `GameOffsets` project (`GameOffsets.dll`) is a collection of plain C# structs that
mirror the in-memory layout of Path of Exile's own data structures. It contains no game
logic — just the field-by-field map the engine uses to interpret raw bytes it reads out of
the live game process.

> **Build-specific.** These offsets describe a *particular* PoE build. Game patches move
> fields around, so after a patch the structs here can be wrong and must be re-verified
> against a running client. Verification needs Windows and the game; the tools that do it live
> in `tools/` (`RefLive`, `FindOffset`, `SanityRead`), and each of them refuses to guess when
> the game is not there.

## How a struct maps memory

Each offset struct uses explicit layout so its fields land at exact byte positions:

```csharp
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct IngameDataOffsets
{
    [FieldOffset(0xB0)]  public long CurrentArea;
    [FieldOffset(0xD4)]  public byte CurrentAreaLevel;
    [FieldOffset(0x114)] public uint CurrentAreaHash;
    [FieldOffset(0x128)] public NativePtrArray MapStats;
    [FieldOffset(0x968)] public long ServerData;
    [FieldOffset(0x970)] public long LocalPlayer;
    [FieldOffset(0xA28)] public long EntityList;
    [FieldOffset(0xA30)] public long EntitiesCount;
    [FieldOffset(0xA38)] public long SleepingEntityList;
    [FieldOffset(0xA40)] public long SleepingEntityCount;
    [FieldOffset(0xC08)] public TerrainData Terrain;
    [FieldOffset(0x48)]  public long LabDataPtr;
}
```

Every number above was **measured against the running client**, not carried
over from the reference distribution. The method is the one this repository uses for every
offset, and it is worth stating because it is what makes the numbers claims rather than guesses:

1. the reference distribution reads the same process correctly, so it is asked for the **true
   address** of each sub-object - `tools/RefLive` loads its `ExileCore.dll` as an ordinary
   library and prints what it sees;
2. `tools/FindOffset --find --in <object> --len 0x1000 --value <true address>` reports every
   place inside the object where that address actually lies;
3. the answer counts only if it survives **repetition**: a different zone gives the object a new
   base address and new values in its fields, and the offset has to come out the same.

Uniqueness alone is a weaker argument than it looks, and the source says so per field. A match is
unique only *within the window it was searched*: widen the window from `0x1000` to `0x2000` and the
player pointer turns up **twice** (`0x970` and `0x10E8`); widen it to `0x10000` and a one-byte value
like the area level has seven candidates. Repetition across zones is what actually carries the
weight, and it is not uniform: the area level and hash reproduced in three zones, the pointers and
the terrain block in two, `MapStats` in one (but identified by content), `SleepingEntityCount` and
`EnvironmentData` in one. Every raw number behind those claims, zone by zone, is in
[api/ingamedata-measured.md](api/ingamedata-measured.md); `tools/measure-ingamedata.sh` reproduces
the whole table in one command.

`MapStats` was confirmed the strongest way available here, by **content**: the reference
reported eleven `(stat, value)` pairs, and the array this field points at holds exactly those
eleven pairs, in that order, ending exactly after them. The earlier guess for it - the 48-byte run
of zeroes between `CurrentAreaLevel` and `CurrentAreaHash` - was refuted by that same measurement.

The same method reached one field outside this struct. `Camera` is **not** embedded in
`IngameState`: the address the reference reports for it is lower than `IngameState`'s own, so no
offset from that object could reach it. The pointer to it sits at `IngameState + 0x270`, as the only
match in a `0x10000` window; the number it replaced (`0xF4C`, read as an embedded struct) landed in
a text buffer. `ServerData` had the same shape of error and is described in
`Core/PoEMemory/MemoryObjects/IngameState.cs`.

Note what the method does **not** establish. `tools/RefLive` runs the reference's own code against
the same process, so all of this proves *this fork reads what the reference reads* - not *this is
the layout of the game's own struct*. And `tools/parity.sh` is no help here at all: it counts
**names** on the reference's API surface and never checks a single number.

`LabDataPtr` was measured **without the oracle**, and it is the most instructive number here: the
reference reports `LabyrinthData` as `null` even while the character stands inside the Labyrinth.
The reference is right about the fields its own users exercise, and this is not one of them, so
"ask the reference" is a working assumption rather than a law. It was found by *difference*
instead - the head of the object is a run of zeroes outside the Labyrinth, and inside it exactly
one qword in that run becomes a heap pointer - and confirmed by *content*: the reference's own
parser aimed at that address (`RefLive --as LabyrinthData <addr>`) reads out ten rooms of a
coherent layout, with a `LinkedWith` graph that agrees in both directions, while the same parser
aimed at `ServerData` or at `EntityList` reads zero rooms.

Every offset above was then re-measured after a full client restart, in a fourth zone: `TheGame`
moved from `0x44C0C092E80` to `0x5F4F6093300` and `IngameState` from `0x44C17002C10` to
`0x5F4FC562810`, a different address space entirely, and every number came out the same. ASLR is
covered.

> **The reference's own numbers do not transfer.** Its `IngameDataOffsets` declares
> `LocalPlayer` and `EntityList` 8 bytes apart; on this client they are `0xB8` apart. What
> *does* transfer is the set of fields and their **order**, which makes the reference a
> generator of hypotheses and never a source of values. See
> [api/parity-measured.md](api/parity-measured.md).

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
    [FieldOffset(0x00)] public int NumCols;              // TILES, not cells
    [FieldOffset(0x08)] public int NumRows;              // TILES, not cells
    [FieldOffset(0x10)] public NativePtrArray TgtArray;
    [FieldOffset(0x28)] public int NumTileIndexCols;
    [FieldOffset(0x30)] public int NumTileIndexRows;
    [FieldOffset(0x38)] public NativePtrArray TileIndexes;
    [FieldOffset(0x50)] public NativePtrArray TileDescriptions;
    [FieldOffset(0xB8)] public NativePtrArray LayerMelee;
    [FieldOffset(0xD0)] public NativePtrArray LayerRanged;
    [FieldOffset(0xE8)] public int BytesPerRow;
    [FieldOffset(0xEC)] public int TileHeightMultiplier;
}
```

Offsets are relative to `IngameDataOffsets.Terrain`, which sits at `0xC08` on this build.

- `LayerMelee` and `LayerRanged` are native `std::vector<byte>` buffers (modeled as
  `NativePtrArray` with `First`/`Last`/`End`). Each byte packs two 4-bit cells, and
  `BytesPerRow` is the row stride in bytes.
- The layout above is **not** the reference's, and that is the point. There `NumCols` and
  `NumRows` are adjacent 4-byte fields and `TileDescriptions` is followed immediately by
  `LayerMelee`; here every scalar is padded to 8 bytes and `0x50` bytes of unidentified data
  sit between the tile descriptions and the layers. The pointer members were located by their
  true addresses; the scalars were then read out of a window dump at the offsets those
  pointers implied, and every one of them agreed with what the reference reported for the same
  area.

### The units trap

`NumCols` and `NumRows` count **tiles**, and one tile is 23 cells on a side. The walkable grid
is `BytesPerRow * 2` cells wide and `LayerMelee.Size / BytesPerRow` cells tall, which equals
`NumRows * 23`.

This is worth spelling out because reading `NumRows` as a cell count fails *silently*. On the
area this was measured in, `NumRows` was 81 while the grid was 1863 rows tall; 81 is a
perfectly plausible-looking row count, and every plausibility check in the codebase would have
passed it. `tools/SanityRead` made exactly that mistake and now derives the height from the
layer instead, printing `height == NumRows * 23` beside it as an independent cross-check: two
different fields agree only when both are read correctly.

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
