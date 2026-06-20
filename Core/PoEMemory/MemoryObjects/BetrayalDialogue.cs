using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory Betrayal dialogue record, exposing the target, job, upgrade and dialogue text.
/// </summary>
public class BetrayalDialogue : RemoteMemoryObject
{
    /// <summary>Gets the Betrayal target this dialogue refers to.</summary>
    public BetrayalTarget Target => TheGame.Files.BetrayalTargets.GetByAddress(M.Read<long>(Address + 0x8));

    /// <summary>Gets an unidentified integer field at offset 0x10.</summary>
    public int Unknown1 => M.Read<int>(Address + 0x10);

    /// <summary>Gets an unidentified integer field at offset 0x14.</summary>
    public int Unknown2 => M.Read<int>(Address + 0x14);

    /// <summary>Gets an unidentified integer field at offset 0x38.</summary>
    public int Unknown3 => M.Read<int>(Address + 0x38);

    /// <summary>Gets an unidentified boolean field at offset 0x6c.</summary>
    public bool Unknown4 => M.Read<byte>(Address + 0x6c) > 0;

    /// <summary>Gets an unidentified boolean field at offset 0x8d.</summary>
    public bool Unknown5 => M.Read<byte>(Address + 0x8d) > 0;

    /// <summary>Gets the Betrayal job associated with this dialogue.</summary>
    public BetrayalJob Job => TheGame.Files.BetrayalJobs.GetByAddress(M.Read<long>(Address + 0x44));

    /// <summary>Gets the Betrayal upgrade associated with this dialogue.</summary>
    public BetrayalUpgrade Upgrade => ReadObjectAt<BetrayalUpgrade>(0x64);

    /// <summary>Gets the dialogue text.</summary>
    public string DialogueText => M.ReadStringU(M.Read<long>(Address + 0xA6, 0x18));

    /// <summary>Gets the first set of related keys.</summary>
    public List<int> Keys1 => ReadKeys(0x20);

    /// <summary>Gets the second set of related keys.</summary>
    public List<int> Keys2 => ReadKeys(0x54);

    /// <summary>Gets the third set of related keys.</summary>
    public List<int> Keys3 => ReadKeys(0x85);

    private List<int> ReadKeys(long offset)
    {
        var addr = M.Read<long>(Address + offset);
        var result = new List<int>();

        if (addr != 0)
            for (long i = 0; i < 5; i++)
            {
                result.Add(M.Read<int>(addr + i * 0x8));
            }

        return result;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Target?.Name}, {Job?.Name}, {Upgrade?.UpgradeName}, {DialogueText}";
    }
}
