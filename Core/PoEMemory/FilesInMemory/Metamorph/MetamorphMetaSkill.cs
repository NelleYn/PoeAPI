using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.FilesInMemory.Metamorph;

/// <summary>
/// A single metamorph meta skill record, resolving its monster variety and meta skill type.
/// </summary>
public class MetamorphMetaSkill : RemoteMemoryObject
{
    /// <summary>The monster variety this meta skill belongs to.</summary>
    public MonsterVariety MonsterVarietyMetadata => TheGame.Files.MonsterVarieties.GetByAddress(M.Read<long>(Address + 0x8));

    /// <summary>The meta skill type associated with this record.</summary>
    public MetamorphMetaSkillType MetaSkill => TheGame.Files.MetamorphMetaSkillTypes.GetByAddress(M.Read<long>(Address + 0x18));

    /// <summary>The skill's name.</summary>
    public string SkillName => M.ReadStringU(M.Read<long>(Address + 0xE8), 255);

    /// <summary>The first granted effect.</summary>
    public string GrantedEffect1 => M.ReadStringU(M.Read<long>(Address + 0x28, 0), 255);

    /// <summary>The second granted effect.</summary>
    public string GrantedEffect2 => M.ReadStringU(M.Read<long>(Address + 0x58, 0), 255);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{MetaSkill}, {MonsterVarietyMetadata?.VarietyId}";
    }
}
