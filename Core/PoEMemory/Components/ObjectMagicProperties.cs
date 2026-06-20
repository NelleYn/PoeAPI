using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;
using SharpDX;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the rarity and modifier list of a magic/rare monster or object.
/// </summary>
public class ObjectMagicProperties : Component
{
    private readonly CachedValue<ObjectMagicPropertiesOffsets> _cachedValue;
    private long _modsHash;
    private readonly List<string> modsList = new List<string>();

    /// <summary>Initializes a new instance of the <see cref="ObjectMagicProperties"/> class.</summary>
    public ObjectMagicProperties()
    {
        _cachedValue = new FrameCache<ObjectMagicPropertiesOffsets>(() => M.Read<ObjectMagicPropertiesOffsets>(Address));
    }

    /// <summary>Gets the raw magic properties offsets struct (cached per frame).</summary>
    public ObjectMagicPropertiesOffsets ObjectMagicPropertiesOffsets => _cachedValue.Value;

    /// <summary>Gets the rarity of the object.</summary>
    public MonsterRarity Rarity
    {
        get
        {
            if (Address != 0)
            {
                return (MonsterRarity) ObjectMagicPropertiesOffsets.Rarity;
            }

            return MonsterRarity.Error;
        }
    }

    /// <summary>Gets a hash of the object's modifiers, useful for caching.</summary>
    public long ModsHash => ObjectMagicPropertiesOffsets.Mods.GetHashCode();

    /// <summary>Gets the list of modifier names on the object (cached until the mods hash changes).</summary>
    public List<string> Mods
    {
        get
        {
            if (Address == 0) return null;

            if (_modsHash == ModsHash) return modsList;

            var begin = ObjectMagicPropertiesOffsets.Mods.First;
            var end = ObjectMagicPropertiesOffsets.Mods.Last;
            if (begin == 0 || end == 0) return new List<string>();
            var j = 0;

            for (var i = begin; i < end; i += 0x28)
            {
                var read = M.Read<long>(i + 0x20, 0);
                var mod = Cache.StringCache.Read($"{nameof(ObjectMagicProperties)}{read}", () => M.ReadStringU(read));
                modsList.Add(mod);
                j++;

                if (j > 256)
                {
                    DebugWindow.LogMsg($"{nameof(ObjectMagicProperties)} read mods error address", 2, Color.OrangeRed);
                    break;
                }
            }

            _modsHash = ModsHash;
            return modsList;
        }
    }
}
