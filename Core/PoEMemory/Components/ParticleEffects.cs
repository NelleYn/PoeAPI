using System;
using System.Collections.Generic;
using System.Numerics;

namespace ExileCore.PoEMemory.Components;
public class ParticleEffects : Component
{
    public class ParticleEffect : RemoteMemoryObject
    {
        [Obsolete("Subject to change if I ever figure out what these are")]
        public int Id1 => this + 8;

        [Obsolete("Subject to change if I ever figure out what these are")]
        public int Id2 => this + 12;

        [Obsolete("Subject to change if I ever figure out what these are")]
        public int Id3 => this + 36;

        public string PetNames
        {
            get
            {
                //IL_0005: Unknown result type (might be due to invalid IL or missing references)
                _ = this + 16;
                _ = new int[2]
                {
                    24,
                    8
                };
                return (string)(object)this;
            }
        }
    }

    public Matrix4x4 Matrix => (Matrix4x4)(this + (long)this);

    public unsafe Vector3 Scale
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<ParticleEffect> Effects
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_000a: Expected O, but got I4
            _ = this + 48;
            return (List<ParticleEffect>)40;
        }
    }
}