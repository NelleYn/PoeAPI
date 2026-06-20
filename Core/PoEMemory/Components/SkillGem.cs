using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing a skill gem's level, experience, max level, and socket color.
/// </summary>
public class SkillGem : Component
{
    private readonly CachedValue<SkillGemOffsets> _cachedValue;
    private readonly FrameCache<GemInformation> _cachedValue2;

    /// <summary>Initializes a new instance of the <see cref="SkillGem"/> class.</summary>
    public SkillGem()
    {
        _cachedValue = new FrameCache<SkillGemOffsets>(() => M.Read<SkillGemOffsets>(Address));
        _cachedValue2 = new FrameCache<GemInformation>(() => M.Read<GemInformation>(_cachedValue.Value.AdvanceInformation));
    }

    /// <summary>Gets the current gem level.</summary>
    public int Level => (int)_cachedValue.Value.Level;//TODO: fixme, remove cast

    /// <summary>Gets the total experience gained by the gem.</summary>
    public uint TotalExpGained => _cachedValue.Value.TotalExpGained;

    /// <summary>Gets the experience at the start of the current level.</summary>
    public uint ExperiencePrevLevel => _cachedValue.Value.TotalExpGained;

    /// <summary>Gets the experience required to reach the maximum level.</summary>
    public uint ExperienceMaxLevel => _cachedValue.Value.ExperienceMaxLevel;

    /// <summary>Gets the experience remaining until the next level.</summary>
    public uint ExperienceToNextLevel => ExperienceMaxLevel - ExperiencePrevLevel;

    /// <summary>Gets the maximum level the gem can reach.</summary>
    public int MaxLevel => _cachedValue2.Value.MaxLevel;

    /// <summary>Gets the color of the socket the gem requires.</summary>
    public int SocketColor => _cachedValue2.Value.SocketColor;
}
