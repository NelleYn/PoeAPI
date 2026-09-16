using System;
using System.Collections.Generic;

namespace GameOffsets;

/// <summary>
/// Maps a component's OWN VTABLE — the qword at <c>[component + 0x00]</c>, expressed as an RVA
/// (absolute address minus the main module's base) — to the C# component type name, and back.
/// </summary>
/// <remarks>
/// <para>
/// WHY THE VTABLE, AND NOT THE LOOKUP TABLE'S nameId. The previous model resolved a component by
/// the 32-bit id stored beside it in the metadata's slot table (<see cref="ComponentSlot"/>). That
/// premise was REFUTED by measurement on a live client on 2026-09-16, and refuted IN BOTH
/// DIRECTIONS, which is why no amount of extra ids could have repaired it:
/// <list type="bullet">
/// <item><description>
/// ONE TYPE, DIFFERENT ids — not an oddity but the NORM. Counted across the five 2026-09-16 harvest
/// runs, THIRTEEN of the twenty-five named types carried more than one id: Actor (0x1C6/0x2C6),
/// BaseEvents (0x114/0x214/0x414 — three), Buffs (0x24B/0x34B), Functions (0x123/0x223),
/// InteractionAction (0x12E/0x22E), Life (0x15F/0x25F), Pathfinding (0x18F/0x28F),
/// Positioned (0x11C/0x21C), Render (0x101/0x201), StateMachine (0x156/0x256), Stats (0x1A7/0x2A7),
/// Targetable (0x177/0x277), Transitionable (0x163/0x263). Every difference is a MULTIPLE of 0x100
/// (BaseEvents skips one: 0x114, 0x214, 0x414), so the high bits carry something about the KIND of
/// owner and the low bits alone would have to be the type — but nothing measured says where that
/// boundary is, and guessing it is how this premise failed the first time. A single run sees only
/// some of the variants: the zone runs showed four such names, the town run ten. That is the
/// practical trap — a table built from one run looks consistent and is wrong elsewhere.
/// </description></item>
/// <item><description>
/// WHAT IT WOULD HAVE COST. Positioned and Life are on the hot path, and BOTH are in that list. On
/// the id model, a lookup for the grid position or the health of an entity whose owner kind uses the
/// high variant would have found nothing at all, silently, on some entities and not others.
/// </description></item>
/// <item><description>
/// ONE id, DIFFERENT types: 0x1D8 is Chest on <c>Metadata/Chests/*</c> and WorldItem on
/// <c>Metadata/MiscellaneousObjects/WorldItem</c>. The harvest run separated the two BY VTABLE and
/// the split is clean: 0x3465EC0 (Chest) appeared only on <c>Metadata/Chests/*</c>, on 7 entities,
/// and 0x3465DA8 (WorldItem) only on <c>Metadata/MiscellaneousObjects/WorldItem</c> — no entity
/// carried a vtable on the wrong side of that line. Resolving by id therefore does not merely lose a
/// component — it hands back a DIFFERENT component under the requested type, which is worse than
/// returning nothing.
/// </description></item>
/// </list>
/// The vtable has neither failure. In the earlier survey sample, 20 distinct nameIds produced 19
/// distinct vtables, and both collisions THERE are explained exactly by the cases above (0x114/0x214
/// share one vtable, 0x12E/0x22E share one vtable, and 0x1D8 splits into two vtables). Across all
/// five runs the vtable never moved: every name resolved to exactly ONE rva, DiesAfterTime came out
/// identical in two independent runs, and the survey's twelve pairs read back the same in three runs
/// of four — the fourth differing only because BaseEvents arrived as 0x214, which is this same id
/// collision and not a disagreement about the vtable.
/// Across that run every component NAME had exactly ONE vtable RVA — not one ambiguity in 25.
/// </para>
/// <para>
/// PROVENANCE. Measured on 2026-09-16 against a live client: PathOfExile_KG, pid 13660, module base
/// 0x7FF78FCD0000, zone "The Reliquary" (1_5_7, act 5, level 44, hash 0x57BE9BD9), 132-142 entities.
/// The whole chain was read IN ONE PROCESS and every snapshot was framed by an "area and IngameData
/// base did not change" check; snapshots showing a state change were discarded. The first pass took
/// the NAMES from the reference distribution (RefLive --harvest, its <c>CacheComp</c>), read the
/// VTABLES directly, and joined the two sources BY COMPONENT ADDRESS inside a single run — so no
/// pair rested on one source alone.
/// </para>
/// <para>
/// HOW THE TABLE GREW TO 25, later the same day. RefLive --harvest was taught to print the vtable
/// RVA next to the name it already knew, so the join by component address is no longer needed: one
/// invocation, <c>RefLive --harvest 200</c>, emitted every pair below in a SINGLE run over the same
/// zone. Two properties of that run are what make it usable as a source:
/// <list type="bullet">
/// <item><description>
/// NO AMBIGUITY. Every component name in the run carried exactly ONE vtable RVA. Had any name
/// appeared with two, the whole table would be back to the state the nameId is in, and none of it
/// could be believed.
/// </description></item>
/// <item><description>
/// AGREEMENT WITH THE EARLIER, INDEPENDENTLY-JOINED SOURCE. The 12 pairs the zone survey had already
/// established read the same, 12 out of 12; and all 19 pairs this table held before the run came
/// back with identical RVAs, which is why none of the numbers below changed — only six were added.
/// </description></item>
/// </list>
/// The six added by that run are AreaTransition, HideoutDoodad, MinimapIcon, PetAi, StateMachine and
/// TriggerableBlockage.
/// </para>
/// <para>
/// THE FOURTH PASS, 25 PAIRS TO 40, same client and same module base 0x7FF78FCD0000, taken in four
/// sittings of one day: "The Reliquary", dropped items, a fight, and a town. The join is still BY
/// COMPONENT ADDRESS — the name comes from the reference distribution's <c>CacheComp</c> (RefLive
/// --harvest now prints the RVA beside the name) and the vtable is read by this fork's own code at
/// <c>[component + 0x00]</c>. NEITHER the metadata name strings NOR the nameId take any part in that
/// join, which is what keeps the table independent of the thing it refutes. None of the 25 pairs
/// moved; five zone components were added (Brackets, DiesAfterTime, Functions, NPC, Portal), nine
/// item components (see the next paragraph), and Projectile, which is held separately because it was
/// NOT established this way — see <see cref="WeaklyAttributedPairs"/>.
/// </para>
/// <para>
/// WHY ITEM COMPONENTS NEEDED A SEPARATE PASS, and the fact worth more than the nine numbers: they
/// are NOT REACHABLE by walking the entity list at all. Armour, Base, LocalStats, Mods, Quality,
/// RenderItem, Sockets, Stack and Weapon live on the ITEM entity, which hangs off the ground entity
/// at <c>[worldItem + 0x28]</c> — that offset is now MEASURED, 14 out of 14 dropped items, and
/// confirmed by shape: the stride between neighbouring WorldItem components is 0xD0, so the same
/// field is found again at +0xF8 and +0x1C8, i.e. those hits are the neighbouring components, not
/// other fields. The item entity carries its OWN entity vtable, RVA 0x35E0358, which is NOT the
/// ground entity's 0x3456508 — and that is precisely why a search keyed on "the entity vtable" never
/// reaches it, and why standing in a zone full of loot did nothing for these nine. The sample was 14
/// dropped items — currency, a divination card, boots, an axe, a staff, a claw, a helmet — and the
/// distribution is itself a check on the names: Armour appeared only on armour, Weapon only on
/// weapons, Stack only on currency and divination cards, while Base and RenderItem were on every
/// item.
/// </para>
/// <para>
/// ONE COLLISION LEFT UNRESOLVED, AND DELIBERATELY NOT IN THE TABLE: vtable RVA 0x35DFB80. The
/// reference distribution calls it AttributeRequirements on armour and weapons but Usable on
/// currency. The join is by address and the component INSTANCES are different ones, so this is not a
/// mix-up of two objects: it is one vtable under two names. A vtable IS the type, therefore one of
/// the two names is wrong — and nothing available here says which, so entering either would be a
/// guess wearing a measurement's clothes. The cost of leaving it out is nil: this fork has no
/// <c>Usable</c> class at all, while a wrong name would make <c>HasComponent</c> answer TRUE for a
/// component the entity does not have, which is the one failure mode this whole model exists to
/// prevent. This is the SECOND time the reference distribution itself is the thing that errs; the
/// first was Chest and WorldItem sharing nameId 0x1D8, and there it was the vtable that separated
/// them. Also seen and deliberately unnamed: 0x35A5C68 (nameId 0x11E) on
/// <c>Metadata/Effects/Effect</c>, which the reference distribution never named.
/// </para>
/// <para>
/// WHAT THE FOURTH PASS COST THE nameId MODEL. A single combat run raised FOUR "one type, several
/// ids" collisions at once — BaseEvents 0x114/0x214, InteractionAction 0x12E/0x22E, Positioned
/// 0x11C/0x21C, Life 0x15F/0x25F — every one of them separated by exactly 0x100. Two of those
/// reproduce cases already on record; two are NEW, and they are the expensive ones: Positioned and
/// Life are HOT PATH components, and until this run they had only ever been seen in their "lower"
/// form. Under the nameId model, a fork that had only ever recorded 0x11C and 0x15F would silently
/// find no position and no health on whichever entities carry the upper form — no error, no empty
/// bucket, just a component that is there and is not found. The vtable table did not move across the
/// four passes: DiesAfterTime came out identical twice, and the survey's 12-of-12 pairs agreed in
/// three passes out of four — the fourth disagreed only in reading BaseEvents as 0x214, which is
/// this same collision and not a disagreement about the vtable.
/// </para>
/// <para>
/// THE CONTROL THAT MAKES THIS A MODEL AND NOT A LIST, over 50 entities of seven kinds: the
/// Positioned component found by vtable equalled the entity's direct Positioned pointer at
/// <c>entity + 0x98</c> 50 times out of 50; every component was identified on 49 of the 50 entities
/// (one entity carried one component that was not in its metadata's slot table at all); and the
/// resulting composition matched the reference distribution exactly — 12 components on the player,
/// 13 on a monster, 9 on a chest, 4 on a doodad.
/// </para>
/// <para>
/// RVA AND NOT AN ABSOLUTE ADDRESS, because the module is relocated on every launch. The absolute
/// vtable address measured in the run above is only valid for that one process; the RVA is the part
/// that survives. <c>IMemory.AddressOfProcess</c> is the base to subtract.
/// </para>
/// <para>
/// WHAT HAPPENS WHEN THE GAME IS PATCHED: every number here dies at once — and it dies LOUDLY, not
/// silently. An unrecognised vtable lands in the <see cref="UnknownKey"/> bucket, which no
/// <c>typeof(T).Name</c> can ever equal, so the failure shows up as "this entity has no components
/// we know" with the offending RVAs printed, never as a component resolved to the wrong type.
/// </para>
/// </remarks>
public static class ComponentVtables
{
    /// <summary>
    /// Prefix of every synthetic key produced for a vtable this table does not know. No C# type name
    /// can begin with this character, so such a key is unreachable through
    /// <c>typeof(T).Name</c> and cannot be mistaken for a resolved component by any caller.
    /// </summary>
    public const string UnknownPrefix = "?";

    /// <summary>
    /// Upper bound on a plausible RVA inside the game's main module. SHAPE CHECK, NOT A MEASUREMENT:
    /// the client's image is tens of megabytes (every vtable below sits under 0x3700000, i.e. ~55
    /// MiB) and this bound is deliberately loose. Its only job is to tell "a vtable inside the
    /// module that we have not measured yet" apart from "a qword that is not a module vtable at
    /// all" — the first is a gap in this table, the second means the pointer or the module base is
    /// wrong, and the two deserve different keys.
    /// </summary>
    public const long MaxPlausibleRva = 0x10000000L;

    /// <summary>
    /// THE SINGLE SOURCE OF TRUTH: C# component type name to vtable RVA, 39 pairs, every one of them
    /// established by joining the reference distribution's component name to a directly-read vtable
    /// ON THE COMPONENT'S ADDRESS. ADD A PAIR HERE ONLY WITH A MEASUREMENT BEHIND IT — and only with
    /// THAT measurement behind it. A pair attributed any other way goes in
    /// <see cref="WeaklyAttributedPairs"/>, so that the strength of a claim can be read off the
    /// table it sits in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// THE THREE BLOCKS ARE PROVENANCE, NOT DECORATION, which is also why the list is not one flat
    /// alphabetical run: the first block is the 25 pairs the earlier runs produced, unchanged to the
    /// bit by the fourth pass; the second is what the town and the fight added to it; the third is
    /// reachable only through <c>[worldItem + 0x28]</c> and says so by standing apart.
    /// </para>
    /// <para>
    /// BaseEvents, InteractionAction, PlayerClass, PetAi, Brackets, Functions and LocalStats have no
    /// <c>Component</c> class in this fork. They are listed anyway, because they DO occupy component
    /// slots on live entities, and naming them here is what keeps them out of the unknown bucket —
    /// which in turn keeps that bucket meaning exactly one thing: "a component type nobody has
    /// measured yet".
    /// </para>
    /// </remarks>
    private static readonly (string TypeName, long Rva)[] MeasuredPairs =
    {
        // ZONE ENTITIES, the 25 pairs of the earlier runs. The fourth pass re-read them and not one
        // number changed; they are reproduced here exactly as they were.
        ("Actor",                 0x346E558),
        ("Animated",              0x35A5030),
        ("AreaTransition",        0x346DE00),
        ("BaseEvents",            0x35A55E8),
        ("Buffs",                 0x359F5F8),
        ("Chest",                 0x3465EC0),
        ("HideoutDoodad",         0x34668A8),
        ("InteractionAction",     0x359F7D0),
        ("Inventories",           0x359E078),
        ("Life",                  0x35A7340),
        ("MinimapIcon",           0x35A7018),
        ("Monster",               0x35A6778),
        ("ObjectMagicProperties", 0x35A6AE0),
        ("Pathfinding",           0x35A6BA0),
        ("PetAi",                 0x35A6398),
        ("Player",                0x3466450),
        ("PlayerClass",           0x35A0078),
        ("Positioned",            0x35A0F18),
        ("Render",                0x3468920),
        ("StateMachine",          0x35DA8B8),
        ("Stats",                 0x35DD4E8),
        ("Targetable",            0x346C238),
        ("Transitionable",        0x35DD290),
        ("TriggerableBlockage",   0x35DCFE0),
        ("WorldItem",             0x3465DA8),

        // ZONE ENTITIES, added by the fourth pass. Brackets, Functions, NPC and Portal came from the
        // town, which is simply a place that contains entities "The Reliquary" did not; DiesAfterTime
        // came from the fight, and came out identically in two separate passes.
        ("Brackets",              0x359EC30),
        ("DiesAfterTime",         0x359F478),
        ("Functions",             0x359F288),
        ("NPC",                   0x3467218),
        ("Portal",                0x346C740),

        // ITEM COMPONENTS. These sit on the ITEM entity at [worldItem + 0x28], never on the
        // Metadata/MiscellaneousObjects/WorldItem entity the zone list hands out, and that item
        // entity has its own entity vtable (RVA 0x35E0358, not the ground entity's 0x3456508).
        // Sample: 14 dropped items. The comments record which kinds of item carried each one — a
        // component seen on every item of one kind and on no item of another is a check on the name,
        // not a note about loot.
        ("Armour",                0x363AD40),   // armour only
        ("Base",                  0x35E0168),   // every item
        ("LocalStats",            0x363AB88),
        ("Mods",                  0x35DF838),
        ("Quality",               0x363ACD8),
        ("RenderItem",            0x345EE58),   // every item
        ("Sockets",               0x35DF9A0),
        ("Stack",                 0x3634B10),   // currency and divination cards only
        ("Weapon",                0x363AB48),   // weapons only
    };

    /// <summary>
    /// THE ONE PAIR THAT IS NOT AS WELL ESTABLISHED AS THE REST, kept in its own table so that the
    /// weakness travels with the data instead of with a comment. It is merged into the same lookups
    /// as <see cref="MeasuredPairs"/> and resolves exactly like any other pair at run time; what
    /// differs is only what may be claimed about it, which <see cref="IsWeaklyAttributed"/> and
    /// <see cref="WeaklyAttributed"/> let a diagnostic state out loud.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WHAT IS WEAKER ABOUT IT. Every pair in <see cref="MeasuredPairs"/> rests on a join BY
    /// COMPONENT ADDRESS: the reference distribution named the component at address A and this fork
    /// read the vtable at that same address A. Projectile has no such join — three attempts in one
    /// fight and the reference distribution never caught a projectile alive. The RVA itself was read
    /// by this fork directly and is not in doubt; the NAME on it is, and it is attached by two weaker
    /// threads: the nameId 0x1CA, which the distribution reported in a different run, plus the
    /// correlation that this vtable turns up ONLY on
    /// <c>Metadata/Projectiles/ImpactingSteelProjectile</c> and <c>...Secondary</c>. The nameId is
    /// the very thing this class refutes as an identifier, so it is corroboration and not proof.
    /// </para>
    /// <para>
    /// WHY IT IS ENTERED AT ALL RATHER THAN LEFT PENDING. The coverage observed alongside it: across
    /// 1609 passes and 284312 entity sightings, exactly FOUR vtables went unidentified in total. With
    /// the table that close to closed, leaving Projectile out buys no safety — the same entities land
    /// in the unknown bucket either way, and a bucket that holds a component we can in fact name
    /// stops meaning "nobody has measured this". The honest position is to name it and to mark, here,
    /// how the name was attached.
    /// </para>
    /// <para>
    /// WHY A SEPARATE ARRAY, AND NOT A FLAG OR A COMMENT. A third tuple field would force a
    /// <c>false</c> onto all 39 lines whose provenance is not in question — rewriting the very lines
    /// that must be seen not to have changed, and burying the one line that IS in question among 39
    /// that are not. A trailing comment is worse still: it does not survive a line being copied, no
    /// test can assert it, and no diagnostic can print it. A separate array cannot be lost by
    /// accident, is countable, and makes promotion to full strength an explicit move of a line from
    /// one table to another — which is exactly the deliberate act that a real measurement deserves.
    /// </para>
    /// </remarks>
    private static readonly (string TypeName, long Rva)[] WeaklyAttributedPairs =
    {
        ("Projectile",            0x35A0D10),
    };

    /// <summary>
    /// THE WORK LIST: component types this fork asks for by name whose vtable has NOT been measured
    /// yet. Data and not a comment, so that a diagnostic can print it and the gap stays countable
    /// instead of remembered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IT GATES ENTITY CLASSIFICATION, so read an unexpected <c>EntityType</c> against this list
    /// before reading it as the model failing. <c>Entity.ParseType</c> asks for Chest, NPC, Monster,
    /// Shrine, WorldItem, Player, MinimapIcon, AreaTransition, Portal, Monolith, Transitionable,
    /// HideoutDoodad, ClientBetrayalChoice and RenderItem. Of those, ELEVEN are now measured — Chest,
    /// Monster, WorldItem, Player, Transitionable, MinimapIcon, AreaTransition, HideoutDoodad, plus
    /// NPC, Portal and RenderItem from the fourth pass — and THREE are still missing: Shrine,
    /// Monolith, ClientBetrayalChoice.
    /// </para>
    /// <para>
    /// WHAT THOSE THREE COST, stated as <c>EntityType</c> values that <c>ParseType</c> cannot produce
    /// no matter what the entity is: <c>Shrine</c> (Shrine), <c>Monolith</c> and <c>MiniMonolith</c>
    /// (Monolith), <c>BetrayalChoice</c> (ClientBetrayalChoice). The fourth pass turned four more
    /// loose: NPC unlocked <c>Npc</c>, Portal unlocked BOTH <c>Portal</c> and <c>TownPortal</c> (the
    /// latter inside the minimap-icon branch), and RenderItem unlocked <c>Item</c> — which is
    /// produced on the ITEM entity behind <c>WorldItem.ItemEntity</c>, not on the ground entity,
    /// since that one matches <c>HasComponent&lt;WorldItem&gt;()</c> first and stops at
    /// <c>WorldItem</c>. Already reachable before: MinimapIcon unlocked the whole minimap-icon branch
    /// (<c>IngameIcon</c>, <c>Waypoint</c>, <c>CraftUnlock</c>, <c>Stash</c>, <c>GuildStash</c>,
    /// <c>Breach</c>, <c>Resource</c>, <c>DelveCraftingBench</c>, <c>LegionMonolith</c>,
    /// <c>MiscellaneousObjects</c>), AreaTransition unlocked <c>AreaTransition</c>, and HideoutDoodad
    /// unlocked <c>HideoutDecoration</c>. Separately, and not a measurement gap at all:
    /// <c>SmallChest</c>, <c>QuestObject</c> and <c>ControlObjects</c> are never assigned by
    /// <c>ParseType</c> in the first place.
    /// </para>
    /// <para>
    /// WHY THE REST ARE STILL MISSING — TWO DIFFERENT REASONS, and they are not equally curable:
    /// <list type="bullet">
    /// <item><description>
    /// ABSENT FROM EVERY ZONE VISITED SO FAR. Shrine, Monolith and ClientBetrayalChoice: none of the
    /// four passes stood anywhere that contained one. That is the ONLY thing established about them —
    /// not that they are hard to reach, only that these zones did not hold them. A zone that does
    /// harvests them with no new technique, exactly as the town did for NPC and Portal.
    /// </description></item>
    /// <item><description>
    /// LOCATION NOT ESTABLISHED AT ALL. Charges and Flask: NOTHING has been measured about where they
    /// sit — not which entity carries them, not whether any entity seen so far carries them. They are
    /// asked for by Core's item code and by the consuming plugin, and that is the whole of what is
    /// known. Weapon and Sockets used to stand in this same group on the same expectation, and the
    /// fourth pass showed them to be item components; that is a reason to look for Charges and Flask
    /// on the item entity FIRST, and it is an expectation, not a result.
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// HOW TO FILL IT: for the first group, stand in a zone that contains the missing kinds and run
    /// <c>RefLive --harvest</c>, which prints the vtable RVA beside the component name it reads from
    /// the reference distribution. For the second, the walk must follow <c>WorldItem.ItemEntity</c>
    /// and scan THAT entity's component array — the zone entity list cannot reach an item component,
    /// whatever the zone. Every pair either produces moves into <see cref="MeasuredPairs"/> — and
    /// nowhere else, unless the name was attached without an address join, in which case it belongs
    /// in <see cref="WeaklyAttributedPairs"/>. The live fork's own <c>"?rva0x..."</c> keys name the
    /// same gaps from the other side, with the component address attached.
    /// </para>
    /// <para>
    /// This is NOT the full set of unmeasured types: everything in Core/PoEMemory/Components/ that is
    /// absent from <see cref="MeasuredPairs"/> and from <see cref="WeaklyAttributedPairs"/> is
    /// equally unmeasured and equally invisible. Two vtables are known and deliberately NOT named
    /// here: 0x35DFB80, where the reference distribution contradicts itself (AttributeRequirements
    /// against Usable), and 0x35A5C68 on <c>Metadata/Effects/Effect</c>, which it never named — see
    /// the remarks on this class. Neither is a pending MEASUREMENT; the number is in hand and it is
    /// the NAME that is missing, which is why neither appears in the list below.
    /// </para>
    /// </remarks>
    private static readonly string[] PendingMeasurementNames =
    {
        // Present in the game but absent from every zone visited in the four passes. A zone that
        // contains them harvests them unchanged — this is a travel problem, not a technique problem.
        "Shrine",                // needs a zone that has a shrine in it
        "Monolith",
        "ClientBetrayalChoice",

        // NOTHING IS MEASURED ABOUT WHERE THESE TWO LIVE. Not the entity that carries them, not
        // whether any entity seen so far carries them at all. Asked for by Core's item code and by
        // the consuming plugin, and that is the entire basis for their being named here. Weapon and
        // Sockets stood in this group on the same footing and turned out to be item components, so
        // the item entity behind [worldItem + 0x28] is where to look first — an expectation.
        "Charges",
        "Flask",
    };

    private static readonly Dictionary<string, long> ByTypeName;
    private static readonly Dictionary<long, string> ByRva;
    private static readonly HashSet<string> WeakTypeNames;
    private static readonly List<string> Conflicts = new List<string>();

    static ComponentVtables()
    {
        var total = MeasuredPairs.Length + WeaklyAttributedPairs.Length;

        ByTypeName = new Dictionary<string, long>(total, StringComparer.Ordinal);
        ByRva = new Dictionary<long, string>(total);

        WeakTypeNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pair in WeaklyAttributedPairs)
            WeakTypeNames.Add(pair.TypeName);

        // BOTH tables feed the SAME lookups, on purpose: a weakly attributed pair must resolve at run
        // time exactly like any other, or it would be a pending measurement wearing a different name.
        // The weak ones go in FIRST so that a name present in both tables ends up holding the strong
        // RVA — and, when the two disagree, is reported below rather than silently resolved either
        // way. The duplicate and cross-table checks are what make the split safe: they turn "somebody
        // copied the line across instead of moving it" from a silent state into a printed one.
        foreach (var table in new[] { WeaklyAttributedPairs, MeasuredPairs })
        {
            foreach (var pair in table)
            {
                // A duplicate would make this table lie in one direction while looking complete. It
                // is RECORDED rather than thrown: a throwing static constructor takes the whole
                // process down at type-load time, which is a far worse outcome than a diagnostic
                // line, and this type is loaded from inside the entity scan.
                if (ByTypeName.TryGetValue(pair.TypeName, out var existingRva) && existingRva != pair.Rva)
                    Conflicts.Add($"type {pair.TypeName} listed twice, RVA {existingRva:X} and {pair.Rva:X}");

                if (ByRva.TryGetValue(pair.Rva, out var existingName) && existingName != pair.TypeName)
                    Conflicts.Add($"RVA {pair.Rva:X} listed twice, types {existingName} and {pair.TypeName}");

                ByTypeName[pair.TypeName] = pair.Rva;
                ByRva[pair.Rva] = pair.TypeName;
            }
        }

        // A name that is BOTH measured and pending is the exact mistake a harvesting pass makes: the
        // pair gets added and the work list does not get trimmed. The result is a list that reports a
        // gap which no longer exists, and an unreliable work list is only a slower way of being wrong.
        // Recorded, never thrown, for the same reason as the duplicates above.
        foreach (var pending in PendingMeasurementNames)
        {
            if (ByTypeName.ContainsKey(pending))
                Conflicts.Add($"type {pending} is listed as measured AND as pending measurement");
        }

        // A name in BOTH tables is the mistake the split invites: promoting a pair by COPYING the
        // line instead of MOVING it. The two RVAs would usually agree, so the duplicate check above
        // would stay quiet, and the table would go on reporting as weakly attributed something it
        // now claims to have measured properly. The strength of a claim has exactly one home.
        foreach (var weak in WeaklyAttributedPairs)
        {
            foreach (var measured in MeasuredPairs)
            {
                if (string.Equals(weak.TypeName, measured.TypeName, StringComparison.Ordinal))
                    Conflicts.Add($"type {weak.TypeName} is listed as measured AND as weakly attributed");
            }
        }
    }

    /// <summary>
    /// The C# component type names this table can resolve — <see cref="MeasuredPairs"/> and
    /// <see cref="WeaklyAttributedPairs"/> together, because both resolve at run time. Ask
    /// <see cref="IsWeaklyAttributed"/> for how well a given name is established.
    /// </summary>
    public static IReadOnlyCollection<string> KnownTypeNames => ByTypeName.Keys;

    /// <summary>
    /// The component type names this fork asks for whose vtable has NOT been measured — see
    /// <see cref="PendingMeasurementNames"/>. Every name here resolves to "this entity does not have
    /// that component", whatever the entity actually carries.
    /// </summary>
    public static IReadOnlyList<string> PendingMeasurement => PendingMeasurementNames;

    /// <summary>
    /// The type names whose vtable RVA is known but whose NAME rests on something weaker than a join
    /// by component address — see <see cref="WeaklyAttributedPairs"/>. They resolve like any other
    /// pair; this is what lets a diagnostic say so instead of presenting every row as equal.
    /// </summary>
    public static IReadOnlyCollection<string> WeaklyAttributed => WeakTypeNames;

    /// <summary>
    /// Contradictions found inside <see cref="MeasuredPairs"/> and <see cref="WeaklyAttributedPairs"/>
    /// at type-load time. EMPTY IS THE EXPECTED STATE; anything here means the table itself is
    /// inconsistent and must be re-measured before anything built on it is believed.
    /// </summary>
    public static IReadOnlyList<string> TableConflicts => Conflicts;

    /// <summary>
    /// Number of pairs in the table: the size of what is known. <see cref="StrongCount"/> is the part
    /// of it that rests on a join by component address, and the difference between the two is
    /// <see cref="WeaklyAttributed"/>.
    /// </summary>
    public static int Count => MeasuredPairs.Length + WeaklyAttributedPairs.Length;

    /// <summary>
    /// Number of pairs established by joining the reference distribution's component name to a
    /// directly-read vtable ON THE COMPONENT'S ADDRESS — the full-strength claim.
    /// </summary>
    public static int StrongCount => MeasuredPairs.Length;

    /// <summary>
    /// Whether this type's name rests on something weaker than an address join — see
    /// <see cref="WeaklyAttributedPairs"/>. A caller that reports what the fork knows should say so;
    /// a caller that merely resolves components has no reason to ask.
    /// </summary>
    /// <param name="typeName">The component's C# type name, i.e. <c>typeof(T).Name</c>.</param>
    /// <returns>True when the pair is in the weakly attributed table.</returns>
    public static bool IsWeaklyAttributed(string typeName)
    {
        return typeName != null && WeakTypeNames.Contains(typeName);
    }

    /// <summary>
    /// Whether a value has the shape of an RVA inside the game's main module — see
    /// <see cref="MaxPlausibleRva"/>. A vtable that fails this is not a gap in this table; it means
    /// the component pointer or the module base is wrong.
    /// </summary>
    /// <param name="rva">Absolute vtable address minus the main module's base.</param>
    /// <returns>True when the value could be a module RVA.</returns>
    public static bool IsPlausibleRva(long rva)
    {
        return rva > 0 && rva < MaxPlausibleRva;
    }

    /// <summary>
    /// Looks up the C# type name behind a component vtable RVA.
    /// </summary>
    /// <param name="rva">Vtable read at <c>[component + 0x00]</c>, minus the main module's base.</param>
    /// <param name="typeName">The C# type name, when this vtable has been measured.</param>
    /// <returns>
    /// False when this vtable has never been measured. The caller MUST NOT fall back to any other
    /// identification: the lookup-table id is refuted (see this class's remarks) and guessing would
    /// return a different component's address under the requested type. Use <see cref="UnknownKey"/>
    /// instead, so the gap stays visible.
    /// </returns>
    public static bool TryGetTypeName(long rva, out string typeName)
    {
        return ByRva.TryGetValue(rva, out typeName);
    }

    /// <summary>
    /// Looks up the measured vtable RVA of a C# component type name.
    /// </summary>
    /// <param name="typeName">The component's C# type name, i.e. <c>typeof(T).Name</c>.</param>
    /// <param name="rva">The measured RVA, when one exists.</param>
    /// <returns>
    /// False when the type's vtable has not been measured. This is the ONLY encoding of "not
    /// measured" — there is no zero entry and no sentinel — and it is what lets a diagnostic tell
    /// "we cannot see this component type at all" apart from "this entity does not carry it".
    /// </returns>
    public static bool TryGetRva(string typeName, out long rva)
    {
        if (typeName == null)
        {
            rva = 0;
            return false;
        }

        return ByTypeName.TryGetValue(typeName, out rva);
    }

    /// <summary>
    /// Builds the dictionary key for a component whose vtable lies inside the module but is not in
    /// this table, e.g. <c>"?rva0x35A1234"</c>. It NAMES the gap instead of hiding it: the RVA is in
    /// the key and the component's address is the value, which is everything the next measuring pass
    /// needs.
    /// </summary>
    /// <param name="rva">The unmeasured vtable RVA.</param>
    /// <returns>A key that no <c>typeof(T).Name</c> can ever equal.</returns>
    public static string UnknownKey(long rva)
    {
        return UnknownPrefix + "rva0x" + rva.ToString("X");
    }

    /// <summary>
    /// Builds the dictionary key for a component whose vtable is not inside the main module at all,
    /// e.g. <c>"?vt0x7FF78F000000"</c>. Distinct from <see cref="UnknownKey"/> on purpose: this is
    /// not a missing measurement but a sign that the component pointer, or the module base, is
    /// wrong.
    /// </summary>
    /// <param name="vtable">The absolute vtable value as read.</param>
    /// <returns>A key that no <c>typeof(T).Name</c> can ever equal.</returns>
    public static string ForeignVtableKey(long vtable)
    {
        return UnknownPrefix + "vt0x" + vtable.ToString("X");
    }

    /// <summary>
    /// Whether a key came from <see cref="UnknownKey"/> or <see cref="ForeignVtableKey"/> rather
    /// than from a measured type name.
    /// </summary>
    /// <param name="key">A key taken from an entity's component map.</param>
    /// <returns>True for a synthetic key.</returns>
    public static bool IsUnknownKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key[0] == '?';
    }
}
