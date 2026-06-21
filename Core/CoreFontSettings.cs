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
        public ListNode Font { get; set; } = new();
        public ContentNode<RangeNode<int>> Sizes { get; set; } = new();

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
    public ContentNode<CoreFontEntrySetting> Fonts { get; set; } = new();
    public ListNode MainFont { get; set; } = new();
    public ListNode FontGlyphRange { get; set; } = new();

    [Menu(null, "If unchecked, some plugin may ignore the selected font")]
    public ToggleNode ApplySelectedFontGlobally { get; set; } = new();

    [JsonIgnore]
    public ButtonNode Apply { get; set; } = new();

    public unsafe CoreFontSettings()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}