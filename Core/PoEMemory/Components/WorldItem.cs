using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the item entity contained within a ground/world item.
/// </summary>
/// <remarks>
/// <para>
/// THE +0x28 IS MEASURED NOW, not inherited. The offset came down from upstream and was taken on
/// faith; on 2026-09-16 it was measured against a live client on 14 dropped items — currency, a
/// divination card, boots, an axe, a staff, a claw, a helmet — and the pointer at
/// <c>[worldItem + 0x28]</c> led to the item entity on 14 of 14.
/// </para>
/// <para>
/// CONFIRMED BY SHAPE AS WELL AS BY COUNT, which is what rules out "some other field that happens to
/// hold an entity pointer". The stride between neighbouring WorldItem components is 0xD0, so the
/// SAME field turns up again at +0xF8 and +0x1C8 — those are the next components along, not further
/// fields of this one. That also settles what the component's size is: 0xD0.
/// </para>
/// <para>
/// THE ENTITY BEHIND THIS POINTER IS NOT LIKE THE ONE CARRYING THIS COMPONENT. It has its own entity
/// vtable, RVA 0x35E0358, where the ground entity's is 0x3456508, and it is the only place the item
/// components live — Base, RenderItem, Mods, Sockets, Quality, Stack, Armour, Weapon, LocalStats.
/// None of them can be reached by walking the zone entity list, however much loot is on the floor;
/// this pointer is the only way across. See <c>GameOffsets.ComponentVtables</c> for those pairs.
/// </para>
/// </remarks>
public class WorldItem : Component
{
    private readonly CachedValue<Entity> _cachedValue;

    /// <summary>Initializes a new instance of the <see cref="WorldItem"/> class.</summary>
    public WorldItem()
    {
        _cachedValue = new FrameCache<Entity>(() => Address != 0 ? ReadObject<Entity>(Address + 0x28) : null);
    }

    /// <summary>
    /// Gets the entity of the item lying on the ground — the one carrying the item components, NOT
    /// the <c>Metadata/MiscellaneousObjects/WorldItem</c> entity this component hangs on. See the
    /// remarks on this class for how the +0x28 was measured and why the distinction matters.
    /// </summary>
    public Entity ItemEntity => _cachedValue.Value;
}
