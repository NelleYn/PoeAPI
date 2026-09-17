namespace ExileCore.Shared.Enums
{
    /// <summary>
    /// Kind of entity, assigned from the metadata path by <c>Entity.GetEntityType</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// THE NUMERIC VALUES MATTER ACROSS A BOUNDARY, which is why a member cannot be added or removed
    /// here casually. A plugin is compiled against one <c>ExileCore.dll</c> and may run against
    /// another: the reference distribution's, or this fork's. Enum members are compile-time
    /// constants, so <c>EntityType.Portal</c> is baked into the plugin as a NUMBER, while
    /// <c>entity.Type</c> comes back as whatever number the host assigns. If the two enums disagree,
    /// every comparison past the point of disagreement silently matches the wrong kind — no
    /// exception, no log line, just a plugin that treats an area transition as a portal.
    /// </para>
    /// <para>
    /// A MEMBER WAS REMOVED HERE FOR EXACTLY THAT REASON. <c>SmallChest</c> used to sit between
    /// <see cref="Chest"/> and <see cref="Npc"/>, so every member after it was one higher than the
    /// reference's: this fork's <see cref="AreaTransition"/> was 105 against the reference's 104,
    /// <see cref="Breach"/> 114 against 113, <see cref="BetrayalChoice"/> 122 against 121, and so on
    /// for 27 members in total. The autopilot compares against twelve of them —
    /// <see cref="Monster"/>, <see cref="Player"/>, <see cref="Chest"/>, <see cref="Portal"/>,
    /// <see cref="MiscellaneousObjects"/>, <see cref="IngameIcon"/>, <see cref="AreaTransition"/>,
    /// <see cref="WorldItem"/>, <see cref="QuestObject"/>, <see cref="Item"/>, <see cref="Shrine"/>
    /// and <see cref="Npc"/> — and all but the first three sit after the removed member.
    /// </para>
    /// <para>
    /// REMOVING IT WAS SAFE BECAUSE NOTHING PRODUCED IT. <c>Entity.GetEntityType</c> classifies every
    /// chest, including small ones, as <see cref="Chest"/>; it never returned <c>SmallChest</c>, so
    /// no entity ever carried the value and no comparison against it could ever have been true. The
    /// one reference to it in the tree was <c>proposals/IconsBuilder</c>, which is source without a
    /// project file and is not built. <c>ChestType.SmallChest</c> is a DIFFERENT enum and is
    /// untouched — that one is produced and used.
    /// </para>
    /// </remarks>
    public enum EntityType
    {
        Error,
        None,
        ServerObject,
        Effect,
        Light,
        Monster = 100,
        Chest,
        Npc,
        Shrine,
        AreaTransition,
        Portal,
        QuestObject,
        Stash,
        Waypoint,
        Player,
        Pet,
        WorldItem,
        Resource,
        Breach,
        ControlObjects,
        HideoutDecoration,
        CraftUnlock,
        Daemon,
        TownPortal,
        Monolith,
        MiniMonolith,
        BetrayalChoice,
        IngameIcon,
        LegionMonolith,
        Item,
        Terrain,
        DelveCraftingBench,
        GuildStash,
        MiscellaneousObjects
    }
}
