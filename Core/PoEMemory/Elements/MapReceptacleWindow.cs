using System;
using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
[Obsolete("This will be removed soon")]
public class MapReceptacleWindow : Element
{
    public Element CloseMapDialog => (Element)3;
    public Element ActivateButton => (Element)(object)new int[1]
    {
        4
    };
    public Element MapPiecesPanel => (Element)7;

    public List<Element> MapsElements
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<Entity> InsertedMaps
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}