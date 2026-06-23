# Settings & menu nodes

> Plugin settings are plain C# classes whose properties are *node* wrappers; ExileCore reflects over them to build the ImGui menu and to load/save JSON. See the [API reference index](README.md).

A plugin's settings object implements [`ISettings`](#isettings). Every public property whose type is one of the node types below is turned into a menu widget automatically by [`SettingsParser`](#settingsparser), and persisted as JSON by [`SettingsContainer`](#settingscontainer). You rarely instantiate the drawer types yourself — you just declare node-typed properties with defaults.

Namespaces:

- Nodes: `ExileCore.Shared.Nodes`
- Attributes: `ExileCore.Shared.Attributes`
- Interfaces: `ExileCore.Shared.Interfaces`

See [plugins.md](plugins.md) for how a plugin exposes its settings, and [input.md](input.md) for using `HotkeyNode` at runtime. (Sibling docs may not exist yet.)

---

## ISettings

The base contract for every plugin settings class. It requires a single `Enable` toggle that the core uses to switch the owning plugin on or off.

```csharp
namespace ExileCore.Shared.Interfaces;

public interface ISettings
{
    ToggleNode Enable { get; set; }
}
```

A minimal settings class:

```csharp
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

public class MySettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(true);
}
```

> The `Enable` property is special-cased: `SettingsParser` skips it unless it carries a `[Menu]` attribute (see [Enable handling](#enable-handling)).

## ISettingsHolder

The drawer contract. A holder is one node in the rendered menu tree: it carries a label, tooltip, a `DrawDelegate` that renders the widget, and a list of child holders. You do not implement this yourself — `SettingsParser` produces a `SettingsHolder` (the concrete implementation) per property and the menu window calls `Draw()`.

```csharp
namespace ExileCore.Shared.Interfaces;

public interface ISettingsHolder
{
    string Name { get; set; }
    string Tooltip { get; set; }
    string Unique { get; }            // ImGui id: $"{Name}##{ID}"
    int ID { get; set; }
    Action DrawDelegate { get; set; }
    IList<ISettingsHolder> Children { get; }
    void Draw();
}
```

The concrete `SettingsHolder` (in `Core/SettingsParser.cs`) adds a `HolderChildType Type { get; set; }` (`Tab` or `Border`, default `Border`) controlling whether its children render as a tab or a bordered group. `Unique` is computed as `$"{Name}##{ID}"`, giving each widget a stable ImGui id so labels can repeat.

---

## Node types

Each node is a thin wrapper around a value. Most expose implicit conversions to/from their underlying type, so you can read a node directly where the raw value is expected (e.g. `if (Settings.Enable) ...`). Declare them as auto-properties with a default `new(...)`.

| Node | Value type | Renders as | Notes |
| --- | --- | --- | --- |
| `ToggleNode` | `bool` | Checkbox | implicit → `bool`; `OnValueChanged` event; `SetValueNoEvent` |
| `RangeNode<T>` | `T : struct` | Slider | `int`/`float`/`long`/`Vector2` supported; implicit → `T` |
| `HotkeyNode` | `System.Windows.Forms.Keys` | Key-capture button | implicit ↔ `Keys`; `PressedOnce()`, `UnpressedOnce()` |
| `ColorNode` | `SharpDX.Color` | Color picker (`ColorEdit4`) | implicit ↔ `Color`/`uint`/`ColorBGRA` |
| `ButtonNode` | — | Button | runs `OnPressed` action when clicked |
| `ListNode` | `string` | Combo box | choose from `Values`; `OnValueSelected` callbacks |
| `TextNode` | `string` | (text value) | implicit ↔ `string`; `OnValueChanged`; `SetValueNoEvent` |
| `FileNode` | `string` | File tree (`config/`) | implicit ↔ `string`; `OnFileChanged` event |
| `StashTabNode` | (stash tab ref) | (custom) | holds `Name`/`VisibleIndex`; not auto-drawn |
| `EmptyNode` | — | nothing | placeholder used as a `[Menu]` group root |

### ToggleNode

A boolean. Renders as an ImGui `Checkbox`. Implicitly converts to `bool`. Fires `OnValueChanged` (an `EventHandler<bool>`) when the value changes; use `SetValueNoEvent(bool)` to set it silently.

```csharp
public ToggleNode Enable { get; set; } = new ToggleNode(true);
public ToggleNode ShowOverlay { get; set; } = new ToggleNode(false);
```

Constructors: `ToggleNode()` (defaults to `false`) and `ToggleNode(bool value)`.

### RangeNode&lt;T&gt;

A bounded numeric value rendered as a slider. The constructor takes **value, then min, then max**:

```csharp
public RangeNode(T value, T min, T max)
```

```csharp
public RangeNode<int>   Radius     { get; set; } = new RangeNode<int>(50, 0, 200);
public RangeNode<float> Opacity    { get; set; } = new RangeNode<float>(0.5f, 0f, 1f);
public RangeNode<int>   PickupRange{ get; set; } = new RangeNode<int>(600, 1, 1000);
```

`SettingsParser` provides slider drawers for `RangeNode<int>` (`SliderInt`), `RangeNode<float>` (`SliderFloat`), `RangeNode<long>` (rendered via `SliderInt` after a cast) and `RangeNode<Vector2>` (`SliderFloat2`, using the X component of `Min`/`Max` for both axes). Other `T` log a "not supported" warning.

`Min`/`Max` are `[JsonIgnore]` (they come from the declared default, not from disk). `Value` raises `OnValueChanged` (`EventHandler<T>`). The node implicitly converts to `T`.

`Vector2` here is `System.Numerics.Vector2`:

```csharp
using Vector2 = System.Numerics.Vector2;
public RangeNode<Vector2> Position { get; set; } =
    new RangeNode<Vector2>(new Vector2(50f, 50f), Vector2.Zero, new Vector2(100f, 100f));
```

### HotkeyNode

Wraps `System.Windows.Forms.Keys`. The parameterless constructor defaults to `Keys.Space`. It implicitly converts to and from `Keys`, so you can assign a key directly:

```csharp
public HotkeyNode ToggleKey { get; set; } = Keys.F12;            // implicit Keys -> HotkeyNode
public HotkeyNode PickUpKey { get; set; } = new HotkeyNode(Keys.F);
public HotkeyNode PauseKey  { get; set; } = new HotkeyNode(Keys.Space);
```

In the menu it renders as a button that opens a modal popup; pressing any key (or Esc to cancel) rebinds it. Setting `Value` raises the `OnValueChanged` action. For polling at runtime use:

- `PressedOnce()` — `true` exactly once per physical press (rising edge).
- `UnpressedOnce()` — `true` exactly once when the key is released.

(See [input.md](input.md); these are backed by `Input.IsKeyDown` / `Input.GetKeyState`.)

### ColorNode

Wraps `SharpDX.Color`. Renders as an ImGui `ColorEdit4` (with alpha bar, no text inputs, half alpha preview). It also tracks an HTML `Hex` string. Constructors accept a `SharpDX.Color` or a packed `uint` (interpreted as ABGR via `Color.FromAbgr`); implicit conversions exist from `Color`, `uint` and `ColorBGRA`:

```csharp
using Color = SharpDX.Color;

public ColorNode TextColor       { get; set; } = new Color(255, 255, 255, 255);
public ColorNode BackgroundColor { get; set; } = new Color(0, 0, 0, 50);   // implicit Color -> ColorNode
public ColorNode Accent          { get; set; } = 0xFF00FF00;               // implicit uint (ABGR) -> ColorNode
```

It is serialized by [`ColorNodeConverter`](#json-converters) as an 8-digit ABGR hex string.

### ButtonNode

A click action. It has no value — only a `[JsonIgnore] Action OnPressed` (defaults to a no-op). Renders as a button that invokes `OnPressed` when clicked. Wire the action up in your plugin's initialization:

```csharp
public ButtonNode RefreshArea { get; set; } = new ButtonNode();
// elsewhere:
Settings.RefreshArea.OnPressed += () => Refresh();
```

### ListNode

A string chosen from a list. Renders as an ImGui combo box over `Values`. `Values` is `[JsonIgnore]` (you populate it at runtime); only the selected `Value` is persisted. `OnValueSelectedPre`/`OnValueSelected` fire around a change. Implicitly converts to `string`.

```csharp
public ListNode Profile { get; set; } =
    new ListNode { Values = new List<string> { "global" }, Value = "global" };

// populate at runtime:
Settings.Profile.SetListValues(LoadProfileNames());
```

### TextNode

An editable string. Implicitly converts to/from `string`, fires `OnValueChanged` on change, and offers `SetValueNoEvent(string)`. (TextNode itself has no built-in drawer in `SettingsParser`; plugins typically render it in their own `DrawSettings` or use it as a plain persisted string.)

```csharp
public TextNode CustomConfigDir { get; set; } = new TextNode();
public TextNode MetadataRegex   { get; set; } = new TextNode("^$");
```

### FileNode

A file path string, rendered as a tree node that lists files under the `config/` directory for selection. Implicitly converts to/from `string` and raises `OnFileChanged` when the path changes. Persisted as a plain string by [`FileNodeConverter`](#json-converters).

```csharp
public FileNode RuleFile { get; set; } = new FileNode();
```

### StashTabNode

Identifies a stash tab by `Name` and `VisibleIndex`. It has no auto-generated drawer; plugins that target stash tabs draw selectors themselves and persist the node. Constructor:

```csharp
public StashTabNode(string name, int visibleIndex, int id, InventoryTabFlags flag)
```

Persisted members: `Name` (defaults to `StashTabNode.EMPTYNAME` = `"-NoName-"`) and `VisibleIndex` (defaults to `-1`, meaning "ignore"). `Exist`, `Id` and `IsRemoveOnly` are `[JsonIgnore]`. `IsRemoveOnly` is derived from `InventoryTabFlags.RemoveOnly` (`ExileCore.Shared.Enums`).

```csharp
public StashTabNode TargetTab { get; set; } = new StashTabNode();
```

### EmptyNode

A valueless placeholder. It draws nothing, but combined with a `[Menu]` index it acts as the **root** of a menu group that other properties nest under (see [MenuAttribute](#menuattribute)).

```csharp
[Menu("Performance", 100)]
public EmptyNode PerformanceRoot { get; set; } = new EmptyNode();
```

---

## Attributes

### MenuAttribute

`[Menu]` (`ExileCore.Shared.Attributes.MenuAttribute`, `AttributeUsage = Property`) controls a property's label, tooltip, ordering index and parent. Without it, `SettingsParser` derives a label from the property name (inserting spaces before interior capitals, e.g. `ShowOverlay` → `Show Overlay`).

Fields and constructor overloads:

| Field | Meaning |
| --- | --- |
| `MenuName` | display label |
| `Tooltip` | hover text (the `(?)` marker) |
| `index` | this entry's id; `-1` means "auto-assign a random id" |
| `parentIndex` | id of the parent group to nest under; `-1` means top level |

```csharp
public MenuAttribute(string menuName);
public MenuAttribute(string menuName, string tooltip);
public MenuAttribute(string menuName, int index);
public MenuAttribute(string menuName, string tooltip, int index);
public MenuAttribute(string menuName, int index, int parentIndex);
public MenuAttribute(string menuName, string tooltip, int index, int parentIndex);
```

Nesting pattern — give a group root an `index`, then point children at it via `parentIndex`:

```csharp
[Menu("Performance", 100)]                 // group root (index 100)
public EmptyNode PerformanceRoot { get; set; } = new EmptyNode();

[Menu("Target FPS", 10, 100)]              // index 10, parentIndex 100
public RangeNode<int> TargetFps { get; set; } = new RangeNode<int>(60, 5, 200);

[Menu("Dynamic FPS", "HUD FPS follows game FPS", 15, 100)]
public ToggleNode DynamicFps { get; set; } = new ToggleNode(false);
```

A property whose type itself implements `ISettings` and that carries a `[Menu]` with an explicit `index` becomes a **tab** (`HolderChildType.Tab`), and its inner properties are parsed under it.

### IgnoreMenuAttribute

`[IgnoreMenu]` (declared alongside `MenuAttribute`, `AttributeUsage = Property`) excludes a property from the generated menu while still persisting it. `SettingsParser` skips any property carrying it:

```csharp
[Menu("Font size")]
[IgnoreMenu]   // kept in JSON, but not shown in the menu
public RangeNode<int> FontSize { get; set; } = new RangeNode<int>(13, 7, 36);
```

To exclude a property from *saving* as well, add Newtonsoft's `[JsonIgnore]`.

### HideInReflectionAttribute

`[HideInReflection]` (`AttributeUsage = Property | Field | Method`) marks a member so it is excluded when members are enumerated via reflection elsewhere in the framework. Note this is a separate mechanism from `[IgnoreMenu]`: `SettingsParser` itself filters on `IgnoreMenuAttribute`, not on `HideInReflectionAttribute`.

---

## SettingsParser

`ExileCore.SettingsParser` (static) reflects over an `ISettings` object and fills a `List<ISettingsHolder>` of drawers.

```csharp
public static void Parse(ISettings settings, List<ISettingsHolder> draws, int id = -1);
```

For each public property (in declaration order):

1. Skip it if it has `[IgnoreMenu]`.
2. Read its `[Menu]` (or synthesize one from the property name).
3. <a id="enable-handling"></a>If the property is named `Enable` and has no `[Menu]`, skip it (the core renders the enable toggle separately).
4. If the property type implements `ISettings`, recurse into it — creating a **tab** holder when its `[Menu]` has an explicit `index`, otherwise inlining its members.
5. Otherwise create a `SettingsHolder`, attach it to its parent (by `parentIndex`, then by the recursion `id`, else at top level), and assign a `DrawDelegate` based on the node's runtime type via a `switch`:

| Runtime type | ImGui call |
| --- | --- |
| `ButtonNode` | `Button` → `OnPressed()` |
| `HotkeyNode` | `Button` + key-capture `BeginPopupModal` |
| `ToggleNode` | `Checkbox` |
| `ColorNode` | `ColorEdit4` |
| `ListNode` | `BeginCombo` / `Selectable` over `Values` |
| `FileNode` | `TreeNode` listing files in `config/` |
| `RangeNode<int>` | `SliderInt` |
| `RangeNode<float>` | `SliderFloat` |
| `RangeNode<long>` | `SliderInt` (cast) |
| `RangeNode<Vector2>` | `SliderFloat2` |
| `EmptyNode` | none (group root) |
| anything else | logs a "not supported" warning |

The produced `SettingsHolder.Draw()` renders the widget; holders with children render their children inside a bordered child region (or tab) labelled with `Name` and an optional `(?)` tooltip.

## SettingsContainer

`ExileCore.SettingsContainer` loads and persists settings as JSON under the `config/` directory, organised by profile (default profile `global`). Files are guarded by a `ReaderWriterLockSlim`.

- Core settings live in `config/settings.json`.
- Plugin settings live in `config/<profile>/<PluginName>_settings.json`.
- `SaveSettings(IPlugin)` / `LoadSettings(IPlugin)` handle per-plugin files; `SaveCoreSettings` / `LoadCoreSettings` handle the core file; `SaveSettingFile<T>` / `LoadSettingFile<T>` read/write an arbitrary typed JSON file.

### Serializer settings

All node-aware (de)serialization uses the shared `static readonly JsonSerializerSettings jsonSettings`:

```csharp
jsonSettings = new JsonSerializerSettings
{
    ContractResolver = new SortContractResolver(),
    Converters = new JsonConverter[]
    {
        new ColorNodeConverter(),
        new ToggleNodeConverter(),
        new FileNodeConverter()
    }
};
```

`SortContractResolver` orders serialized properties by inheritance depth then declared `Order`, keeping base-class members first and output stable.

### JSON converters

These Newtonsoft `CustomCreationConverter`s flatten nodes to scalars so settings files stay human-readable:

| Converter | Reads | Writes |
| --- | --- | --- |
| `ColorNodeConverter` | hex string → `ColorNode` (parsed as ABGR hex via `Color.FromAbgr`) | `ColorNode` → 8-digit ABGR hex (e.g. `"ff00ff00"`) |
| `ToggleNodeConverter` | `bool` → `ToggleNode` | `ToggleNode` → `bool` |
| `FileNodeConverter` | `string` → `FileNode` | `FileNode` → its string `Value` |

Other nodes (`RangeNode`, `ListNode`, `HotkeyNode`, `TextNode`, `StashTabNode`) serialize via their public properties, with `[JsonIgnore]` excluding runtime-only members (e.g. `RangeNode.Min`/`Max`, `ListNode.Values`).

---

## Complete example

A settings class combining several node types with a `[Menu]` group. (Declaration patterns adapted from real plugins; see [Source](#source).)

```csharp
using System.Collections.Generic;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using Color = SharpDX.Color;

namespace MyPlugin;

public class MyPluginSettings : ISettings
{
    // Required by ISettings. Special-cased: rendered by the core, not the menu.
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    // Auto-labelled "Pick Up Key" from the property name.
    public HotkeyNode PickUpKey { get; set; } = Keys.F;

    [Menu("Pickup range", "How far to scan for items, in game units.")]
    public RangeNode<int> PickupRange { get; set; } = new RangeNode<int>(600, 1, 1000);

    public ToggleNode IgnoreMoving { get; set; } = new ToggleNode(false);

    public ListNode Profile { get; set; } =
        new ListNode { Values = new List<string> { "global" }, Value = "global" };

    // --- Rendering group ---------------------------------------------------
    [Menu("Rendering", 100)]                 // group root
    public EmptyNode RenderingRoot { get; set; } = new EmptyNode();

    [Menu("Show overlay", 10, 100)]
    public ToggleNode ShowOverlay { get; set; } = new ToggleNode(true);

    [Menu("Highlight color", 20, 100)]
    public ColorNode HighlightColor { get; set; } = new Color(255, 215, 0, 255);

    [Menu("Reset rendering", 30, 100)]
    public ButtonNode ResetRendering { get; set; } = new ButtonNode();

    // Persisted, but hidden from the menu.
    [IgnoreMenu]
    public TextNode LastFilter { get; set; } = new TextNode();
}
```

Reading values is direct thanks to implicit conversions:

```csharp
if (!Settings.Enable) return;
if (Settings.PickUpKey.PressedOnce()) { /* ... */ }
int range = Settings.PickupRange;        // RangeNode<int> -> int
Color color = Settings.HighlightColor;   // ColorNode -> SharpDX.Color
```

Wire up button actions during plugin init:

```csharp
Settings.ResetRendering.OnPressed += () => ResetOverlay();
```

---

## Source

- `Core/Shared/Nodes/` — `ToggleNode.cs`, `RangeNode.cs`, `HotkeyNode.cs`, `ColorNode.cs`, `ButtonNode.cs`, `ListNode.cs`, `TextNode.cs`, `FileNode.cs`, `StashTabNode.cs`, `EmptyNode.cs`
- `Core/Shared/Nodes/ColorNodeConverter.cs`, `ToggleNodeConverter.cs`, `FileNodeConverter.cs`, `SortContractResolver.cs`
- `Core/Shared/Attributes/MenuAttribute.cs` (declares `MenuAttribute` and `IgnoreMenuAttribute`), `HideInReflectionAttribute.cs`
- `Core/Shared/Interfaces/ISettings.cs`, `ISettingsHolder.cs`
- `Core/SettingsParser.cs` (`SettingsParser`, `SettingsHolder`, `HolderChildType`), `Core/SettingsContainer.cs`
- `Core/CoreSettings.cs` — the framework's own `ISettings` implementation, a worked reference
- `Core/Shared/Enums/InventoryEnums.cs` — `InventoryTabFlags` (used by `StashTabNode`)
