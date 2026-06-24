using System.Collections.Generic;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class SupportedAnimationList : StructuredRemoteMemoryObject<ActorAnimationListOffsets>
{
    private List<AnimationStageList> _animations;
    public List<AnimationStageList> Animations
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}