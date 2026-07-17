## 2026-07-17

### Other

- `cd9ed62` Port master-compatible types from the reconstruction: ActionOverlay, PluginAssemblyLoadContext, ImGuiHelpers scopes
- `2ae08e2` Resolve the last unfinished API leftovers: unresolved UI offsets and stale docs


## 2026-07-14

### Other

- `12ac887` Graphics: add DrawCircle (segmented ring via DrawLine)
- `74de4e7` Expose ExileApi-Compiled API surface used by movement/helper plugins


## 2026-07-12

### Documentation

- `96f5b21` docs(changelog): patch notes 2026-07-10 [skip ci]


## 2026-07-10

### Fixes

- `63f05b2` Core/Shared/SomeMagic: fix CS0121 GetBytes ambiguity for sbyte/byte on .NET 10
- `47210d3` Fix Loader AppForm tray menu: migrate legacy ContextMenu/MenuItem to ContextMenuStrip/ToolStripMenuItem
- `9bd9176` Fix ThreadUnit.ForceAbort crashing on .NET 10 by replacing Thread.Abort with a cooperative stop
- `59fd739` Fix SettingsContainer first-run null CoreSettings and write-lock leaks
- `f9e8ec8` Fix Runner skipping the coroutine shifted into a removed slot
- `344f2aa` Remove missing git-ignored plugin projects from ExileApi.sln

### Other

- `c67710a` Promote Compat helpers from proposals/ into Core/Shared/Compat
- `dbee5eb` Resolve code TODOs: implement ActorSkill.IsMine, document three stale markers

### Documentation

- `e4e29b0` docs(architecture): sln no longer lists plugin projects

## 2026-07-09

### Documentation

- `7641b13` docs(BaseTreeRoutine): verify no Buffs/TryGetComponent workaround remains
- `e4e7c9f` docs: sync CHANGELOG and plugin-usage-index with PR #42

### Other

- `18dc334` proposals/ItemFilterLibrary: wire up Map/Charges/AttributeRequirements/SkillGem/Weapon/Armour
- `59b6e1b` proposals/IconsBuilder: port DeliriumIcon, add Heist chest handling
- `64a823f` proposals/Compat: implement 3 previously-omitted compat shims
- `f20c834` Core cleanup: resolve SkillGem.Level cast TODO, remove dead GetSlot(), clarify InventoryTabType placeholders
- `bb2573d` Fix dead code in BaseIcon constructor: restore throw on null args
- `b4ae448` Fix PluginWrapper coroutine filter discarding Where() result


# Changelog

## 2026-07-08

### Other

- `39e7b76` Implement ItemStats.ParseSockets using existing socket offsets
- `006ce81` Fix HotReloadDll debounce and remove stale IPlugin TODO
- `b9e0791` Add ProphecyChainDat wrapper
- `bf1a509` Add PosNum/GridPosNum accessors and UIHoverElement alias
- `668bb93` Add ReadStdVector<T> generic memory reader
- `b650cc6` Implement Memory.cs pointer-read overloads

## 2026-07-06

### Other

- `dd5d483` Add proposals/BaseTreeRoutine: TreeSharp behaviour-tree port candidate
- `57e16b9` proposals: add ItemFilterLibrary port candidate
- `5fa9407` Fix 10 broken cookbook cross-links in plugin-catalog.md

