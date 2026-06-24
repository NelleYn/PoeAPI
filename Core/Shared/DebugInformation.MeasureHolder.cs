// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Diagnostics;

namespace ExileCore.Shared;
partial class DebugInformation
{
    public class MeasureHolder : IDisposable
    {
        private readonly DebugInformation _debugInformation;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;
        public TimeSpan Elapsed => (TimeSpan)this;

        public MeasureHolder(DebugInformation debugInformation)
        {
        }

        public void Dispose()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}