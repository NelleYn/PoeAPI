using System.Collections.Generic;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;
public class AnimationStageList : RemoteMemoryObject
{
    private List<AnimationStage> _stages;
    private NativePtrArray StageList => (NativePtrArray)this;

    public List<AnimationStage> AllStages
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}