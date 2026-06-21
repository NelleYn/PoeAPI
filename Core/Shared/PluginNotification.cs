using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.Shared;
public record PluginNotification
{
    public string Category { get; init; }
    public string Id { get; init; }
    public string Text { get; init; }

    public PluginNotification(string Category, string Id, string Text)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out string Category, out string Id, out string Text)
    {
        Category = (string)(object)this;
        Id = (string)(object)this;
        Text = (string)(object)this;
    }
}