// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.Shared;

namespace ExileCore;
partial class MenuWindow
{
    private record MainDebugTableRecord
    {
        [CompilerGenerated]
        protected virtual Type EqualityContract
        {
            [CompilerGenerated]
            get
            {
                return (Type)typeof(MainDebugTableRecord).TypeHandle;
            }
        }

        public DebugInformation Current { get; init; }
        public DebugInformation TotalDebug { get; init; }
        public int GroupCount { get; init; }

        public readonly string Name;
        public readonly float Percent;
        public readonly double Tick;
        public readonly float Total;
        public readonly float TotalPercent;
        public readonly float AllPluginAverage;
        public MainDebugTableRecord(DebugInformation Current, DebugInformation TotalDebug, int GroupCount)
        {
            //IL_001a: Unknown result type (might be due to invalid IL or missing references)
            _ = (object)this * (object)((object)Current / (object)TotalDebug);
            _ = (object)this * (object)((object)Current / (object)TotalDebug);
            _ = TotalDebug / (float)GroupCount;
        }

        [CompilerGenerated]
        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public override int GetHashCode()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public virtual bool Equals(MainDebugTableRecord? other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected MainDebugTableRecord(MainDebugTableRecord original)
        {
        }

        [CompilerGenerated]
        public void Deconstruct(out DebugInformation Current, out DebugInformation TotalDebug, out int GroupCount)
        {
            //IL_0009: Expected I4, but got O
            Current = (DebugInformation)(object)this;
            TotalDebug = (DebugInformation)(object)this;
            GroupCount = (int)this;
        }
    }
}