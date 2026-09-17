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

That pointer leads to a struct which has now been re-measured in full. `CameraOffsets` was wrong in
every field: `Width` was declared at `0x4`, where the object holds a zero, so `Camera.HalfWidth` was
0 and `WorldToScreen` returned an identically-zero X for every point in the world - silently, with
no exception and no log line. The measured values are `Width 0x318`, `Height 0x31C`,
`MatrixBytes 0x1A8`, `Position 0x2E8`, `ZNear 0x308` (new) and `ZFar 0x30C`, and every one of them
was FOUND by a criterion rather than confirmed against a guess: the viewport against the OS's own
`GetClientRect`, the matrix against the projection of the player landing on the horizontal centre of
the screen, the camera position by inverting that matrix and then searching memory for the result,
and the two clip planes by deriving them from the same matrix. Details, including two offsets that
remain ambiguous against a same-valued twin, are in [api/camera-measured.md](api/camera-measured.md);
the tool is `tools/CamCap`.

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

## The entity layer

Measured against the live client on 2026-09-16, one zone, 132–142 entities, 40–50 of them taken
apart. Every raw number, the criterion it had to pass, and the count it passed on are in
[api/entities-measured.md](api/entities-measured.md); what follows is only what the structs look
like as a result.

```csharp
EntityOffsets:        0x08 EntityDetails*   0x10 components std::vector (First/Last/End)
                      0x78 IngameData*      0x88 Id    0x8C Flags    0x98 Positioned*
PathEntityOffsets:    details + 0x08 Path   details + 0x18 Length
ComponentLookup:      details + 0x28 -> lookup;  lookup + 0x40 -> std::vector of 8-byte slots
                      component = [entity.ComponentsArray.First + slot.Index * 8]
```

The path pair is the one that carries a real criterion: the number at `details + 0x18` has to equal
the **actual length** of the string at `details + 0x08`, and that string has to start with
`Metadata/`. It did for **141 entities out of 141** in the zone. The component vector is confirmed
the same way round: `(Last - First) / 8` equals the number of non-empty slots in that metadata's
lookup table, entity by entity (4=4, 9=9, 13=13, 14=14), and every component the table resolves
points back at its own entity through `[component + 0x08]`.

`Id` and `Flags` share one qword - `0x0000260C_000004CE` on one entity, `Id` in the low half and
`Flags` in the high one - so a single 8-byte read gets both. `Flags` is measured **one byte wide and
no wider**: its low byte is `0x0C` on all forty entities taken apart, while the bytes above it
differ with **no rule established**, so reading the field wider means importing an unmeasured
meaning into a measured number. An earlier reading of those high bytes - `0x00` on doodads, `0x26`
on monsters, `0x22` on chests - was **refuted** by a later pass the same day: monsters produce both
(`Metadata/Monsters/ReliquaryMonsterEmerge` reads `0x0000220C`) and chests produce both `0x0000220C`
and `0xFFFF220C`. They do not sort by kind of entity, and nothing has been measured about what they
do sort by.

Two things this measurement **refuted**, and both of them were load-bearing:

- **The slot's `nameId` is not a component type id.** It is ambiguous in both directions: one type
  gets different ids - five names are now known to do so (`BaseEvents` `0x114`/`0x214`,
  `InteractionAction` `0x12E`/`0x22E`, `StateMachine` `0x156`/`0x256`, and from the combat pass
  `Positioned` `0x11C`/`0x21C` and `Life` `0x15F`/`0x25F`), every one of them exactly `0x100` apart -
  and one id covers different types (`0x1D8` is `Chest` on chests and `WorldItem` on world items).
  A key like that fails in **two** ways, both silent: it hands back the wrong component, and on the
  upper form of an id it finds nothing at all - which on `Positioned` and `Life` means no position
  and no health on part of the entities. What works instead is the component's **own vtable**,
  `[component + 0x00]`, taken as an RVA against the module base `0x7FF78FCD0000`: it separates
  exactly the pair the id conflates - `0x3465EC0` (`Chest`) was seen only on `Metadata/Chests/*`,
  seven entities, and `0x3465DA8` (`WorldItem`) only on `Metadata/MiscellaneousObjects/WorldItem`.
  **Forty measured `type -> RVA` pairs** are tabulated in the measurement document - 30 on zone
  entities, 9 item-side, plus `Projectile` on weaker evidence - joined **by component address**
  across four passes of one day; every name has exactly one RVA, and no pair moved between passes.
  A model built on vtables does not need the slot table at all - walking the component
  vector is enough - and it was cross-checked against the direct `Positioned` pointer at entity
  `+0x98`: the two agreed **50 times out of 50**.
- **`InventoryId` is not at `+0x70`.** The survey agreed with the reference on three entities;
  on forty it breaks - on `Metadata/Chests/DarkPot2v2` that qword is a pointer into the game module
  (`0x00007FF792EFFF00`), and reading it as a `uint32` just returns the pointer's low half. The
  field is **unmeasured**, not merely unconfirmed.

Fifteen of the forty pairs are new against the twenty-five that went in first: `Brackets`,
`DiesAfterTime`, `Functions`, `NPC` and `Portal` on zone entities, the nine item-side components,
and `Projectile`. `MinimapIcon` (from the earlier round) is the one that paid most: in
`Entity.ParseType` a whole branch of the classification sits behind `HasComponent<MinimapIcon>()`,
so while that vtable was missing, `EntityType.AreaTransition`, `Waypoint` and the `IngameIcon`
fallback could not be reached at all; `Portal` and `NPC` now make `TownPortal`, `Portal` and `Npc`
reachable the same way. What these pairs do **not** license is reading their fields: only the vtable
was measured, and the `[FieldOffset]`s inside those structs are still carried over from the
reference.

Three qualifications belong with that table, and all three are in the measurement document:
`Projectile` (`0x35A0D10`) rests on a `nameId` from another run plus a path correlation, not on the
address join the other thirty-nine use; `0x35DFB80` is **left out** because the oracle names it
`AttributeRequirements` on armour and weapons and `Usable` on currency, and one vtable cannot be two
types; and `0x35A5C68` (`nameId 0x11E`, on `Metadata/Effects/Effect`) has no name at all. Item-side
components live on a **second entity** whose own vtable is `0x35E0358`, reached through
`WorldItem + 0x28` - measured on 14 dropped items out of 14, and corroborated by the `0xD0` stride
between neighbouring `WorldItem` components, which makes the same field reappear at `+0xF8` and
`+0x1C8`. That number was already in `Core/PoEMemory/Components/WorldItem.cs`, inherited; the
measurement gives it provenance, not a new value.

The hot-path components were confirmed by content:

```csharp
Positioned:  0x294 / 0x298 GridX / GridY (int32)      0x2B8 WorldPosition (3 float)
Render:      0x120 Pos (3 float)   0x12C Bounds (3 float)
             0x148 Name (embedded NativeUtf16Text; length 0x158, capacity 0x160)
Life:        0x180 head / 0x1A4 max / 0x1A8 cur   (health)
             0x1D0 head / 0x1F4 max / 0x1F8 cur   (mana)
             0x218 head / 0x23C max / 0x240 cur   (energy shield)
```

`GridX`/`GridY` are **int32, not float**, and that distinction is the whole measurement: the ratio
`world / grid` only converges on 10.87 when the grid pair is read as integers. `Life` is three
blocks whose head qword points **at the component itself**, which is how a block is recognised, and
the values they produced are the strongest evidence here because they agree with something known
outside memory: the character has Chaos Inoculation, and the blocks read HP 1/1, mana 1135/67,
ES 10353/10353. On the mana pair, read the number and stop there: a later read in the **same
session** gave 1135/1135, and what made the first reading low - spending, regeneration, a reserved
pool - is not measured and is not decidable from this struct. Any account of it is a hypothesis, and
none is needed: the reservation offsets are unknown either way, which is why `Life` reports `null`
reservations and a `NaN` `MPPercentage`.

`Positioned.Reaction` stays where it was, at the inherited `0x58` with `ReactionMeasured = false`,
and that is a decision rather than an omission. A later pass the same day **narrowed** it to three
candidate bytes inside one qword - `+0x1E0` and `+0x1E2` behave like a hostility flag (player 1,
pet 1, all thirteen monsters 0, which is also what the reference reports: 1 for the player, 0 for a
monster), while `+0x1E3` behaves like a faction number (player 1, pet 1, monsters 2, chests and
items 3, Terrain 3, `AreaTransition` and the waypoint 5, effects 255, doodads 1). Nothing in the
sample tells them apart, because the one case that would - an **allied monster** - did not exist in
that zone and did not turn up in any of the three later passes either, and the reference's own
`PositionedComponentOffsets` is documented as not fitting this
build, so its agreement would prove nothing. A narrowing is not a measurement; the candidates are
recorded in [api/entities-measured.md](api/entities-measured.md) and not applied.

One number outside the entity layer came out of the same pass and belongs here for lack of a better
home: `ServerData.Latency` is at `+0xC490` from that object's own `Address`. The reference reported
a latency of 3 at that moment and the qword there was `0x000004AA00000003`, whose low `uint32` is
exactly 3; the number it replaced, `0x6CA0`, reads `0x128CBA26` there, which is not a latency under
any interpretation.

What this layer does **not** have is repetition. `IngameDataOffsets` above was re-measured in four
zones and after a full client restart; every number in this section comes from **a single zone of a
single launch**, and the vtable RVAs are tied to that build by construction. Treat them as measured
and unrepeated.

## Updating offsets after a patch

Because the structs are just a layout map, they can be edited and re-applied without a full
rebuild: the `offset` / `offsets` / `loader_offsets` commands recompile the `GameOffsets/`
folder into `GameOffsets.dll` at runtime with Roslyn and load it into the process (see
[plugin-compiler.md](plugin-compiler.md#recompiling-gameoffsets)). After a game patch, the
typical loop is: correct the `[FieldOffset]` values against a current reference, then trigger
a recompile.

## Notes

- Windows-only and tied to a live PoE process; offsets are meaningless without the game.
- This document describes the structs as they currently appear in the repository. Some of them
  **have** been verified against a live build and say so with a count and a criterion -
  `IngameDataOffsets` and `TerrainData` (see [api/ingamedata-measured.md](api/ingamedata-measured.md)),
  the entity layer and the hot-path components (see
  [api/entities-measured.md](api/entities-measured.md)), and the camera (see
  [api/camera-measured.md](api/camera-measured.md)). Everything not named in those documents
  is still carried over from the reference distribution and unverified here; the structs do not mark
  the difference, only their XML comments and these documents do.
