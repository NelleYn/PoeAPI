using System;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class EscapeState : GameState
{
    private readonly FrameCache<EscapeStateOffsets> _cache;
    public bool WasEverActive => this != null;
    public bool IsActive => this == null;
    public TimeSpan TotalActiveTime => (TimeSpan)(ulong)this;
    public Element HoveredElement => (Element)(object)this;
    public Element UIRoot => (Element)(object)this;
}