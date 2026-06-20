using System.Collections.Generic;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Elements;
public class ShortcutSettings : Element
{
    private const long ShortcutArrayOffset = 920L;
    public StdVector ShortcutArray => (StdVector)(this + (long)this);
    public IList<Shortcut> Shortcuts => (IList<Shortcut>)this;

    public Shortcut LeagueInterfaceShortcut
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Shortcut LeaguePanelShortcut
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Shortcut StalkerSentinelShortcut
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Shortcut PandemoniumSentinelShortcut
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Shortcut ApexSentinelShortcut
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}