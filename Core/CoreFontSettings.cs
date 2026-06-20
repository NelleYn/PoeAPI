using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;

namespace ExileCore;
[Submenu(CollapsedByDefault = true)]
public class CoreFontSettings
{
    public class CoreFontEntrySetting
    {
        public ListNode Font
        {
            [CompilerGenerated]
            get
            {
                return (ListNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ContentNode<RangeNode<int>> Sizes
        {
            [CompilerGenerated]
            get
            {
                return (ContentNode<RangeNode<int>>)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        public unsafe CoreFontEntrySetting()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    internal readonly List<string> AllowedFonts;
    public ContentNode<CoreFontEntrySetting> Fonts
    {
        [CompilerGenerated]
        get
        {
            return (ContentNode<CoreFontEntrySetting>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public ListNode MainFont
    {
        [CompilerGenerated]
        get
        {
            return (ListNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public ListNode FontGlyphRange
    {
        [CompilerGenerated]
        get
        {
            return (ListNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [Menu(null, "If unchecked, some plugin may ignore the selected font")]
    public ToggleNode ApplySelectedFontGlobally
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [JsonIgnore]
    public ButtonNode Apply
    {
        [CompilerGenerated]
        get
        {
            return (ButtonNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public unsafe CoreFontSettings()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}