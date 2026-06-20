using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Components;
public class GroundEffect : Component
{
    public int SizeIncreaseOverTime => this + 44;
    public int Scale => this + 72;
    public float MaxDuration => this + 48;
    public float Duration => this + 52;
    public GroundEffectDat EffectDescription => (GroundEffectDat)(this + 56);
}