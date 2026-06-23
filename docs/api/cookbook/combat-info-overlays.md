# Recipe: DPS, character info & HUD bars

Read combat/character data (`Stats`, `Actor.ActorSkills`, `Life`, `Player`) and turn it into
HUD overlays — DPS estimates, XP/level bars, elite-monster health bars, kill counters and
augmented item tooltips.

[API reference index](../README.md) · [cookbook index](README.md)

All snippets are adapted from real plugins and use only members that exist in this fork. Where
a plugin relies on upstream-only API, that is called out and linked to
[../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). For the
underlying types see [../components-combat.md](../components-combat.md) (`Stats`, `GameStat`,
`Actor`, `Life`, `Player`, `ActorSkill`), [../graphics.md](../graphics.md) (drawing) and
[../enums.md](../enums.md) (`GameStat`, `MonsterRarity`, `FontAlign`).

---

## 1. Per-skill DPS from `ActorSkill.Stats`

The engine already computes DPS-style numbers per skill and exposes them as `GameStat` keys on
each `ActorSkill`. `ActorSkill` has a ready-made `float Dps` and `int GetStat(GameStat)`, but
you can also read the raw `Stats` dictionary and pick whichever stat the game populated for
that skill (the value is "hundred times" the real number, so divide by 100). This is the core
of `SkillDPS`.

```csharp
// adapted from exApiTools/SkillDPS (SkillDpsCore.cs)
// Map the skill bar (server ids) to the matching ActorSkill, then read its DPS stat.
var ids = GameController.IngameState.ServerData.SkillBarIds;        // IList<ushort>
var actor = GameController.Player.GetComponent<Actor>();
if (ids == null || actor?.ActorSkills == null) return;

foreach (var skill in actor.ActorSkills)
{
    if (!ids.Contains(skill.Id)) continue;                          // only equipped skills

    decimal value;
    if (skill.Stats.TryGetValue(GameStat.HundredTimesDamagePerSecond, out var dps))
        value = dps / 100m;
    else if (skill.Stats.TryGetValue(GameStat.HundredTimesAverageDamagePerSkillUse, out var avg))
        value = avg / 100m;                                         // average-damage skills
    else if (skill.Stats.TryGetValue(GameStat.IntermediaryFireSkillDotDamageToDealPerMinute, out var dot))
        value = dot / 60m;                                          // DoT: per-minute -> per-second
    else
        continue;

    // value is this skill's DPS for the current configuration
}
```

`ActorSkill` also exposes the convenience members directly, so a simpler "active skill DPS"
read is:

```csharp
foreach (var skill in actor.ActorSkills)
    if (skill.IsOnSkillBar && skill.Dps > 0)
        Graphics.DrawText($"{skill.Name}: {skill.Dps:#,0}", pos, Color.White);
```

`Dps` is `GetStat(GameStat.HundredTimesDamagePerSecond + (IsUsing ? 4 : 0)) / 100f`, and
`HundredTimesAttacksPerSecond` switches to `HundredTimesCastsPerSecond` while casting — see the
`ActorSkill` table in [../components-combat.md](../components-combat.md).

> All of `HundredTimesDamagePerSecond`, `HundredTimesAttacksPerSecond`,
> `HundredTimesCastsPerSecond`, `HundredTimesAverageDamagePerSkillUse`,
> `IntermediaryFireSkillDotDamageToDealPerMinute` and `BaseSkillShowAverageDamageInsteadOfDps`
> exist as `GameStat` values in this fork. To anchor the label to the on-screen skill icon,
> read `GameController.IngameState.IngameUi.SkillBar` (a `SkillBarElement`) and use the child
> `Element` rectangle at the same index — see [../ui-elements.md](../ui-elements.md).

### Skip the label while a tooltip covers it

`SkillDPS` hides its overlay text when the hover tooltip overlaps the skill icon, so the
in-game tooltip stays readable:

```csharp
var hover = GameController.IngameState.UIHoverTooltip;
var iconRect = skillElement.GetClientRect();
if (hover != null && hover.IsVisibleLocal && hover.GetClientRect().Intersects(iconRect))
    continue; // don't draw over the tooltip
```

---

## 2. Damage meter from per-frame HP deltas

`DPSMeter` does not read a skill's DPS at all — it estimates *dealt* damage by watching every
nearby monster's HP fall frame to frame. The trick is `Entity.SetHudComponent` /
`GetHudComponent<T>()`: a plugin can attach an arbitrary object to an entity to remember its
last-seen life total.

```csharp
// adapted from exApiTools/DPSMeter (DpsMeter.cs)
public class CacheLife { public long Life { get; set; } }

public override void EntityAdded(Entity entity)
{
    if (!entity.HasComponent<Monster>() || !entity.IsHostile || !entity.IsAlive) return;
    var life = entity.GetComponent<Life>();
    if (life != null)
        entity.SetHudComponent(new CacheLife { Life = life.CurHP + life.CurES });
}

private void CalculateDps(out long aoeDamage, out long singleDamage)
{
    aoeDamage = 0; singleDamage = 0;
    foreach (var monster in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
    {
        var cache = monster.GetHudComponent<CacheLife>();
        var life = monster.GetComponent<Life>();
        if (cache == null || life == null) continue;

        var hp = monster.IsAlive ? life.CurHP + life.CurES : 0;
        if (cache.Life != hp)
        {
            var dmg = Math.Min(cache.Life - hp, life.MaxHP + life.MaxES); // clamp to pool
            aoeDamage   += dmg;                       // total drained this tick
            singleDamage = Math.Max(singleDamage, dmg); // biggest single hit
        }
        cache.Life = hp;
    }
}
```

Sum the last N ticks (`DPSMeter` keeps a 20-slot ring buffer and adds them up) to smooth the
number into a DPS reading. Accumulate `time += GameController.DeltaTime;` and only recompute
once `time` passes your update interval. For thread safety on the heavy loop, `DPSMeter` runs
`CalculateDps` as a `Job` via `GameController.MultiThreadManager.AddJob(...)` (see
[../utilities.md](../utilities.md)).

`DPSMeter` draws its readout into the left panel: it takes `GameController.LeftPanel.StartDrawPoint`
as the cursor, calls `GameController.LeftPanel.WantUse(() => Settings.Enable)` once in
`Initialise()`, and writes `StartDrawPoint` back after drawing so other panel plugins stack
below it. See §6 for the panel pattern.

---

## 3. XP / level bar from `Player`

The `Player` component carries `XP` (`uint`, total experience) and `Level` (`int`). The level
curve is not in memory, so `XpBar` ships a hard-coded experience table and computes the percent
into the current level.

```csharp
// adapted from exApiTools/XpBar (Core.cs) — ExpTable is the per-level cumulative XP array
private double GetExpPct(int level, uint exp)
{
    if (level >= 100) return 0;
    uint start = ExpTable[level - 1];
    uint next  = ExpTable[level];
    return (double)(exp - start) / (next - start) * 100;
}

public override void Render()
{
    var player = GameController.Player.GetComponent<Player>();
    if (player == null) return;

    var pct = GetExpPct(player.Level, player.XP);
    var text = $"{player.Level}: {Math.Round(pct, 3)}%";

    var size = Graphics.MeasureText(text, 20);
    var rect = GameController.Window.GetWindowRectangle();          // GameWindow
    var center = new Vector2(rect.X + rect.Width / 2, rect.Height - 15);

    var box = new RectangleF(center.X - 5 - size.X / 2, center.Y - size.Y / 2, size.X + 10, size.Y);
    Graphics.DrawBox(box, Color.Black);
    Graphics.DrawText(text, center, Color.White, FontAlign.Center);
}
```

`GameController.Window.GetWindowRectangle()` (the `GameWindow`) gives the overlay area so the
bar can be centered at the bottom of the screen. `Player.Strength` / `Dexterity` /
`Intelligence` / `PlayerName` are read the same way for a character-info panel (the approach in
DetectiveSquirrel's *Character Data*).

---

## 4. Elite / rare monster health bars

`EliteBar` draws a ranked list of health bars for nearby rare/unique monsters, colored by
rarity and updated off-thread. It also attaches a per-entity HUD component (see §2) that
re-reads `Life` each tick so `Render()` stays cheap.

```csharp
// adapted from exApiTools/EliteBar (EliteBar.cs)
public class EliteDrawBar
{
    public EliteDrawBar(Entity entity, Color color)
    { Entity = entity; Color = color; Name = entity.RenderName; }

    public Entity Entity { get; }
    public string Name { get; }
    public Color Color { get; }
    public int CurLife { get; private set; }
    public float PercentLife { get; private set; }
    public bool IsAlive { get; private set; }

    public void Update()
    {
        IsAlive = Entity.IsValid && Entity.IsAlive;
        var life = Entity.GetComponent<Life>();
        CurLife     = life == null ? 0 : life.CurHP + life.CurES;
        PercentLife = life == null ? 0 : CurLife / (float)(life.MaxHP + life.MaxES);
    }
}
```

Pick the bar color from `Entity.Rarity` (a `MonsterRarity`):

```csharp
var color = entity.Rarity switch
{
    MonsterRarity.White  => Settings.NormalMonster,
    MonsterRarity.Magic  => Settings.MagicMonster,
    MonsterRarity.Rare   => Settings.RareMonster,
    MonsterRarity.Unique => Settings.UniqueMonster,
    _                    => Color.LightGray,
};
entity.SetHudComponent(new EliteDrawBar(entity, color));
```

Render each bar by scaling a background rectangle by `PercentLife`:

```csharp
var bar = new RectangleF(Settings.X, Settings.Y + Settings.Space * index, Settings.Width, Settings.Height);
Graphics.DrawImage("healthbar_bg.png", bar, Color.Black);
bar.Width *= drawCmd.PercentLife;
Graphics.DrawImage("healthbar.png", bar, drawCmd.Color);
bar.Inflate(1, 1);
Graphics.DrawFrame(bar, Settings.BorderColor, 1);
Graphics.DrawText($"{drawCmd.Name} {drawCmd.CurLife:#,0} | {drawCmd.PercentLife * 100:0}%",
    new Vector2(bar.X, bar.Y), Settings.TextColor);
```

`EliteBar` optionally draws the same bar as a native ImGui `ProgressBar` instead of images, and
adds a directional arrow toward the monster computed from
`(entity.GridPos - GameController.Player.GridPos).GetPolarCoordinates(out var phi)` plus
`MathHepler.GetDirectionsUV(phi, distance)` (both in `Core/Shared/Helpers/MathHepler.cs`). The
ImGui path uses `Color.ToImguiVec4()` from `Core/Shared/Helpers/Extensions.cs`. For the ImGui
overlay-window pattern see [../graphics.md](../graphics.md).

---

## 5. Kill counter

`KillCounter` counts dead monsters per rarity, deduplicating by entity id and area hash so the
same corpse is never counted twice. It only counts entities that have `ObjectMagicProperties`
(i.e. real monsters with a rarity).

```csharp
// adapted from exApiTools/KillCounter (KillCounter.cs)
private void TickLogic()
{
    foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
    {
        if (entity.IsAlive) continue;            // only the freshly dead
        Count(entity);
    }
}

private void Count(Entity entity)
{
    var areaHash = GameController.Area.CurrentArea.Hash;
    var seen = _countedIds.GetValueOrDefault(areaHash) ?? (_countedIds[areaHash] = new HashSet<long>());

    if (!entity.HasComponent<ObjectMagicProperties>()) return;
    if (!seen.Add(entity.Id)) return;            // HashSet.Add returns false if already present

    var rarity = entity.Rarity;
    if (entity.IsHostile && rarity >= MonsterRarity.White && rarity <= MonsterRarity.Unique)
        _counters[rarity]++;
}
```

Reset `_countedIds` / `_counters` in `AreaChange(AreaInstance)` (carrying a running session
total forward), and render the per-rarity tally into the left panel using rarity colors. See
[../entities.md](../entities.md) for `Entity.Id` / `IsAlive` / `IsHostile` and
[../game-controller.md](../game-controller.md) for `Area.CurrentArea.Hash`.

---

## 6. Stacking text into the left panel

`DPSMeter`, `KillCounter` and similar HUD plugins draw into the shared left map panel rather
than at fixed screen coordinates, so multiple plugins line up automatically.

```csharp
public override bool Initialise()
{
    GameController.LeftPanel.WantUse(() => Settings.Enable);   // claim the panel once
    return true;
}

public override void Render()
{
    var pos = GameController.LeftPanel.StartDrawPoint;         // current cursor (right-aligned)
    var size = Graphics.DrawText("kills: 123", pos, Settings.TextColor, FontAlign.Right);
    pos.Y += size.Y;
    GameController.LeftPanel.StartDrawPoint = pos;             // hand the cursor to the next plugin
}
```

`StartDrawPoint` is reset to the panel's top each frame by the engine
(`Core/GameController.cs`); each plugin advances `Y` and writes it back. See
[../game-controller.md](../game-controller.md) (`LeftPanel`, `UnderPanel`) and
[../graphics.md](../graphics.md) (`DrawText` returning the rendered size for layout).

---

## 7. Augmenting the item tooltip + weapon DPS

`AdvancedTooltip` reads the hovered item and draws extra text around the game's tooltip:
affix tiers, item level, and a computed weapon DPS. The entry point is the hovered icon.

```csharp
// adapted from exApiTools/AdvancedTooltip (AdvancedTooltip.cs)
var icon = GameController.Game.IngameState.UIHover.AsObject<HoverItemIcon>();
if (icon is not { ToolTipType: not ToolTipType.None, ItemFrame: { } tooltip })
    return;

var item = icon.Item;                                   // the Entity behind the tooltip
var mods = item?.GetComponent<Mods>();
var tooltipRect = tooltip.GetClientRect();              // anchor extra text to the frame
```

`Mods.ItemMods` and `Mods.ItemLevel` feed the affix/ilvl annotations (see
[../components-items.md](../components-items.md)). The weapon-DPS estimate reads the `Weapon`
component plus quality and the item's local damage mods:

```csharp
if (item.TryGetComponent<Weapon>(out var weapon))
{
    var aSpd = 1000f / weapon.AttackTime;               // attacks/sec (AttackTime is ms)
    float physLo = weapon.DamageMin, physHi = weapon.DamageMax, physMult = 1f;

    if (item.TryGetComponent<Quality>(out var quality))
        physMult += quality.ItemQuality / 100f;         // quality boosts physical

    // ...add local_physical_damage_+% / added-damage mods, then:
    var pDps = (physLo + physHi) * physMult / 2f * aSpd;
    Graphics.DrawText($"{pDps:#.#} dps", new Vector2(tooltipRect.Right - 15, tooltipRect.Y), Color.White, FontAlign.Right);
}
```

> `Weapon` exposes `DamageMin`, `DamageMax`, `AttackTime` (ms) and `CritChance` in this fork
> (`Core/PoEMemory/Components/Weapon.cs`); `Quality.ItemQuality` is the quality percent. The
> per-stat damage rollup in `AdvancedTooltip` walks the item's mods and switches on the mod's
> string `Key` (e.g. `local_physical_damage_+%`, `local_minimum_added_fire_damage`) — that
> mod-record plumbing (`ModValue`, `GameController.Files.Mods` translation) is item-data work;
> see [../components-items.md](../components-items.md) and
> [../files-in-memory.md](../files-in-memory.md).

---

## 8. Gem leveling guidance (mostly upstream-only)

`GemGuide` and `PassiveSkillTreePlanter` give build guidance: which gems to socket, where to
buy them, and which passive nodes to take. The fork-friendly building blocks exist:

- `entity.GetComponent<SkillGem>()` — a socketed gem's `Level`, `MaxLevel`, `TotalExpGained`,
  `ExperienceToNextLevel`, `SocketColor` (`Core/PoEMemory/Components/SkillGem.cs`). Good for a
  "gem is N% to next level / can be leveled" badge.
- `entity.GetComponent<Sockets>()` — `Links`, `SocketGroup`, `NumberOfSockets`, `IsRGB`, and
  `SocketedGems` (each with `GemEntity` + `SocketIndex`). Enough to know what is socketed
  where.

```csharp
if (item.TryGetComponent<Sockets>(out var sockets))
    foreach (var socketed in sockets.SocketedGems)
        if (socketed.GemEntity.TryGetComponent<SkillGem>(out var gem) && gem.Level < gem.MaxLevel)
            { /* highlight a level-up candidate */ }
```

However, the bulk of `GemGuide`'s matching logic relies on API that does **not** exist in this
fork and is documented as upstream-only in
[../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md):

- The external **`ItemFilterLibrary`** (`ItemData`, `ItemData.StaticPlayerData.OwnedGems`,
  `GemInfo`) — not part of this repo.
- `Sockets.SocketInfoByLinkGroup`, the `SocketColor` enum, and `SkillGemDatSocketType` — this
  fork's `Sockets` exposes `Links` / `SocketGroup` / `SocketedGems` instead, and there is no
  `SocketColor` / `SkillGemDatSocketType` enum here.
- The static gem files `GemEffects` / `GemEffect` and `SkillGemDat(+SocketType)`, the
  `SkillGem.GemEffect` accessor, and the `QuestRewardWindow`
  (`GetPossibleRewards`) / `PurchaseWindow.TabContainer.VisibleStash` UI — all listed as
  absent in the compatibility doc (`PurchaseWindow` itself exists, but its `TabContainer`
  member tree does not).

`PassiveSkillTreePlanter` is almost entirely self-contained: it imports a tree from a PoB /
maxroll URL, decodes it, and highlights nodes — it leans on the in-game passive tree UI rather
than the combat/character API covered here, so port it against
[../ui-elements.md](../ui-elements.md) and the compatibility doc rather than this recipe.

Don't reimplement gem matching against invented members — start from the
[compatibility delta](../compatibility-exileapi-compiled.md) and the real `SkillGem` /
`Sockets` members above.

---

## Source repos

- [exApiTools/DPSMeter](https://github.com/exApiTools/DPSMeter) — `DpsMeter.cs` (per-frame HP-delta damage meter, `GetHudComponent`/`SetHudComponent`, left panel).
- [exApiTools/SkillDPS](https://github.com/exApiTools/SkillDPS) — `Core/SkillDpsCore.cs` (`ActorSkill.Stats` DPS by `GameStat`, skill-bar mapping, tooltip-overlap skip).
- [exApiTools/XpBar](https://github.com/exApiTools/XpBar) — `Core.cs` (`Player.XP`/`Level` + experience table, centered bottom bar).
- [exApiTools/EliteBar](https://github.com/exApiTools/EliteBar) — `EliteBar.cs` (rarity-colored `Life` bars, ImGui `ProgressBar`, direction arrow).
- [exApiTools/KillCounter](https://github.com/exApiTools/KillCounter) — `KillCounter.cs` (dedup kill tally by rarity, `ObjectMagicProperties`).
- [exApiTools/AdvancedTooltip](https://github.com/exApiTools/AdvancedTooltip) — `AdvancedTooltip.cs` (tooltip augmentation, `Weapon`/`Quality` DPS estimate).
- [exApiTools/GemGuide](https://github.com/exApiTools/GemGuide) — `GemGuide.cs` (gem socketing guidance; mostly upstream-only / `ItemFilterLibrary`).
- [exApiTools/PassiveSkillTreePlanter](https://github.com/exApiTools/PassiveSkillTreePlanter) — `PassiveSkillTreePlanter.cs` (tree import/highlight; UI-driven, outside this recipe's scope).

Cross-checked against this fork's `Core/`: `Stats`, `GameStat`, `Actor.ActorSkills`,
`ActorSkill` (`Dps`/`Stats`/`GetStat`), `Life`, `Player`, `Weapon`, `Quality`, `SkillGem`,
`Sockets`, `Graphics`, `GameController.LeftPanel`, `EntityListWrapper.ValidEntitiesByType`,
`MathHepler` and `Color.ToImguiVec4()`.
