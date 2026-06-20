using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing a monster health bar overlay.
/// </summary>
public class HPbarElement : Element
{
    /// <summary>
    /// Gets the monster entity this health bar belongs to.
    /// </summary>
    public Entity MonsterEntity => ReadObject<Entity>(Address + 0x96C);

    /// <summary>
    /// Gets the child health bar elements, typed as <see cref="HPbarElement"/>.
    /// </summary>
    public new List<HPbarElement> Children => GetChildren<HPbarElement>().Cast<HPbarElement>().ToList();
}
