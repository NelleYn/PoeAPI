# Compat helpers (candidate)

> **EXPERIMENTAL — not compiled in this environment.** This directory lives under the
> top-level `proposals/` tree, which is **outside every `.csproj`** in the solution, so nothing
> here is part of the build and it cannot break compilation. The project is Windows + game-only
> and cannot be compiled or run in this environment; treat these files as **ready-to-move-in
> candidates**, not verified binaries.

Additive C# helpers (extension methods / static helpers) that fill the highest-traffic API gaps
between this fork and the **ExileApi-Compiled** distribution that most reference plugins target.
They are written so that **no existing file is modified** — every helper is an extension method
or static method over a type that already exists in the fork. Each helper is grounded in a
**real fork member** (verified via `grep`/`Read` of `master` `Core/`, cited below as `file:line`)
and emulates a concrete upstream member documented in
[`docs/api/compatibility-exileapi-compiled.md`](../../docs/api/compatibility-exileapi-compiled.md).

## How to integrate

1. Move the `.cs` files (not this README) into `Core/Shared/Compat/`.
2. Change the file-scoped namespace in each from `namespace ExileCore.Shared.Compat;` to
   `namespace ExileCore.Shared;` (so callers get the helpers via the existing
   `using ExileCore.Shared;` they already have), **or** keep `ExileCore.Shared.Compat` and add a
   `using` where needed. Either compiles; `ExileCore.Shared` is the lower-friction choice.
3. Build `Core` (Windows + game required). These compile against existing public members only.
4. Delete the leading `// EXPERIMENTAL candidate …` header comment once promoted.

The Numerics helpers deliberately use `System.Numerics` for the `*Num` accessors and SharpDX for
the underlying members, exactly as the fork does (`Core/Shared/Helpers/Extensions.cs` aliases
`Vector2`/`Vector4` to `System.Numerics`).

---

## `NumericsCompat.cs`

`System.Numerics` accessors emulating upstream `PosNum` / `GridPosNum` / `WorldPosNum` /
`BoundsNum` / `RotationNum`. The fork stores these as SharpDX vectors; the helpers convert at the
boundary.

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `SharpDX.Vector3.ToVector3Num()` | (converter) | inline — fork has no Vector3 converter | `Core/Shared/Helpers/Extensions.cs:129,139` ship only `ToVector4Num`/`ToVector2Num`. |
| `Entity.PosNum()` | `Entity.PosNum : System.Numerics.Vector3` | `Entity.Pos` — `Core/PoEMemory/MemoryObjects/Entity.cs:121` | Compat doc, "Entity & EntityListWrapper". |
| `Entity.GridPosNum()` | `Entity.GridPosNum : Vector2` | `Entity.GridPos` — `Entity.cs:157` | Compat doc, "Entity & EntityListWrapper". |
| `Entity.BoundsNum()` | `Entity.BoundsNum` (via Render) | `Render.Bounds` — `Core/PoEMemory/Components/Render.cs:49` (+ `Entity.GetComponent<T>` — `Entity.cs:605`) | Returns `Vector3.Zero` if no `Render`. No `Bounds` on `Entity` in fork. |
| `Positioned.WorldPosNum()` | `Positioned.WorldPosNum : Vector2` | `Positioned.WorldPos` — `Core/PoEMemory/Components/Positioned.cs:40` | Compat doc, "Components — world". |
| `Positioned.GridPosNum()` | (Numerics grid pos) | `Positioned.GridPos` — `Positioned.cs:34` | |
| `Render.PosNum()` | `Render.PosNum : Vector3` | `Render.Pos` — `Render.cs:34` | |
| `Render.BoundsNum()` | `Render.BoundsNum : Vector3` | `Render.Bounds` — `Render.cs:49` | |
| `Render.RotationNum()` | `Render.RotationNum : Vector3` | `Render.Rotation` — `Render.cs:46` | Compat doc, "Components — combat & character" (`RotationNum.X` used in WAYG). |
| `Element.PositionNum()` | `Element.PositionNum : Vector2` | `Element.Position` — `Core/PoEMemory/Element.cs:61` | Compat doc, "UI Elements". |

Conversion uses the fork's `ToVector2Num` (`Extensions.cs:139`) for the Vector2 cases. The fork has
**no** SharpDX→`System.Numerics.Vector3` converter, so `ToVector3Num` is provided inline (it does
the same field-copy the other converters do).

## `MemoryCompat.cs`

`IMemory.ReadStdVector<T>` — emulates the upstream low-level read that the fork lacks (compat doc,
"Memory (low-level)": no `ReadStdVector` in either tree). It reads a contiguous C++ `std::vector`
of **value/unmanaged** structs.

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `IMemory.ReadStdVector<T>(NativePtrArray)` | `IMemory.ReadStdVector<T>(...)` | `NativePtrArray.First`/`.Last` — `GameOffsets/Native/NativePtrArray.cs:14,17`; `IMemory.Read<T>(long)` — `Core/Memory.cs:288`, decl `Core/Shared/Interfaces/IMemory.cs:56` | `T : unmanaged`. Stride = `Marshal.SizeOf<T>()`. |
| `IMemory.ReadStdVector<T>(NativePtrArray, int elementSize)` | upstream stride overload | same as above | Explicit element stride. |
| `IMemory.ReadStdVector<T>(long begin, long end, int elementSize)` | upstream raw overload | `IMemory.Read<T>(long)` — `Core/Memory.cs:288` | Guards against negative/huge bounds (cap 100000, mirroring `ReadStructsArray`'s guard at `Core/Memory.cs:144`). |

**Choose the right primitive.** This helper is for vectors that store structs *inline*. For other
shapes the fork already has the right call and you should prefer it:
- vector of `RemoteMemoryObject` **classes** → fork `ReadStructsArray<T>` (`Core/Memory.cs:137`);
- vector of **pointers** → fork `ReadNativeArray<T>(INativePtrArray)` (`Core/Memory.cs:451`).

## `ComponentCompat.cs`

Small component bridges plugins expect.

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `Stack.MaxSize()` | `Stack.MaxSize` | `Stack.Info` — `Core/PoEMemory/Components/Stack.cs:12`; `CurrencyInfo.MaxStackSize` — `Core/PoEMemory/Components/CurrencyInfo.cs:9` | Compat doc, "Components — items": read `Stack.Info.MaxStackSize`. Returns `0` if info null. |
| `SoundController.PlaySound(string, float volume)` | `SoundController.PlaySound(string, float)` | `SoundController.SetVolume(float)` — `Core/SoundController.cs:138`; `SoundController.PlaySound(string)` — `Core/SoundController.cs:71` | Compat doc, "Graphics, fonts & sound". Sets **master** volume then plays (volume persists). |
| `Life.GetBuffs()` | upstream `Buffs` component / `GetBuffs()` | `Life.Buffs` — `Core/PoEMemory/Components/Life.cs:79` | Compat doc, "Components — combat & character": no `Buffs` *component* in fork; buffs come off `Life`/`Entity`. Returns empty list if null. |
| `Life.HasBuffSafe(string)` | upstream buff query | `Life.HasBuff(string)` — `Core/PoEMemory/Components/Life.cs:122` | Null-safe wrapper. Named `*Safe` to avoid shadowing the existing instance `Life.HasBuff`. |
| `Mods.ImplicitMods()` | `Mods.ImplicitMods : List<ItemMod>` | `Mods.ModsStruct.implicitMods` — `GameOffsets/ModsComponentOffsets.cs:13`; `RemoteMemoryObject.GetObject<T>` — `Core/PoEMemory/RemoteMemoryObject.cs:84` | Compat doc, "Components — items". Reproduces the fork's own *private* `Mods.GetMods(long,long)` walk (`Mods.cs:106-126`: 0x28-byte stride, capped at 12 entries) over the implicit-only range instead of the combined `Mods.ItemMods`. Returns empty list if `mods` is null, has no address, or the range looks corrupt. |
| `Mods.ExplicitMods()` | `Mods.ExplicitMods : List<ItemMod>` | same as above, using `Mods.ModsStruct.explicitMods` — `GameOffsets/ModsComponentOffsets.cs:14` | Compat doc, "Components — items". Same walk, explicit-only range. |

## `EntityCompat.cs`

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `Entity.TryGetComponent<T>(out T)` | `Entity.TryGetComponent<T>(out T) : bool` | `Entity.GetComponent<T>()` — `Core/PoEMemory/MemoryObjects/Entity.cs:609` | Compat doc, "Entity & EntityListWrapper". Thin `bool`/`out` wrapper: `GetComponent<T>()` already returns `null` when absent. |

---

## Previously omitted, now implemented

An earlier pass over this directory marked four items below as omitted. Re-checked against the
current `Core/`/`GameOffsets/` (after PR #42, "readiness" batch) plus a closer read of `Mods.cs`,
they turned out to have a real, usable fork member to ground an additive helper on after all —
they were left out of the original batch for *scope* reasons, not because the underlying data was
unavailable. They are now implemented (see the tables above) and removed from the omitted list:

- **`Element.PositionNum`** → `NumericsCompat.Element.PositionNum()`. `Element.Position`
  (`Core/PoEMemory/Element.cs:61`) was already a plain SharpDX `Vector2`; this was always trivial,
  just outside the original batch's three target areas.
- **`Entity.TryGetComponent<T>(out T)`** → `EntityCompat.Entity.TryGetComponent<T>(out T)`. Thin
  wrapper over the existing `Entity.GetComponent<T>()` (`Entity.cs:609`), which already returns
  `null` when the component is absent.
- **`Mods.ImplicitMods` / `Mods.ExplicitMods`** → `ComponentCompat.Mods.ImplicitMods()` /
  `.ExplicitMods()`. The original note ("no per-affix-category list to delegate to") was incorrect:
  `Mods.ModsStruct` (`Mods.cs:26`) already carries **separate** `implicitMods` / `explicitMods`
  `NativePtrArray` fields (`GameOffsets/ModsComponentOffsets.cs:13-14`) — the fork's own
  `Mods.ItemMods` getter (`Mods.cs:47-55`) just concatenates both ranges via the *private*
  `GetMods(long,long)`. These helpers walk each range independently using the same stride/guard
  the private method uses, without needing to touch `Mods.cs` itself.

## Still omitted (no real fork member to build on)

The following upstream members from the compatibility doc remain **deliberately not implemented**
because the fork has no underlying member to ground an additive helper on (inventing one would
mean fabricating offsets/behavior, which is out of scope for a non-compilable proposal). Re-checked
against `Core/`/`GameOffsets/` as of this pass (post PR #42); none of these gained a usable fork
member:

- **`Mods.EnchantedMods` / `Mods.IncubatorName`** — unlike `ImplicitMods`/`ExplicitMods` above,
  `ModsComponentOffsets` (`GameOffsets/ModsComponentOffsets.cs`) has **no** enchant-mods array field
  and no incubator-name field at all; there is nothing to walk without fabricating an offset.
- **`Chest.Rarity`, `Transitionable.CurrentState`, `Actor.ActorVaalSkills`, `Render.Size`,
  `SkillGem.SkillExperience/ExperienceMax`** — no corresponding fork member to wrap (compat doc
  marks these missing/renamed with differing semantics; map by hand per the doc). Confirmed absent
  in `Core/PoEMemory/Components/Chest.cs`, `Transitionable.cs`, `Actor.cs`, `Render.cs`, and
  `SkillGem.cs` (which has only `ExperienceMaxLevel`/`ExperiencePrevLevel`/`ExperienceToNextLevel`).
- **`IngameState.Data.ServerData`, `InventorySlotE.Expanded*`, `FontAlign.VerticalCenter`,
  `SubMap`** — renamed paths, absent enum values, or absent types; not fixable with an extension
  method (the enum values / types simply do not exist here). Confirmed absent in
  `Core/PoEMemory/MemoryObjects/IngameData.cs`/`IngameState.cs`, `Core/Shared/Enums/InventorySlotE.cs`,
  `Core/Shared/Enums/FontAlign.cs`, and no `SubMap` type anywhere in `Core/`/`GameOffsets/`.

For every remaining row the fix stays as the compatibility doc states: use the named in-repo
equivalent, convert SharpDX↔Numerics at the boundary, or accept the subsystem is unavailable.

---

## Source grounding

All fork members above were verified by reading `master` `Core/` in this repository. Upstream
signatures were cross-checked against `origin/claude/ecstatic-ritchie-cje3s8` (the decompiled
ExileApi-Compiled reconstruction, PR #18) and
[`docs/api/compatibility-exileapi-compiled.md`](../../docs/api/compatibility-exileapi-compiled.md).
Note (per that doc): `ReadStdVector`, the `*Num` accessors, `Stack.MaxSize`, the 2-arg
`PlaySound`, and the `Buffs` component are **upstream-distribution-only** members — they are absent
from the reconstruction tree too, so their evidence is the reference-plugin call sites cited in the
compatibility doc, not the reconstruction.
