# Low-level memory access

> For advanced plugins that need to read process memory the high-level API does not expose. Prefer the typed game model (entities, components, UI elements) whenever it covers your need — raw reads are build-specific and easy to get wrong. See the [API reference index](README.md).

ExileCore reads the live Path of Exile process over a read-only handle. Every typed game object is ultimately a wrapper over an `Address` plus an `IMemory` reader. This page documents that reader and the `RemoteMemoryObject` base class so you can follow pointers the framework does not already surface.

## `IMemory` / `Memory`

`ExileCore.Shared.Interfaces.IMemory` is the abstraction; `ExileCore.Memory` is the concrete implementation that opens the process handle (`ProcessAccessFlags.VirtualMemoryRead`) and runs the initial pattern scans. All reads are best-effort: an invalid address yields `default(T)`, an empty array, or an empty string rather than throwing (with the exception of `ReadMem`, which logs and rethrows on a failed native read).

Inside a `RemoteMemoryObject` you reach it through the `M` property; from a plugin you reach it through `GameController.Memory`.

### Process / base members

| Member | Type | Meaning |
| --- | --- | --- |
| `AddressOfProcess` | `long` | Base address of the game's main module. |
| `OpenProcessHandle` | `IntPtr` | The open read handle. |
| `MainWindowHandle` | `IntPtr` | Game main window handle. |
| `Process` | `Process` | The underlying `System.Diagnostics.Process`. |
| `BaseOffsets` | `Dictionary<OffsetsName, long>` | Offsets resolved by the initial pattern scan. |
| `IsInvalid()` | `bool` | `true` once the process has exited or the handle was disposed. |

### Reading values and buffers

| Method | Signature | Notes |
| --- | --- | --- |
| `Read<T>` | `T Read<T>(long addr) where T : struct` | Copies the bytes at `addr` onto a managed struct. The workhorse for reading a `GameOffsets` struct. |
| `Read<T>` | `T Read<T>(IntPtr addr) where T : struct` | Same, `IntPtr` overload. |
| `Read<T>` (pointer chain) | `T Read<T>(long addr, params int[] offsets)` / `(IntPtr addr, params int[] offsets)` | Dereferences the pointer at `addr`, then walks each `offset` as a further pointer, returning `T` at the final hop. Returns `default` if any hop is null. |
| `ReadMem` | `byte[] ReadMem(long addr, int size)` / `(IntPtr addr, int size)` | Raw byte buffer. Returns `new byte[0]` for `size <= 0` or non-positive address; logs and rethrows on native failure. |
| `ReadBytes` | `byte[] ReadBytes(long addr, int size)` / `(long addr, long size)` | Thin aliases over `ReadMem`. |

> The `Read<T>(Pointer addr)` overloads and `ReadSecondPointerArray_Count` exist on the interface but `throw NotImplementedException` — do not call them.

### Reading strings

| Method | Signature | Notes |
| --- | --- | --- |
| `ReadString` | `string ReadString(long addr, int length = 256, bool replaceNull = true)` | ASCII. Returns `""` for low/invalid addresses. |
| `ReadStringU` | `string ReadStringU(long addr, int length = 256, bool replaceNull = true)` | UTF-16 (Unicode). The common case for PoE text. Returns `""` on `addr == 0`, empty buffer, or `length` outside `0..5120`. |
| `ReadNativeString` | `string ReadNativeString(long addr)` | Reads a native (length-prefixed) string at `addr`, dereferencing the inline buffer when the reserved/capacity field indicates a heap allocation (small-string optimization). |

`replaceNull` (default `true`) trims everything from the first NUL onward.

### Reading arrays and collections

| Method | Signature | Notes |
| --- | --- | --- |
| `ReadStructsArray<T>` | `List<T> ReadStructsArray<T>(long startAddress, long endAddress, int structSize, RemoteMemoryObject game) where T : RemoteMemoryObject, new()` | Materializes one `T` per `structSize`-byte slot in `[start, end)` via `game.GetObject<T>`. Bails out (returns empty, logs) if the implied count is negative or `> 100000`. |
| `ReadNativeArray<T>` | `IList<T> ReadNativeArray<T>(INativePtrArray ptrArray, int offset = 8) where T : struct` | Reads a value `T` per pointer in a native vector (`First`/`Last`). |
| `ReadPointersArray` | `IList<long> ReadPointersArray(long startAddress, long endAddress, int offset = 8)` | Raw 64-bit pointers between two addresses. Caps at `20000 * 8` bytes and a 2s budget. |
| `ReadDoublePtrVectorClasses<T>` | `IList<T> ReadDoublePtrVectorClasses<T>(long address, RemoteMemoryObject game, bool noNullPointers = false)` | Reads a vector of pointers-to-objects (start at `address`, last at `address + 0x10`) and materializes each via `game.GetObject<T>`. |
| `ReadList<T>` | `IList<T> ReadList<T>(IntPtr head) where T : struct` | Walks a native doubly-linked list from `head` (see `NativeListNode`), reading each node value as `T`. |
| `ReadListPointer` | `IList<long> ReadListPointer(IntPtr head)` | Same walk, collecting each node pointer. |
| `ReadDoublePointerIntList` | `IList<Tuple<long, int>> ReadDoublePointerIntList(long address)` | Walks a `(key, value)` linked list. |
| `FindPatterns` | `long[] FindPatterns(params IPattern[] patterns)` | Scans the executable image for byte patterns; used by the engine's startup scan, not normally by plugins. |

## `RemoteMemoryObject`

`ExileCore.PoEMemory.RemoteMemoryObject` is the base class for every object backed by a memory location. It wraps a single `Address` and provides the helpers that materialize *other* objects from pointers stored relative to it. Materialization is lazy and cheap: each helper just sets a new object's `Address`; no memory is read until you touch a property.

| Member | Type / signature | Meaning |
| --- | --- | --- |
| `Address` | `long` (get; protected set) | The anchor address. Setting it raises `OnAddressChange()`. |
| `M` | `IMemory` | The shared memory reader. |
| `Cache` | `static Cache` | The shared read cache (see [caching.md](caching.md)). |
| `TheGame` | `TheGame` | The current game context. |
| `GetObject<T>(long address)` | `T where T : RemoteMemoryObject, new()` | New `T` anchored directly at `address`. |
| `GetObject<T>(IntPtr address)` | `T` | Same, `IntPtr` overload. |
| `GetObjectAt<T>(int offset)` / `(long offset)` | `T` | New `T` anchored at `Address + offset` (no pointer dereference). |
| `ReadObject<T>(long addressPointer)` | `T` | Reads the pointer stored at `addressPointer`, anchors a new `T` at the dereferenced address. |
| `ReadObjectAt<T>(int offset)` | `T` | `ReadObject<T>(Address + offset)` — follow a pointer field at `offset`. |
| `AsObject<T>()` | `T` | Reinterprets *this* object's `Address` as a different `T`. |

The distinction that matters:

- `GetObjectAt<T>(offset)` treats `Address + offset` as where the object *is* (an embedded sub-struct).
- `ReadObjectAt<T>(offset)` treats `Address + offset` as a *pointer to* where the object is, and follows it.

```csharp
// inside a RemoteMemoryObject subclass:
var embedded = GetObjectAt<SomeElement>(0x30);   // object lives at Address+0x30
var viaPointer = ReadObjectAt<SomeElement>(0x40); // pointer at Address+0x40 -> object
```

`AtlasNode` (`Core/PoEMemory/MemoryObjects/AtlasNode.cs`) is a compact real example of direct reads and lazy pointer-following:

```csharp
public float PosX => M.Read<float>(Address + 0x11D);
public string FlavourText => M.ReadStringU(M.Read<long>(Address + 0x44)); // follow pointer, then read UTF-16
public WorldArea Area => TheGame.Files.WorldAreas.GetByAddress(M.Read<long>(Address + 0x8));
```

## Native strings

`ExileCore.PoEMemory.MemoryObjects.NativeStringReader.ReadString(long address, IMemory M)` is a static helper that reads a `std::string`-style native string, accounting for the small-string optimization: it reads the size at `address + 0x10` and capacity at `address + 0x18`, dereferences the heap buffer when `capacity >= 8`, otherwise reads the inline bytes. `IMemory.ReadNativeString` does the equivalent for plugin code that holds only an address.

For strings already described by a `GameOffsets` struct, the `NativeStringU.ToString(this NativeStringU str, IMemory mem)` extension (`Core/Shared/Helpers/MiscHelpers.cs`) decodes inline-vs-heap from the struct fields directly.

## The GameOffsets bridge

The recommended pattern for typed memory objects is: define a layout struct in `GameOffsets` with `[StructLayout(LayoutKind.Explicit)]` + `[FieldOffset(...)]`, read the whole struct once with `M.Read<T>(Address)`, then expose typed properties from the cached value. See [../offsets.md](../offsets.md) for the struct conventions (do not duplicate offset definitions here).

`Cursor` (`Core/PoEMemory/MemoryObjects/Cursor.cs`) shows the full bridge, caching the struct per frame:

```csharp
public class Cursor : Element
{
    private readonly CachedValue<CursorOffsets> _cachevalue;

    public Cursor()
    {
        _cachevalue = new FrameCache<CursorOffsets>(() => M.Read<CursorOffsets>(Address));
    }

    public int ClicksCached => _cachevalue.Value.Clicks;                  // from the cached struct
    public int Clicks => M.Read<int>(Address + 0x24C);                    // direct, uncached
    public string ActionStringCached => _cachevalue.Value.ActionString.ToString(M); // NativeStringU field -> string
}
```

`FrameCache<T>` recomputes once per rendered frame; see [caching.md](caching.md) for the cache types.

### Native vectors

A native `std::vector` is modeled by `GameOffsets.Native.NativePtrArray` — three pointers, `First` / `Last` / `End`, plus `Size => Last - First`. When a `GameOffsets` struct has a `[FieldOffset(...)] public NativePtrArray Foo;` field, iterate it with `M.ReadNativeArray<T>(foo)` (element values) or step the pointers manually between `First` and `Last`. Related native shapes live under `GameOffsets/Native/` (`NativeListNode`, `NativeHashNode`, `NativeUnicodeText`, `Vector2i`).

## Advanced: writing your own memory object

If — and only if — the high-level API does not expose what you need, you can model a structure yourself. This is build-specific: every offset below must be re-verified against a current reference after a game patch (see [../offsets.md](../offsets.md)), and the framework's own typed objects should be preferred wherever they exist.

Minimal sketch:

```csharp
using System.Runtime.InteropServices;
using ExileCore.PoEMemory;
using ExileCore.Shared.Cache;

// 1. Layout struct (in GameOffsets, or alongside your plugin):
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct MyThingOffsets
{
    [FieldOffset(0x10)] public int   SomeCount;
    [FieldOffset(0x18)] public long  ChildPtr;   // pointer to another object
}

// 2. RemoteMemoryObject subclass that reads the struct via a frame cache:
public class MyThing : RemoteMemoryObject
{
    private readonly CachedValue<MyThingOffsets> _cache;

    public MyThing() => _cache = new FrameCache<MyThingOffsets>(() => M.Read<MyThingOffsets>(Address));

    public int SomeCount => _cache.Value.SomeCount;

    // follow ChildPtr to materialize another object lazily:
    public MyChild Child => GetObject<MyChild>(_cache.Value.ChildPtr);
}
```

You materialize an instance from an address you already hold (for example via `GetObject<MyThing>(address)` on any existing `RemoteMemoryObject`, or `GameController.Game.GetObject<MyThing>(address)`). Reads only happen when properties are touched; the `FrameCache` keeps a struct read to once per frame.

For where these objects sit in the live object graph, see [ingame-state.md](ingame-state.md); for static game data read out of memory, see [files-in-memory.md](files-in-memory.md).

## Source

- `Core/Memory.cs`, `Core/Shared/Interfaces/IMemory.cs`
- `Core/PoEMemory/RemoteMemoryObject.cs`
- `Core/PoEMemory/MemoryObjects/NativeStringReader.cs`, `Core/PoEMemory/MemoryObjects/Cursor.cs`, `Core/PoEMemory/MemoryObjects/AtlasNode.cs`
- `Core/Shared/Helpers/MiscHelpers.cs` (`NativeStringU.ToString`), `Core/Shared/Cache/FrameCache.cs`
- `GameOffsets/Native/NativePtrArray.cs`, `GameOffsets/Native/NativeStringU.cs`, `GameOffsets/Native/NativeListNode.cs`, `GameOffsets/Native/NativeHashNode.cs`
- Plugin cross-check: [instantsc/Radar](https://github.com/instantsc/Radar) uses `GameController.Memory.Read<TgtTileStruct>(...)` for direct struct reads (`Radar.Pathfinding.cs`). Most plugins use the high-level API rather than reading memory directly; that fork also calls a `ReadStdVector` helper that is not present in this repository.
