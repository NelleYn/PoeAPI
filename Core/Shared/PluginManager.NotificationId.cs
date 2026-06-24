// Partial extension that restores a nested type missing from the modernized source.
namespace ExileCore.Shared;

partial class PluginManager
{
    /// <summary>Identifies a plugin notification by plugin, category and notification key.</summary>
    internal record NotificationId(string PluginId, string Category, string Notification);
}
