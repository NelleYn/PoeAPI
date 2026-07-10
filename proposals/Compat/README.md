# Compat helpers — PROMOTED

> **PROMOTED.** The candidate `.cs` files that used to live in this directory are now part of the
> Core build: they were moved to [`Core/Shared/Compat/`](../../Core/Shared/Compat/) with the
> file-scoped namespace changed from `ExileCore.Shared.Compat` to **`ExileCore.Shared`** (the
> "lower-friction choice" from the original integration notes — callers get the helpers through the
> `using ExileCore.Shared;` they already have). This README stays behind as the promotion record.

Additive C# helpers (extension methods / static helpers) that fill the highest-traffic API gaps
between this fork and the **ExileApi-Compiled** distribution that most reference plugins target.
No existing file was modified — every helper is an extension method or static method over a type
that already exists in the fork, grounded in a real fork member and emulating a concrete upstream
member documented in
[`docs/api/compatibility-exileapi-compiled.md`](../../docs/api/compatibility-exileapi-compiled.md).

## Dropped at promotion

Three members from the original `NumericsCompat.cs` candidate were **deleted** during promotion
because Core now provides the same thing directly — keeping them would have caused ambiguity
(CS0121) or dead duplication:

| Dropped candidate member | Superseded by (Core) |
| --- | --- |
| `SharpDX.Vector3.ToVector3Num()` | Identical extension already in `Core/Shared/Helpers/Extensions.cs:150` |
| `Entity.PosNum()` extension | Instance property `Entity.PosNum` — `Core/PoEMemory/MemoryObjects/Entity.cs:140` |
| `Entity.GridPosNum()` extension | Instance property `Entity.GridPosNum` — `Core/PoEMemory/MemoryObjects/Entity.cs:179` |

One `ComponentCompat.cs` candidate was **dropped as unshippable** rather than superseded:

- **`SoundController.PlaySound(string, float)`** — the proposal composed `SetVolume(float)` +
  `PlaySound(string)`, but that is not a faithful emulation: `SetVolume` mutates the **global
  master** volume permanently (a state leak across every plugin's subsequent playback, where
  upstream's parameter is per-playback), and it dereferences the mastering voice without the
  `initialized` guard, so it throws `NullReferenceException` on a controller constructed without a
  Sounds directory — exactly the state the fork's own 1-arg `PlaySound` no-ops on
  (`Core/SoundController.cs:71,138`). Faithful per-voice volume needs XAudio2 work inside
  `SoundController` itself. Porters should keep the workaround from the compatibility doc: call
  `SetVolume` + `PlaySound` explicitly if the global-volume semantics are acceptable.

Everything else shipped as proposed. The shipped members are listed below.

---

## `NumericsCompat.cs`

`System.Numerics` accessors emulating upstream `PosNum` / `GridPosNum` / `WorldPosNum` /
`BoundsNum` / `RotationNum`. The fork stores these as SharpDX vectors; the helpers convert at the
boundary using the fork's `ToVector2Num` / `ToVector3Num` converters
(`Core/Shared/Helpers/Extensions.cs:140,150`).

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `Entity.BoundsNum()` | `Entity.BoundsNum` (via Render) | `Render.Bounds` — `Core/PoEMemory/Components/Render.cs:49` (+ `Entity.GetComponent<T>` — `Entity.cs:609`) | Returns `Vector3.Zero` if no `Render`. No `Bounds` on `Entity` in fork. |
| `Positioned.WorldPosNum()` | `Positioned.WorldPosNum : Vector2` | `Positioned.WorldPos` — `Core/PoEMemory/Components/Positioned.cs:40` | Compat doc, "Components — world". |
| `Positioned.GridPosNum()` | (Numerics grid pos) | `Positioned.GridPos` — `Positioned.cs:34` | |
| `Render.PosNum()` | `Render.PosNum : Vector3` | `Render.Pos` — `Render.cs:34` | |
| `Render.BoundsNum()` | `Render.BoundsNum : Vector3` | `Render.Bounds` — `Render.cs:49` | |
| `Render.RotationNum()` | `Render.RotationNum : Vector3` | `Render.Rotation` — `Render.cs:46` | Compat doc, "Components — combat & character" (`RotationNum.X` used in WAYG). |
| `Element.PositionNum()` | `Element.PositionNum : Vector2` | `Element.Position` — `Core/PoEMemory/Element.cs:61` | Compat doc, "UI Elements". |

For upstream `Entity.PosNum` / `Entity.GridPosNum`, use the Core instance properties directly
(see "Dropped at promotion" above).

## `MemoryCompat.cs`

`IMemory.ReadStdVector<T>` extension overloads — emulate the upstream low-level read for a
contiguous C++ `std::vector` of **value/unmanaged** structs. They intentionally share the
`ReadStdVector` name with the fork's instance methods (`Core/Shared/Interfaces/IMemory.cs:58,64`)
for plugin compatibility; the parameter shapes do not collide.

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `IMemory.ReadStdVector<T>(NativePtrArray)` | `IMemory.ReadStdVector<T>(...)` | `NativePtrArray.First`/`.Last` — `GameOffsets/Native/NativePtrArray.cs:14,17`; `IMemory.ReadMem(long,int)` — `Core/Shared/Interfaces/IMemory.cs:41` | `T : unmanaged`. Stride = `Marshal.SizeOf<T>()`. |
| `IMemory.ReadStdVector<T>(NativePtrArray, int elementSize)` | upstream stride overload | same as above | Explicit element stride. |
| `IMemory.ReadStdVector<T>(long begin, long end, int elementSize)` | upstream raw overload | `IMemory.ReadMem(long,int)` — one bulk read, decoded per element like the instance `ReadStdVector<T>(long)` (`Core/Memory.cs:229`) | Guards against negative/huge bounds (cap 100000, mirroring the instance method's guard). |

**Choose the right primitive.** These helpers are for vectors that store structs *inline*. For other
shapes the fork already has the right call and you should prefer it:
- vector of `RemoteMemoryObject` **classes** → fork `ReadStructsArray<T>` (`Core/Memory.cs:138`);
- vector of **pointers** → fork `ReadNativeArray<T>(INativePtrArray)` (`Core/Memory.cs:531`).

## `ComponentCompat.cs`

Small component bridges plugins expect.

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `Stack.MaxSize()` | `Stack.MaxSize` | `Stack.Info` — `Core/PoEMemory/Components/Stack.cs:12`; `CurrencyInfo.MaxStackSize` — `Core/PoEMemory/Components/CurrencyInfo.cs:9` | Compat doc, "Components — items". Returns `0` if info null. |
| `Life.GetBuffs()` | upstream `Buffs` component / `GetBuffs()` | `Life.Buffs` — `Core/PoEMemory/Components/Life.cs:79` | Returns `List<Buff>` (upstream shape); empty list if null. |
| `Life.HasBuffSafe(string)` | upstream buff query | `Life.HasBuff(string)` — `Core/PoEMemory/Components/Life.cs:122` | Null-safe wrapper. Named `*Safe` to avoid shadowing the instance `Life.HasBuff`. |
| `Mods.ImplicitMods()` | `Mods.ImplicitMods : List<ItemMod>` | `Mods.ModsStruct.implicitMods` — `GameOffsets/ModsComponentOffsets.cs:13`; `RemoteMemoryObject.GetObject<T>` — `Core/PoEMemory/RemoteMemoryObject.cs:84` | Reproduces the fork's private `Mods.GetMods(long,long)` walk (0x28-byte stride, capped at 12) over the implicit-only range. |
| `Mods.ExplicitMods()` | `Mods.ExplicitMods : List<ItemMod>` | same, using `Mods.ModsStruct.explicitMods` — `GameOffsets/ModsComponentOffsets.cs:14` | Same walk, explicit-only range. |

## `EntityCompat.cs`

| Helper | Emulates (upstream) | Builds on (fork member, file:line) | Notes |
| --- | --- | --- | --- |
| `Entity.TryGetComponent<T>(out T)` | `Entity.TryGetComponent<T>(out T) : bool` | `Entity.GetComponent<T>()` — `Core/PoEMemory/MemoryObjects/Entity.cs:609` | Thin `bool`/`out` wrapper: `GetComponent<T>()` already returns `null` when absent. |

---

## Still omitted (no real fork member to build on)

The following upstream members from the compatibility doc remain **deliberately not implemented**
because the fork has no underlying member to ground an additive helper on (inventing one would
mean fabricating offsets/behavior):

- **`Mods.EnchantedMods` / `Mods.IncubatorName`** — `ModsComponentOffsets`
  (`GameOffsets/ModsComponentOffsets.cs`) has **no** enchant-mods array field and no incubator-name
  field at all; there is nothing to walk without fabricating an offset.
- **`Chest.Rarity`, `Transitionable.CurrentState`, `Actor.ActorVaalSkills`, `Render.Size`,
  `SkillGem.SkillExperience/ExperienceMax`** — no corresponding fork member to wrap (compat doc
  marks these missing/renamed with differing semantics; map by hand per the doc).
- **`IngameState.Data.ServerData`, `InventorySlotE.Expanded*`, `FontAlign.VerticalCenter`,
  `SubMap`** — renamed paths, absent enum values, or absent types; not fixable with an extension
  method.

For every remaining row the fix stays as the compatibility doc states: use the named in-repo
equivalent, convert SharpDX↔Numerics at the boundary, or accept the subsystem is unavailable.

---

## Source grounding

All fork members above were verified by reading `master` `Core/` in this repository. Upstream
signatures were cross-checked against `origin/claude/ecstatic-ritchie-cje3s8` (the decompiled
ExileApi-Compiled reconstruction, PR #18) and
[`docs/api/compatibility-exileapi-compiled.md`](../../docs/api/compatibility-exileapi-compiled.md).
Note (per that doc): the `*Num` accessors, `Stack.MaxSize`, the 2-arg `PlaySound`, and the `Buffs`
component are **upstream-distribution-only** members — they are absent from the reconstruction tree
too, so their evidence is the reference-plugin call sites cited in the compatibility doc, not the
reconstruction. Upstream's value-struct `ReadStdVector` shapes are likewise distribution-only, but
the fork has since gained its own instance `ReadStdVector<T>(long)` (`Core/Memory.cs:229`, declared
at `Core/Shared/Interfaces/IMemory.cs:58`); the extensions here only add the header/raw-bounds
parameter shapes upstream plugins call.
