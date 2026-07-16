using System;
using System.Numerics;
using System.Threading;
using ImGuiNET;

namespace ExileCore
{
    /// <summary>
    /// ImGui scope helpers matching the ExileApi-Compiled API surface: push a style var/color
    /// and pop it exactly once when the returned scope is disposed (double-dispose is a no-op,
    /// so the native style stack can never underflow). Only the members whose behavior is
    /// fully determined are ported; upstream's search comboboxes and icon widgets are not.
    /// </summary>
    public static class ImGuiHelpers
    {
        private sealed class StyleVarScope : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    ImGui.PopStyleVar(1);
                }
            }
        }

        private sealed class StyleColorScope : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    ImGui.PopStyleColor(1);
                }
            }
        }

        public static IDisposable UseStyleVar(ImGuiStyleVar idx, float val)
        {
            ImGui.PushStyleVar(idx, val);
            return new StyleVarScope();
        }

        public static IDisposable UseStyleVar(ImGuiStyleVar idx, Vector2 val)
        {
            ImGui.PushStyleVar(idx, val);
            return new StyleVarScope();
        }

        public static IDisposable UseStyleColor(ImGuiCol idx, Vector4 col)
        {
            ImGui.PushStyleColor(idx, col);
            return new StyleColorScope();
        }

        public static IDisposable UseStyleColor(ImGuiCol idx, uint col)
        {
            ImGui.PushStyleColor(idx, col);
            return new StyleColorScope();
        }
    }
}
