// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.IconsBuilder.Icons;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using SharpDX;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Entity → icon factory. The reusable core of the library: for each game entity it decides which
/// concrete <see cref="BaseIcon"/> subclass (if any) represents it, and attaches that icon to the
/// entity as a HUD component via <see cref="Entity.SetHudComponent{T}"/>. A renderer (e.g.
/// <c>MinimapIcons</c>) then pulls those components back with <see cref="Entity.GetHudComponent{T}"/>
/// and draws them. This type can be hosted on its own as a settings plugin, or instantiated and
/// driven by another plugin (the original <c>MinimapIcons</c> drove it directly).
/// </summary>
public class IconsBuilder : BaseSettingsPlugin<IconsBuilderSettings>
{
    private readonly EntityType[] _chests = { EntityType.Chest, EntityType.SmallChest };

    private readonly EntityType[] _skippedEntity =
    {
        EntityType.WorldItem, EntityType.HideoutDecoration, EntityType.Effect, EntityType.Light, EntityType.ServerObject
    };

    private readonly List<string> _ignoreEntities = new List<string>
    {
        "Metadata/Monsters/Frog/FrogGod/SilverPool",
        "Metadata/MiscellaneousObjects/WorldItem",
        "Metadata/Pet/Weta/Basic",
        "Metadata/Monsters/Daemon/SilverPoolChillDaemon",
        "Metadata/Monsters/Daemon",
        "Metadata/Monsters/Frog/FrogGod/SilverOrbFromMonsters",
        "Metadata/Terrain/Labyrinth/Objects/Puzzle_Parts/TimerGears",
        "Metadata/Chests/DelveChests/DelveAzuriteVeinEncounter",
        "Metadata/Chests/DelveChests/DelveAzuriteVeinEncounterNoDrops"
    };

    /// <summary>
    /// Map of monster-mod name to a (column,row) cell on the shared <c>sprites.png</c> sheet. Hosts
    /// fill this from their own alert config; the ported icon classes read it to draw special icons
    /// for modded monsters. Empty by default.
    /// </summary>
    public Dictionary<string, Size2> ModIcons { get; } = new Dictionary<string, Size2>();

    private readonly Queue<Entity> _addedIcon = new Queue<Entity>(128);

    /// <inheritdoc/>
    public override void OnLoad()
    {
        Graphics.InitImage("sprites.png");
    }

    /// <inheritdoc/>
    public override bool Initialise()
    {
        Settings.Reparse.OnPressed += () =>
        {
            foreach (var entity in GameController.EntityListWrapper.Entities)
            {
                if (!entity.IsValid) continue;
                EntityAdded(entity);
            }
        };

        return true;
    }

    /// <inheritdoc/>
    public override void AreaChange(AreaInstance area)
    {
        _addedIcon.Clear();
        foreach (var entity in GameController.EntityListWrapper.Entities.Where(x => x.IsValid))
            _addedIcon.Enqueue(entity);
    }

    /// <inheritdoc/>
    public override void EntityAdded(Entity entity)
    {
        if (!Settings.Enable.Value) return;
        if (_skippedEntity.Any(x => x == entity.Type)) return;
        _addedIcon.Enqueue(entity);
    }

    /// <inheritdoc/>
    public override Job Tick()
    {
        if (!Settings.Enable.Value) return null;

        while (_addedIcon.Count > 0)
        {
            try
            {
                var entity = _addedIcon.Dequeue();
                var icon = EntityAddedLogic(entity);
                if (icon != null)
                    entity.SetHudComponent(icon);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"{nameof(IconsBuilder)} -> {ex}", 3);
            }
        }

        return null;
    }

    private bool SkipEntity(Entity entity)
    {
        if (entity.Type == EntityType.Daemon) return true;
        if (_ignoreEntities.Any(x => entity.Path.Contains(x))) return true;
        return false;
    }

    /// <summary>
    /// Substitute for the original's <c>entity.League == LeagueType.Delirium</c> dispatch check (see
    /// README): reads the <see cref="GameStat.AffectedByDelirium"/> stat the same way
    /// <see cref="LegionIcon"/> already reads <see cref="GameStat.MonsterMinimapIcon"/>.
    /// </summary>
    private static bool IsAffectedByDelirium(Entity entity)
    {
        var stats = entity.Stats;
        return stats != null && stats.TryGetValue(GameStat.AffectedByDelirium, out var affected) && affected != 0;
    }

    /// <summary>
    /// The core mapping: returns the icon that represents <paramref name="entity"/>, or null when the
    /// entity should have no icon. This is the reusable heart of the library.
    /// </summary>
    public BaseIcon EntityAddedLogic(Entity entity)
    {
        if (SkipEntity(entity)) return null;

        // Out-of-range replacement for entities that have an in-game minimap icon.
        if (Settings.UseReplacementsForGameIconsWhenOutOfRange &&
            entity.HasComponent<MinimapIcon>())
        {
            var minimapIconComponent = entity.GetComponent<MinimapIcon>();
            if (minimapIconComponent != null && !minimapIconComponent.IsHide &&
                !string.IsNullOrEmpty(minimapIconComponent.Name))
            {
                return new IngameIconReplacerIcon(entity, Settings);
            }
        }

        // Monsters.
        if (entity.Type == EntityType.Monster)
        {
            if (!entity.IsAlive) return null;

            if (entity.League == LeagueType.Legion)
                return new LegionIcon(entity, GameController, Settings, ModIcons);

            // This fork's LeagueType has no Delirium member (see README), so Delirium monsters are
            // recognised via the GameStat.AffectedByDelirium stat plus the Delirium-fog doodad-daemon
            // path prefix instead of entity.League == LeagueType.Delirium.
            if (IsAffectedByDelirium(entity) ||
                entity.Path.StartsWith("Metadata/Monsters/LeagueAffliction/DoodadDaemons", StringComparison.Ordinal))
                return new DeliriumIcon(entity, GameController, Settings, ModIcons);

            return new MonsterIcon(entity, GameController, Settings, ModIcons);
        }

        // NPCs.
        if (entity.Type == EntityType.Npc)
            return new NpcIcon(entity, GameController, Settings);

        // Players.
        if (entity.Type == EntityType.Player)
        {
            if (!entity.IsValid) return null;

            var localName = GameController.IngameState.Data.LocalPlayer.GetComponent<Player>()?.PlayerName;
            var thisName = entity.GetComponent<Player>()?.PlayerName;

            return localName != null && localName == thisName
                ? new SelfIcon(entity, GameController, Settings)
                : new PlayerIcon(entity, GameController, Settings);
        }

        // Chests/strongboxes.
        if (_chests.Any(x => x == entity.Type) && !entity.IsOpened)
            return new ChestIcon(entity, GameController, Settings);

        // Area transitions.
        if (entity.Type == EntityType.AreaTransition)
            return new MiscIcon(entity, GameController, Settings);

        // Shrines.
        if (entity.HasComponent<Shrine>())
            return new ShrineIcon(entity, GameController, Settings);

        if (entity.HasComponent<Transitionable>() && entity.HasComponent<MinimapIcon>())
        {
            // Mission markers.
            if (entity.Path.Equals("Metadata/MiscellaneousObjects/MissionMarker", StringComparison.Ordinal) ||
                (entity.GetComponent<MinimapIcon>()?.Name.Equals("MissionTarget", StringComparison.Ordinal) ?? false))
                return new MissionMarkerIcon(entity, GameController, Settings);

            return new MiscIcon(entity, GameController, Settings);
        }

        if ((entity.HasComponent<MinimapIcon>() && entity.HasComponent<Targetable>()) ||
            entity.Path.Contains("Metadata/Terrain/Leagues/Delve/Objects/EncounterControlObjects/AzuriteEncounterController") ||
            entity.Type == EntityType.LegionMonolith)
            return new MiscIcon(entity, GameController, Settings);

        return null;
    }
}
