using System;
using System.Runtime.CompilerServices;
using System.Text;
using Serilog.Events;
using SharpDX;

namespace ExileCore;
public record DebugMessage
{
    public string Text { get; init; }
    public float? Duration { get; init; }
    public Color? Color { get; init; }
    public LogEventLevel Level { get; init; }

    public DebugMessage(string Text)
    {
        this.Text = Text;
    }

    public void Deconstruct(out string Text)
    {
        Text = this.Text;
    }
}