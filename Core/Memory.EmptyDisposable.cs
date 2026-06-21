// Partial extension that restores a nested type missing from the modernized source.
using System;

namespace ExileCore;
partial class Memory
{
    private class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new EmptyDisposable();

        public void Dispose()
        {
        }
    }
}