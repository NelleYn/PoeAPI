# Recipe: debugging, scaffolding & data export

> Developer-facing techniques mined from the exApiTools dev plugins (DevTree, PluginTemplate, PluginUpdater, GameStatExporter): a reflection-based memory inspector you can embed in your own plugin, the minimal plugin scaffold, auto-updating plugins, and dumping the `GameStat`/stat tables to source. Everything here is adapted to this fork; members that only exist upstream are called out and linked to [`../compatibility-exileapi-compiled.md`](../compatibility-exileapi-compiled.md).

[API reference index](../README.md) · [cookbook index](README.md)

All fork types live in `ExileCore`, `ExileCore.PoEMemory*`, `ExileCore.Shared*`. ImGui is `ImGuiNET` (the same instance the engine renders — see [`../graphics.md`](../graphics.md)). `Color` is `SharpDX.Color` unless a snippet says `System.Drawing.Color`.

---

## 1. The minimal plugin scaffold

The `PluginTemplate` repo ships a `dotnet new` template (`templates/exApiPlugin/`) whose body is just the lifecycle hooks documented in [`../plugins.md`](../plugins.md). Stripped to essentials and adapted to this fork:

```csharp
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;   // AreaInstance
using ExileCore.Shared;                     // Job
using SharpDX;
using Vector2 = System.Numerics.Vector2;

public class MyPluginSettings : ExileCore.Shared.Interfaces.ISettings
{
    public ExileCore.Shared.Nodes.ToggleNode Enable { get; set; } = new(true);
}

public class MyPlugin : BaseSettingsPlugin<MyPluginSettings>
{
    public override bool Initialise() => true;                 // one-time setup
    public override void AreaChange(AreaInstance area) { }      // once per zone
    public override Job Tick() => null;                         // logic; return a Job to go off-thread
    public override void Render()                               // ImGui / Graphics
        => Graphics.DrawText($"{GetType().Name} is working.", new Vector2(100, 100), Color.Red);
}
```

A candidate fork port of this template is expected under `proposals/PluginTemplate/` (a sibling effort); prefer it once it lands.

Fork adaptation notes (verified against `Core/BaseSettingsPlugin.cs`):

- The upstream template loads custom config from a `ConfigDirectory` property. **This fork has no `ConfigDirectory`** — use `DirectoryFullName` (your plugin's own folder) and `Path.Combine` instead. See [`../compatibility-exileapi-compiled.md`](../compatibility-exileapi-compiled.md).
- `Job` is `ExileCore.Job` (`Core/MultiThreadManager.cs`); the off-thread idiom is `new Job("MyPluginMainJob", () => { /* work */ })`. Off-thread work and when it pays off are covered in [`../utilities.md`](../utilities.md).
- For how a plugin folder is discovered, compiled and hot-reloaded, see [`../../plugin-compiler.md`](../../plugin-compiler.md).

---

## 2. A DevTree-style debug window in your own plugin

DevTree's whole job is to walk a live object graph and render it as a collapsible ImGui tree, so you can click into any `RemoteMemoryObject`, read every field/property, and copy addresses. You can embed a small version of this in any plugin to inspect your own state.

### 2a. Seed the roots

DevTree keeps a `Dictionary<string, object>` of named roots and rebuilds it on load and on `AreaChange` (addresses move between zones). The roots are exactly the entry points documented across this reference — `GameController`, `GameController.Game`, `Player`, `IngameState`, `IngameState.IngameUi`, `IngameState.Data.ServerData`, inventories, ground labels:

```csharp
private readonly Dictionary<string, object> _roots = new();

private void InitObjects()
{
    _roots.Clear();
    Add(GameController, "GameController");
    Add(GameController.Player, "Player");
    Add(GameController.IngameState, "IngameState");
    Add(GameController.IngameState.IngameUi, "IngameState.IngameUi");
    Add(GameController.IngameState.Data.ServerData, "ServerData");
    Add(GameController.IngameState.IngameUi.ItemsOnGroundLabels, "GroundLabels");

    void Add(object o, string name)
    {
        if (o == null) { DebugWindow.LogError($"{Name}: can't add null root '{name}'."); return; }
        // For memory objects, tag the address into the node id so it stays unique.
        if (o is ExileCore.PoEMemory.RemoteMemoryObject rmo)
            name += $" ({rmo.Address:X})##root";
        _roots[name] = o;
    }
}

public override void AreaChange(AreaInstance area) => InitObjects();
```

`RemoteMemoryObject.Address` is the verified anchor of every game object ([`../memory.md`](../memory.md)). `DebugWindow.LogError` is the on-screen logger ([`../utilities.md`](../utilities.md)).

### 2b. The reflection renderer

The core is a recursive `Debug(object)` that classifies the value and renders accordingly. The shape below is condensed from DevTree's `DevTree.cs` / `Helpers.cs` but uses only fork-confirmed members:

```csharp
using System.Collections;
using System.Reflection;
using ImGuiNET;
using ExileCore.PoEMemory;
using ExileCore.Shared.Helpers;          // Color.ToImguiVec4()  (Core/Shared/Helpers/Extensions.cs)

private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.FlattenHierarchy;

public void Debug(object obj)
{
    if (obj == null) { ImGui.TextColored(SharpDX.Color.Red.ToImguiVec4(), "Null"); return; }

    var type = obj.GetType();

    // 1. Primitives / strings / enums -> one copyable line.
    if (IsSimpleType(type)) { CopyableText(obj.ToString()); return; }

    // 2. A RemoteMemoryObject at address 0 can't be read.
    if (obj is RemoteMemoryObject { Address: 0 })
    {
        ImGui.TextColored(SharpDX.Color.Red.ToImguiVec4(), "Address 0. Can't read this object.");
        return;
    }

    // 3. Collections -> bracketed count + per-item nodes.
    if (obj is ICollection collection) { DebugCollection(collection); return; }

    // 4. Everything else: show address, then Properties / Fields / Methods tabs.
    if (obj is RemoteMemoryObject rmo)
    {
        ImGui.Text("Address: "); ImGui.SameLine();
        CopyableText($"{rmo.Address:X}");
    }

    ImGui.BeginTabBar("tabs");
    if (ImGui.BeginTabItem("Properties")) { DebugProperties(obj, type); ImGui.EndTabItem(); }
    if (ImGui.BeginTabItem("Fields"))     { DebugFields(obj, type);     ImGui.EndTabItem(); }
    ImGui.EndTabBar();
}

private void DebugProperties(object obj, Type type)
{
    foreach (var p in type.GetProperties(Flags).Where(x => x.GetIndexParameters().Length == 0))
    {
        object value;
        try { value = p.GetValue(obj); }
        catch (Exception e) { ImGui.TextColored(SharpDX.Color.Red.ToImguiVec4(), $"{p.Name}: <threw> {e.Message}"); continue; }

        if (value == null) { ImGui.Text($"{p.Name}: "); ImGui.SameLine(); ImGui.TextColored(SharpDX.Color.Red.ToImguiVec4(), "Null"); }
        else if (IsSimpleType(p.PropertyType)) { ImGui.Text($"{p.Name}: "); ImGui.SameLine(); CopyableText(value.ToString()); }
        else if (ImGui.TreeNode($"{p.Name}##{p.DeclaringType.FullName}")) { Debug(value); ImGui.TreePop(); }
    }
}

private void DebugFields(object obj, Type type)
{
    foreach (var f in type.GetFields(Flags))
    {
        var value = f.GetValue(obj);
        if (IsSimpleType(f.FieldType)) { ImGui.Text($"{f.Name}: "); ImGui.SameLine(); CopyableText($"{value}"); }
        else if (ImGui.TreeNode($"{f.Name}##{type.FullName}")) { Debug(value); ImGui.TreePop(); }
    }
}
```

`IsSimpleType` is DevTree's own helper (`Helpers.cs`) — it short-circuits on primitives, enums, `Nullable<>`, strings and the common vector/color value types so they render inline instead of recursing forever:

```csharp
private static readonly HashSet<Type> Leaf =
[
    typeof(string), typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Guid),
    typeof(SharpDX.Vector2), typeof(SharpDX.Vector3), typeof(SharpDX.Vector4),
    typeof(System.Numerics.Vector2), typeof(System.Numerics.Vector3),
];

public static bool IsSimpleType(Type t) =>
    t.IsPrimitive || t.IsEnum || Leaf.Contains(t) ||
    Convert.GetTypeCode(t) != TypeCode.Object ||
    (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(t.GetGenericArguments()[0]));
```

A "copy on click" leaf is the one piece of polish worth stealing — every value becomes a borderless button that writes itself to the clipboard:

```csharp
private static void CopyableText(string text)
{
    if (ImGui.SmallButton(text))
        ImGui.SetClipboardText(text);
}
```

### 2c. Collections, with a per-item filter

DevTree caps how many items it renders (`Settings.LimitForCollections`) and offers a skip/filter box, which matters because some game collections are huge. The fork-safe core:

```csharp
private int _skip;
private string _search = "";

private void DebugCollection(ICollection collection)
{
    ImGui.TextColored(SharpDX.Color.OrangeRed.ToImguiVec4(), $"[{collection.Count}]");
    if (collection.Count == 0) return;

    ImGui.InputInt("Skip", ref _skip);
    ImGui.SameLine();
    ImGui.InputTextWithHint("##filter", "Filter", ref _search, 200);

    var i = 0;
    foreach (var item in collection.Cast<object>()
                 .Where(x => string.IsNullOrEmpty(_search) ||
                             (x?.ToString()?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false))
                 .Skip(_skip).Take(50))   // hard cap; DevTree uses Settings.LimitForCollections
    {
        var label = item switch
        {
            Entity e => e.Path,
            Element { Text.Length: > 0 } el => el.Text,
            _ => item?.GetType().Name ?? "Null",
        };
        if (ImGui.TreeNode($"[{i}] {label}##{i}")) { Debug(item); ImGui.TreePop(); }
        i++;
    }
}
```

### 2d. Entity components (the `GetComponent` reflection trick)

The most useful DevTree-specific idea: an `Entity` exposes its components by **name** (`Entity.CacheComp` is a `name -> address` map), but `GetComponent<T>()` is generic. DevTree bridges the two by resolving the component name to a `Type`, building a closed `GetComponent<T>` via reflection, and caching the `MethodInfo`. Adapted:

```csharp
private static readonly MethodInfo GetComponentMethod = typeof(Entity).GetMethod("GetComponent");
private readonly Dictionary<string, MethodInfo> _genericCache = new();

private void DebugEntityComponents(Entity e)
{
    if (e.CacheComp == null) return;
    foreach (var (compName, compAddr) in e.CacheComp)
    {
        // Component types live next to Positioned; reuse its assembly-qualified name as a template.
        var qualified = typeof(ExileCore.PoEMemory.Components.Positioned).AssemblyQualifiedName!
            .Replace(nameof(ExileCore.PoEMemory.Components.Positioned), compName);
        var compType = Type.GetType(qualified);
        if (compType == null) { ImGui.Text($"{compName}: not implemented ({compAddr:X})"); continue; }

        if (!_genericCache.TryGetValue(compName, out var generic))
            _genericCache[compName] = generic = GetComponentMethod.MakeGenericMethod(compType);

        var component = generic.Invoke(e, null);
        if (ImGui.TreeNode(compName)) { Debug(component); ImGui.TreePop(); }
    }
}
```

Verify `Entity.CacheComp` and `GetComponent<T>` against [`../entities.md`](../entities.md); the component types this resolves are the ones in [`../components-combat.md`](../components-combat.md), [`../components-items.md`](../components-items.md) and [`../components-world.md`](../components-world.md).

### 2e. Highlighting `Element`s while you browse

When a tree node for an `Element` is hovered, DevTree draws its on-screen rectangle so you can see what you're inspecting. This uses only fork-confirmed `Graphics`/`Element` members ([`../graphics.md`](../graphics.md), [`../ui-elements.md`](../ui-elements.md)):

```csharp
if (value is Element { Width: > 0, Height: > 0 } el && ImGui.IsItemHovered())
    Graphics.DrawFrame(el.GetClientRectCache, SharpDX.Color.Yellow, 2);
```

### Upstream-only DevTree features (do not copy verbatim)

These pieces of DevTree rely on members **absent from this fork**; if you need them, check [`../compatibility-exileapi-compiled.md`](../compatibility-exileapi-compiled.md) first:

- `GameController.RegisterInspector(...)` — DevTree registers its `Inspect(object, name, window)` callback so other plugins can pop an inspector. **Not present in `Core/GameController.cs`.** Expose your own public `Debug(object)` method instead.
- The `GetAddress(hideAddresses)` and `ToHexString()` calls are DevTree's own extension methods (`Extensions.cs`), not Core API — use `rmo.Address` and `$"{x:X}"`.
- The generic `M.ReadMem<long>(addr, count)` offset-scanner is upstream-only. The fork equivalent is `IMemory.ReadPointersArray(start, end, offset)` ([`../memory.md`](../memory.md)).
- The world-space pin drawing (`Graphics.DrawFilledCircleInWorld`, `DrawLineInWorld`, `DrawTextWithBackground`, `SetTextScale`) is **not in this fork's `Core/Graphics.cs`**. Do `Camera.WorldToScreen(pos)` yourself and draw with the screen-space `DrawText`/`DrawLine`/`DrawFrame` documented in [`../graphics.md`](../graphics.md).
- The "Custom expression" Roslyn console (`Microsoft.CodeAnalysis.Scripting` + a private `AssemblyLoadContext`) and `ImGuiHelpers.UseStyleColor` are DevTree/external, not Core.

The lighter, non-scripting `TC_DevTree` variant of the same plugin compiles against `ExileCore` with none of the Roslyn/expression machinery and is a cleaner starting point if you only want the tree renderer.

---

## 3. Dumping `GameStat` and other enum tables (GameStatExporter)

`GameStatExporter` regenerates the `GameStat` enum source straight from the live `Stats.dat` table, so the enum never drifts from the client. The fork already ships the generated [`GameStat`](../enums.md) enum (`Core/Shared/Enums/GameStat.cs`, ~10k members), but the exporter is the tool that produces it — handy when the game patches in new stats.

The data source is `GameController.Files.Stats` (`StatsDat`, verified in `Core/PoEMemory/FilesInMemory/StatsDat.cs`). Its `records` dictionary is keyed by the stat `Key`, and each `StatRecord` carries `Key`, `UserFriendlyName` and a sequential `ID` — exactly what the enum needs:

```csharp
using System.Text;
using ExileCore;

// Wire a ButtonNode in your settings (Core/Shared/Nodes/ButtonNode.cs):
//   public ButtonNode ExportButton { get; set; } = new();
// then in Initialise(): Settings.ExportButton.OnPressed += Export;

private void Export()
{
    var sb = new StringBuilder();
    sb.AppendLine("namespace ExileCore.Shared.Enums;");
    sb.AppendLine();
    sb.AppendLine("public enum GameStat");
    sb.AppendLine("{");

    var seen = new Dictionary<string, int>();
    foreach (var (key, rec) in GameController.Files.Stats.records)   // StatsDat.records
    {
        var descr = string.IsNullOrEmpty(rec.UserFriendlyName) ? rec.Key : rec.UserFriendlyName;
        sb.AppendLine($"\t/// <summary>{descr}</summary>");

        var name = FormatName(key);
        if (seen.TryGetValue(name, out var n)) { name += ++n; seen[name] = n; }  // de-dup
        else seen[name] = 1;

        sb.AppendLine($"\t{name} = {rec.ID},");   // ID is the sequential stat index
    }

    sb.AppendLine("}");
    File.WriteAllText(Settings.SavePath.Value, sb.ToString());
}

// Turn a raw stat key into a C# identifier: "+%foo_bar" -> "PctFooBar"
private static string FormatName(string name)
{
    var s = name.Replace("%", "pct").Replace("+", "").Replace("-", "")
                .TrimStart('0','1','2','3','4','5','6','7','8','9');
    return System.Globalization.CultureInfo.InvariantCulture.TextInfo
        .ToTitleCase(s).Replace("_", "");
}
```

`ButtonNode.OnPressed` is the fork's click hook ([`../settings.md`](../settings.md)). Unhook it in `OnPluginDestroyForHotReload` so hot-reload doesn't leave a dead delegate (`Settings.ExportButton.OnPressed -= Export;`). The same pattern dumps any other `*.Dat` table reachable from `GameController.Files` ([`../files-in-memory.md`](../files-in-memory.md)) to source.

`MercScanner` is a related read-only example: it reads mercenary `Stats`/components and renders them as an overlay, a smaller template for "scan a panel and print its parsed data" debugging.

---

## 4. Auto-updating plugins (PluginUpdater)

`PluginUpdater` manages git-backed plugin folders with [LibGit2Sharp](https://github.com/libgit2/libgit2sharp): it enumerates plugin directories, reads each repo's `Head`/`TrackedBranch` to compute behind/ahead counts, and exposes fetch/pull/force-reset/clone/branch-switch as async `Task`s driven from an ImGui table. The reusable ideas:

- **Discover git plugins** by scanning the plugin folder for sub-dirs containing a `.git` directory, then `new Repository(path)` each one.
- **Report status** from `repo.Head.Tip.Sha[..7]`, `repo.Head.TrackedBranch.Tip`, and `repo.Head.TrackingDetails.{AheadBy,BehindBy}`.
- **Update** = `Commands.Fetch` + `Commands.Pull`; **force-update** = `repo.Reset(ResetMode.Hard, trackedBranch.Tip)`; **revert** = reset to `Head.Tip.Parents.First()`.
- **Credentials** are resolved by shelling out to `git credential fill`, so private repos reuse the user's existing git config rather than storing secrets.

Heavy fork caveats — treat this as inspiration, not a drop-in:

- The repo targets **`ExileCore2`** (the PoE2 fork), not this `ExileCore`. The namespaces in its source are `ExileCore2.*`.
- It calls `PluginManager.SourcePluginDirectoryPath` and `PluginManager.ResolvePluginDirectory(...)`. **This fork's `Core/Shared/PluginManager.cs` exposes neither** — it has `RootDirectory` and per-plugin `DirectoryFullName` instead. Adapt the directory discovery accordingly and see [`../compatibility-exileapi-compiled.md`](../compatibility-exileapi-compiled.md).
- LibGit2Sharp is an external NuGet dependency you must add to your plugin's `.csproj`; how third-party references are resolved at compile time is covered in [`../../plugin-compiler.md`](../../plugin-compiler.md).

---

## Source repos

- [exApiTools/DevTree](https://github.com/exApiTools/DevTree) — reflection memory/object inspector; the `Debug`/`DebugCollection`/component-reflection patterns in §2.
- [exApiTools/TC_DevTree](https://github.com/exApiTools/TC_DevTree) — lighter `ExileCore` variant of DevTree without the Roslyn expression console.
- [exApiTools/PluginTemplate](https://github.com/exApiTools/PluginTemplate) — the `dotnet new` minimal plugin scaffold in §1.
- [exApiTools/PluginUpdater](https://github.com/exApiTools/PluginUpdater) — LibGit2Sharp auto-updater (§4); **targets ExileCore2**.
- [exApiTools/GameStatExporter](https://github.com/exApiTools/GameStatExporter) — generates the `GameStat` enum from `Stats.dat` (§3).
- [exApiTools/MercScanner](https://github.com/exApiTools/MercScanner) — small read-only stat-overlay example referenced in §3.
