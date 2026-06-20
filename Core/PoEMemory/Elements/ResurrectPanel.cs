namespace ExileCore.PoEMemory.Elements;
public class ResurrectPanel : Element
{
    public Element ResurrectInTown => (Element)(object)new int[2]
    {
        1,
        0
    };

    public Element ResurrectAtCheckpoint
    {
        get
        {
            //IL_000a: Expected O, but got I4
            _ = 1;
            return (Element)10;
        }
    }
}