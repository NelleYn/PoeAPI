using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.FilesInMemory.Metamorph;

/// <summary>
/// A single metamorph meta skill type record, resolving its associated base item type.
/// </summary>
public class MetamorphMetaSkillType : RemoteMemoryObject
{
    /// <summary>The meta skill type's internal id.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address + 0x0), 255);

    /// <summary>The meta skill type's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x8), 255);

    /// <summary>The meta skill type's description.</summary>
    public string Description => M.ReadStringU(M.Read<long>(Address + 0x10), 255);

    /// <summary>The base item type associated with this meta skill type.</summary>
    public BaseItemType BaseItemType => TheGame.Files.BaseItemTypes.GetFromAddress(M.Read<long>(Address + 0x38));

    /// <summary>The body part associated with this meta skill type.</summary>
    public string BodyPart => M.ReadStringU(M.Read<long>(Address + 0x40), 255);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{BodyPart}, {Id}, {Name}, {BaseItemType?.BaseName}, {Description}";
    }
}