using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Elements.Necropolis;
public class NecropolisMonsterPanelMonsterAssociation : Element
{
    private const long PackOffset = 576L;
    private const long PackFrequencyOffset = 624L;
    private const long MinMonstersOffset = 612L;
    private const long MaxMonstersOffset = 616L;
    private const long ModIndexOffset = 616L;
    public Element MonsterPortrait => (Element)0;
    public Element ModElement => (Element)1;
    public NecropolisPack Pack => (NecropolisPack)(this + (long)this);
    public PackFrequencyName PackFrequency => (PackFrequencyName)(this + (long)this);
    public int MinMonstersPerPack => (int)(this + (long)this);
    public int MaxMonstersPerPack => (int)(this + (long)this);
    public int ModIndex => (int)(this + (long)this);
}