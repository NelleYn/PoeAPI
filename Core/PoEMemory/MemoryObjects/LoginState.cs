using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class LoginState : GameState
{
    private readonly FrameCache<LoginStateOffsets> _cache;
    public Element UIRoot => (Element)(object)this;
    public Element LoginInput => (Element)(object)new int[2]
    {
        2,
        7
    };
    public Element PasswordInput => (Element)(object)new int[2]
    {
        2,
        9
    };

    public DropdownElement GatewaySelector
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}