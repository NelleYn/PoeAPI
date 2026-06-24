// Partial extension that restores a nested type missing from the modernized source.
using System;
using ExileCore.RenderQ;

namespace ExileCore;

partial class Graphics
{
    /// <summary>Disposable that captures the previous text scale of an <see cref="ImGuiRender"/>.</summary>
    private sealed record SetTextScaleDisposable(ImGuiRender Render, float OldScale) : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
