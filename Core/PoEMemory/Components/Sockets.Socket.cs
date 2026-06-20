// Partial extension that restores a nested type missing from the modernized source.
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.Components;
partial class Sockets
{
    public class Socket
    {
        public SocketColor SocketColor;
        public int LinkGroup;
        public int SocketedGemIndex;
        public Entity SocketedGemEntity;
        public long GemAddress;
        public Socket(SocketColor socketColor, int linkGroup, int socketIndex, Entity gemEntity)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}