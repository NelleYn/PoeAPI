using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Elements;
public class TabletTileElement : Element
{
    private LakeRoom _room;
    public int TileY => (int)(this + (long)this - 8);
    public int TileX => (int)(this + (long)this - 8);
    public int Difficulty => (int)(this + (long)this - 8);

    public LakeRoom Room
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}