using System;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class ActiveAnimationData : StructuredRemoteMemoryObject<ActiveAnimationOffsets>
{
    public int AnimationId => (int)this;
    public float SlowAnimationSpeed => (float)this;
    public float NormalAnimationSpeed => (float)this;

    public unsafe float? AnimationSpeed
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AnimationStage SlowAnimationStartStage => (AnimationStage)(object)this;
    public AnimationStage SlowAnimationEndStage => (AnimationStage)(object)this;

    public Func<float, float> TransformRawProgressFunc
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}