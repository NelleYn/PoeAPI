// Partial extension that restores a nested type missing from the modernized source.
using System.Runtime.CompilerServices;

namespace ExileCore;
partial class Memory
{
    private class BooleanHolder
    {
        public bool Value
        {
            [CompilerGenerated]
            get
            {
                //IL_0002: Expected I4, but got O
                return (byte)(int)this != 0;
            }

            [CompilerGenerated]
            set
            {
            }
        }
    }
}