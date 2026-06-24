namespace ExileCore.PoEMemory.Elements;
public class UltimatumChoiceElement : Element
{
    public bool IsSelectedChoice => (int)(this + (long)this) > 0;
    public int Votes => (int)(this + (long)this);
    public int LockedVotes => (int)(this + (long)this);
}