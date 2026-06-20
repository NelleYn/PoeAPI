using System.Text;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory;

/// <summary>
/// Base class for entity components read from game memory. Each component is
/// owned by an <see cref="Entity"/> whose address is stored at a fixed offset.
/// </summary>
public class Component : RemoteMemoryObject
{
    /// <summary>Gets the memory address of the <see cref="Entity"/> that owns this component.</summary>
    public long OwnerAddress => M.Read<long>(Address + 0x8);

    /// <summary>Gets the <see cref="Entity"/> that owns this component.</summary>
    public Entity Owner => ReadObject<Entity>(Address + 8);

    /// <summary>
    /// Builds a human-readable dump of every public property on this component,
    /// recursively describing nested <see cref="RemoteMemoryObject"/> values.
    /// </summary>
    /// <returns>A multi-line string with one entry per property.</returns>
    public string DumpObject()
    {
        var type = GetType();
        var propertyInfos = type.GetProperties();
        var strs = new StringBuilder();

        foreach (var propertyInfo in propertyInfos)
        {
            var value = propertyInfo.GetValue(this, null);

            if (value is RemoteMemoryObject)
            {
                strs.AppendLine($"{propertyInfo.Name} => {value}");
                strs.AppendLine($"ToString => {value.GetType().GetMethod("ToString").Invoke(this, null)}");
            }
            else
                strs.AppendLine($"{propertyInfo.Name} => {value}");
        }

        return strs.ToString();
    }
}
