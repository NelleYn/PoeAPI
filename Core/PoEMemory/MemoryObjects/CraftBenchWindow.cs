namespace ExileCore.PoEMemory.MemoryObjects;
public class CraftBenchWindow : Element
{
    public Element PrefixesElement => (Element)(object)new int[5]
    {
        3,
        0,
        0,
        1,
        0
    };
    public Element SuffixesElement => (Element)(object)new int[5];
    public Element FilterElement => (Element)(object)new int[4]
    {
        2,
        1,
        0,
        0
    };
    public Element CraftsListElement => (Element)(object)new int[1]
    {
        3
    };
    public Element ItemSlotElement => (Element)(object)new int[2]
    {
        5,
        1
    };
    public Element CraftButton => (Element)(object)new int[3]
    {
        5,
        0,
        0
    };
}