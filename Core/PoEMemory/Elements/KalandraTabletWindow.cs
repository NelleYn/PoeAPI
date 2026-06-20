using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements;
public class KalandraTabletWindow : Element
{
    public List<TabletTileElement> Tiles
    {
        get
        {
            int[] obj = new int[2]
            {
                2,
                0
            };
            while (obj != null)
            {
            }

            return (List<TabletTileElement>)(object)this;
        }
    }

    public List<TabletChoiceElement> Choices => (List<TabletChoiceElement>)(object)new int[2]
    {
        3,
        0
    };
}