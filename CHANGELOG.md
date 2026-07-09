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

