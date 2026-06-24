namespace ExileCore.PoEMemory.Elements;
public class PopUpWindow : Element
{
    public Element TwoButtonWindowOk => (Element)(object)new int[4]
    {
        0,
        0,
        3,
        0
    };
    public Element TwoButtonWindowCancel => (Element)(object)new int[4]
    {
        0,
        0,
        3,
        1
    };
}