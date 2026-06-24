using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory;
public class GroundEffectDat : RemoteMemoryObject
{
    public GroundEffectTypeDat Type => (GroundEffectTypeDat)(object)this;
    public BuffVisual BuffVisual => (BuffVisual)(this + 24);

    public List<string> AOFiles
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 61;
            _ = 8;
            return (List<string>)(object)this;
        }
    }

    public float BaseSize => this + 40;

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}