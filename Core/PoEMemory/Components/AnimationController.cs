using System;
using System.Runtime.CompilerServices;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class AnimationController : Component
{
    private readonly CachedValue<Func<float, float>> _progressTransformFunc;
    private readonly CachedValue<float> _animationSpeed;
    private readonly CachedValue<CachedValue<SupportedAnimationList>> _supportedAnimationList;
    private readonly CachedValue<AnimationControllerOffsets> _cachedValue;
    internal AnimationControllerOffsets Structure => (AnimationControllerOffsets)this;
    private ActiveAnimationData ActiveAnimationData => (ActiveAnimationData)(object)this;
    public float MaxRawAnimationProgress => (object)this - (object)this;
    public float RawNextAnimationPoint => (float)this;
    public float RawAnimationProgress => (float)this;
    public float RawAnimationSpeed => (object)this * (object)this;
    public float TransformedMaxRawAnimationProgress => (float)this;
    public float TransformedRawNextAnimationPoint => (float)this;
    public float TransformedRawAnimationProgress => (float)this;
    public float AnimationSpeed => (float)this;
    public SupportedAnimationList SupportedAnimationList => (SupportedAnimationList)(object)this;
    public int CurrentAnimationId => (int)this;
    public int CurrentAnimationStage => (int)this;

    public AnimationStageList CurrentAnimation
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public float NextAnimationPoint => (object)this / (object)this;
    public float AnimationProgress => (object)this / (object)this;

    public TimeSpan AnimationCompletesIn
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public TimeSpan AnimationActiveFor
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public float TransformProgress(float progress)
    {
        return progress;
    }
}