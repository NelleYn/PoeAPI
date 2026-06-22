namespace ExileCore.PoEMemory.MemoryObjects.Heist;

public class HeistChestRewardTypeRecord : RemoteMemoryObject
{
    public string Id => M.ReadStringU(M.Read<long>(Address));
    public string Art => M.ReadStringU(M.Read<long>(Address + 0x08));
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x10));
    public int MinimumDropLevel => M.Read<int>(Address + 0x28);
    public int MaximumDropLevel => M.Read<int>(Address + 0x2C);
    public int Weight => M.Read<int>(Address + 0x30);
    public string RoomName => M.ReadStringU(M.Read<long>(Address + 0x34));
    public int RequiredJobLevel => M.Read<int>(Address + 0x3C);

    // Offset 0x44 is correct, but TheGame.Files.HeistJobs is not present in this fork — returns null. Verify.
    public HeistJobRecord RequiredJob => null;

    public override string ToString() => Name;
}
