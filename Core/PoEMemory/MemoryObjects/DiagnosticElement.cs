using System;
using ExileCore.Shared.Cache;
using GameOffsets;
using ProcessMemoryUtilities.Memory;

namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps an in-game diagnostic graph element (e.g. FPS, latency, frame time), exposing its
/// position, size and the sampled values backing its plot.
/// </summary>
public class DiagnosticElement : RemoteMemoryObject
{
    private readonly CachedValue<DiagnosticElementOffsets> _cachedValue;
    private readonly CachedValue<DiagnosticElementArrayOffsets> _cachedValue2;
    private readonly FrameCache<float[]> Values;

    /// <summary>Initializes a new instance of the <see cref="DiagnosticElement"/> class.</summary>
    public DiagnosticElement()
    {
        _cachedValue = new FrameCache<DiagnosticElementOffsets>(() => M.Read<DiagnosticElementOffsets>(Address));

        _cachedValue2 =
            new FrameCache<DiagnosticElementArrayOffsets>(
                () => M.Read<DiagnosticElementArrayOffsets>(DiagnosticElementStruct.DiagnosticArray));

        Values = new FrameCache<float[]>(() =>
        {
            var buffer = new float[80];
            ProcessMemory.ReadProcessMemoryArray(M.OpenProcessHandle, (IntPtr) DiagnosticElementStruct.DiagnosticArray, buffer, 0, 80);
            return buffer;
        });
    }

    private DiagnosticElementOffsets DiagnosticElementStruct => _cachedValue.Value;
    private DiagnosticElementArrayOffsets DiagnosticElementArrayStruct => _cachedValue2.Value;

    /// <summary>Gets the address of the backing sample array.</summary>
    public long DiagnosticArray => DiagnosticElementStruct.DiagnosticArray;

    /// <summary>Gets the sampled values plotted by this diagnostic element.</summary>
    public float[] DiagnosticArrayValues => Values.Value;

    /// <summary>Gets the current (most recent) sampled value.</summary>
    public float CurrValue => DiagnosticElementArrayStruct.CurrValue;

    /// <summary>Gets the element's X position.</summary>
    public int X => DiagnosticElementStruct.X;

    /// <summary>Gets the element's Y position.</summary>
    public int Y => DiagnosticElementStruct.Y;

    /// <summary>Gets the element's width.</summary>
    public int Width => DiagnosticElementStruct.Width;

    /// <summary>Gets the element's height.</summary>
    public int Height => DiagnosticElementStruct.Height;
}
