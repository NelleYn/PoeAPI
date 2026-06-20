using ExileCore.PoEMemory.Elements;

namespace ExileCore.PoEMemory.MemoryObjects;
public class GameUi : Element
{
    public Element UnusedPassivePointsButton => (Element)3;
    public int UnusedPassivePointsAmount => (int)this;

    public SentinelPanel SentinelPanel
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AzmeriElement AzmeriElement
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element LifeOrb => (Element)1;
    public Element ManaOrb => (Element)2;
    public Element FlaskPanel => (Element)(object)new int[2]
    {
        5,
        1
    };

    private int GetUnusedPassivePointsAmount()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}