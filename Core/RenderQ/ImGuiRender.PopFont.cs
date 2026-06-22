// Partial extension that restores a nested type missing from the modernized source.
using System;
using ImGuiNET;

namespace ExileCore.RenderQ;

partial class ImGuiRender
{
    /// <summary>Disposable that pops the current ImGui font when disposed (pairs with a PushFont).</summary>
    private sealed record PopFont : IDisposable
    {
        public void Dispose()
        {
            ImGui.PopFont();
        }
    }
}
