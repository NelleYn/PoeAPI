using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using GameOffsets;
using MoreLinq;
using SharpDX;

namespace ExileCore.PoEMemory;

/// <summary>
/// Represents a UI element in the game's interface tree, read from process memory.
/// Provides access to layout, visibility, text and the element's children.
/// </summary>
public class Element : RemoteMemoryObject
{
    /// <summary>Offset of the element's buffer block, relative to its base address.</summary>
    public const int OffsetBuffers = 0;
    private static readonly int IsVisibleLocalOff = Extensions.GetOffset<ElementOffsets>(nameof(ElementOffsets.IsVisibleLocal));
    private static readonly int ChildStartOffset = Extensions.GetOffset<ElementOffsets>(nameof(ElementOffsets.ChildStart));

    // dd id
    // dd (something zero)
    // 16 dup <128-bytes structure>
    // then the rest is
    private readonly CachedValue<ElementOffsets> _cacheElement;
    private CachedValue<bool> _cacheElementIsVisibleLocal;
    private readonly List<Element> _childrens = new List<Element>();
    private CachedValue<RectangleF> _getClientRect;

    private Element _parent;
    private long childHashCache;

    /// <summary>Initializes a new element and sets up the per-frame caches backing its offsets.</summary>
    public Element()
    {
        _cacheElement = new FrameCache<ElementOffsets>(() => Address == 0 ? default : M.Read<ElementOffsets>(Address));
        _cacheElementIsVisibleLocal = new FrameCache<bool>(() => Address == 0 ? default : M.Read<bool>(Address + IsVisibleLocalOff));
    }

    /// <summary>Gets the raw element offset structure read from memory.</summary>
    public ElementOffsets Elem => _cacheElement.Value;

    /// <summary>Gets a value indicating whether the element's self-pointer matches its address.</summary>
    public bool IsValid => Elem.SelfPointer == Address;

    /// <summary>Gets the number of direct children of this element.</summary>
    public long ChildCount => (Elem.ChildEnd - Elem.ChildStart) / 8;

    /// <summary>Gets a value indicating whether this element is locally visible (ignoring ancestors).</summary>
    /// <remarks>ElementFlags.IsVisibleLocal is 0x800, i.e. bit 0x08 of the byte at Flags+1 (328.8).</remarks>
    public bool IsVisibleLocal => (Elem.IsVisibleLocal & 8) == 8;

    /// <summary>Gets the root element of the in-game UI tree.</summary>
    public Element Root => TheGame.IngameState.UIRoot;

    /// <summary>Gets the parent element, or <c>null</c> when this element has no parent.</summary>
    public Element Parent => Elem.Parent == 0 ? null : _parent ?? (_parent = GetObject<Element>(Elem.Parent));

    /// <summary>Gets the element's position relative to its parent.</summary>
    public Vector2 Position => Elem.Position;

    /// <summary>Gets the element's X coordinate relative to its parent.</summary>
    public float X => Elem.X;

    /// <summary>Gets the element's Y coordinate relative to its parent.</summary>
    public float Y => Elem.Y;

    /// <summary>Gets the tooltip element attached to this element, or <c>null</c> when none exists.</summary>
    public Element Tooltip => Address == 0 ? null : GetObject<Element>(M.Read<long>(Address + 0x338)); //0x7F0

    /// <summary>Gets the element's local scale factor.</summary>
    public float Scale => Elem.Scale;

    /// <summary>Gets the element's local width.</summary>
    public float Width => Elem.Width;

    /// <summary>Gets the element's local height.</summary>
    public float Height => Elem.Height;

    /// <summary>Gets a value indicating whether the element is currently highlighted.</summary>
    public bool isHighlighted => Elem.isHighlighted != 0;

    /// <summary>Gets the element's text, with icon placeholders substituted, or <c>null</c> when empty.</summary>
    public virtual string Text
    {
        get
        {
            var text = AsObject<EntityLabel>().Text2;
            if (!string.IsNullOrWhiteSpace(text)) return text.Replace("\u00A0\u00A0\u00A0\u00A0", "{{icon}}");
            return null;
        }
    }

    /// <summary>Gets a value indicating whether this element and its entire ancestor chain are visible.</summary>
    public bool IsVisible
    {
        get
        {
            //998
            if (Address >= 1770350607106052 || Address <= 0) return false;
            return IsVisibleLocal && GetParentChain().All(current => current.IsVisibleLocal);
        }
    }

    /// <summary>Gets the direct children of this element.</summary>
    public IList<Element> Children => GetChildren<Element>();

    /// <summary>Gets a hash of the element's child pointers, used to detect child-list changes.</summary>
    public long ChildHash => Elem.Childs.GetHashCode();

    /// <summary>Gets the element's client rectangle, cached for a short interval.</summary>
    public RectangleF GetClientRectCache =>
        _getClientRect?.Value ?? (_getClientRect = new TimeCache<RectangleF>(GetClientRect, 200)).Value;

    /// <summary>Gets the child element at the given index.</summary>
    /// <param name="index">The zero-based index of the child to retrieve.</param>
    public Element this[int index] => GetChildAtIndex(index);

    /// <summary>Reads and caches the direct children of this element.</summary>
    /// <typeparam name="T">The element type constraint for the children.</typeparam>
    /// <returns>The cached list of child elements.</returns>
    protected List<Element> GetChildren<T>() where T : Element
    {
        var e = Elem;
        if (Address == 0 || e.ChildStart == 0 || e.ChildEnd == 0 || ChildCount < 0) return _childrens;

        if (ChildHash == childHashCache)
            return _childrens;

        var pointers = M.ReadPointersArray(e.ChildStart, e.ChildEnd);

        if (pointers.Count != ChildCount) return _childrens;
        _childrens.Clear();

        foreach (var pointer in pointers)
        {
            _childrens.Add(GetObject<Element>(pointer));
        }

        childHashCache = ChildHash;
        return _childrens;
    }

    /// <summary>Reads the direct children of this element as a freshly materialized list of <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The element type to materialize children as.</typeparam>
    /// <returns>A new list of child elements.</returns>
    public List<T> GetChildrenAs<T>() where T : Element, new()
    {
        var e = Elem;
        if (Address == 0 || e.ChildStart == 0 || e.ChildEnd == 0 || ChildCount < 0) return new List<T>();

        var pointers = M.ReadPointersArray(e.ChildStart, e.ChildEnd);

        if (pointers.Count != ChildCount)
            return new List<T>();

        var results = new List<T>();

        foreach (var pointer in pointers)
        {
            results.Add(GetObject<T>(pointer));
        }

        return results;
    }

    private IList<Element> GetParentChain()
    {
        var list = new List<Element>();

        if (Address == 0)
            return list;

        var hashSet = new HashSet<Element>();
        var root = Root;
        var parent = Parent;

        if (root == null || parent == null)
            return list;

        while (!hashSet.Contains(parent) && root.Address != parent.Address && parent.Address != 0)
        {
            list.Add(parent);
            hashSet.Add(parent);
            parent = parent.Parent;

            if (parent == null)
                break;
        }

        return list;
    }

    /// <summary>Computes this element's accumulated parent position, scaled relative to the UI root.</summary>
    /// <returns>The summed, scaled position contributed by all ancestor elements.</returns>
    public Vector2 GetParentPos()
    {
        float num = 0;
        float num2 = 0;
        var rootScale = TheGame.IngameState.UIRoot.Scale;

        foreach (var current in GetParentChain())
        {
            num += current.X * current.Scale / rootScale;
            num2 += current.Y * current.Scale / rootScale;
        }

        return new Vector2(num, num2);
    }

    /// <summary>Computes the element's on-screen rectangle, accounting for camera size and UI scaling.</summary>
    /// <returns>The element's client rectangle, or <see cref="RectangleF.Empty"/> when the element is invalid.</returns>
    public virtual RectangleF GetClientRect()
    {
        if (Address == 0) return RectangleF.Empty;
        var vPos = GetParentPos();
        float width = TheGame.IngameState.Camera.Width;
        float height = TheGame.IngameState.Camera.Height;
        var ratioFixMult = width / height / 1.6f;
        var xScale = width / 2560f / ratioFixMult;
        var yScale = height / 1600f;

        var rootScale = TheGame.IngameState.UIRoot.Scale;
        var num = (vPos.X + X * Scale / rootScale) * xScale;
        var num2 = (vPos.Y + Y * Scale / rootScale) * yScale;
        return new RectangleF(num, num2, xScale * Width * Scale / rootScale, yScale * Height * Scale / rootScale);
    }

    /// <summary>Walks the element tree following the given sequence of child indices.</summary>
    /// <param name="indices">The chain of child indices to follow.</param>
    /// <returns>The resolved element, or <c>null</c> when an index along the path does not exist.</returns>
    public Element GetChildFromIndices(params int[] indices)
    {
        var poe_UElement = this;

        foreach (var index in indices)
        {
            poe_UElement = poe_UElement.GetChildAtIndex(index);

            if (poe_UElement == null)
            {
                var str = "";
                indices.ForEach(i => str += $"[{i}] ");
                DebugWindow.LogMsg($"{nameof(Element)} with index: {index} not found. Indices: {str}");
                return null;
            }

            if (poe_UElement.Address == 0)
            {
                var str = "";
                indices.ForEach(i => str += $"[{i}] ");
                DebugWindow.LogMsg($"{nameof(Element)} with index: {index} 0 address. Indices: {str}");
                return GetObject<Element>(0);
            }
        }

        return poe_UElement;
    }

    /// <summary>Gets the child element at the given index.</summary>
    /// <param name="index">The zero-based index of the child to retrieve.</param>
    /// <returns>The child element, or <c>null</c> when the index is out of range.</returns>
    public Element GetChildAtIndex(int index)
    {
        return index >= ChildCount ? null : GetObject<Element>(M.Read<long>(Address + ChildStartOffset, index * 8));
    }
}
