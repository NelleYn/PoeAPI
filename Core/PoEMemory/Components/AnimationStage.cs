using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class AnimationStage : StructuredRemoteMemoryObject<ActorAnimationStageOffsets>
{
    private string _stageName;
    private int? _actorAnimationListIndex;
    private float? _stageStart;
    public float StageStart
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int ActorAnimationListIndex
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string StageName
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}