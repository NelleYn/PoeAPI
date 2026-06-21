using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;
using Serilog.Events;

namespace ExileCore;
[Submenu]
public class CorePluginSettings
{
    [Submenu(RenderMethod = "Render", CollapsedByDefault = true, EnableSelfDrawCollapsing = true)]
    public class PluginFolderSettings
    {
        public class PluginFolder
        {
            public string Name;
            public Vector4 Color;
            public bool CollapsedByDefault;
            public Guid Id { get; init; }
        }

        public List<PluginFolder> PluginFolders;
        public Dictionary<string, Guid?> PluginFolderMapping;
        public void Render()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    [Submenu(CollapsedByDefault = true)]
    public class PluginNotificationSettings
    {
        [Submenu(RenderMethod = "Render", CollapsedByDefault = true, EnableSelfDrawCollapsing = true)]
        public class Categories
        {
            public class PerPluginSettings
            {
                public class LevelMapping
                {
                    public string CategoryRegex;
                    public string IdRegex;
                    public LogEventLevel Level;
                    internal string LevelInput;
                    public LevelMapping()
                    {
                        _ = 2;
                    }
                }

                public string CategoryBlacklistRegex;
                public string MapCategoryBlacklistRegex;
                public string IdBlacklistRegex;
                public string MapIdBlacklistRegex;
                public List<LevelMapping> LevelMappings;
            }

            private static readonly List<string> LogEventLevelValues;
            public Dictionary<string, PerPluginSettings> Plugins;
            private string _filter;
            public void Render()
            {
                throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
            }

            static Categories()
            {
                Enum.GetValues<LogEventLevel>();
            }
        }

        public ToggleNode Show { get; set; } = new();
        public ToggleNode ShowWhenNoNotifications { get; set; } = new();
        public ToggleNode MoveWhenSettingsOpen { get; set; } = new();
        public ListNode MinMapEventLevel { get; set; } = new();
        public ListNode MinEventLevel { get; set; } = new();
        public ColorNode VerboseNotificationColor { get; set; } = new();
        public ColorNode DebugNotificationColor { get; set; } = new();
        public ColorNode InfoNotificationColor { get; set; } = new();
        public ColorNode WarningNotificationColor { get; set; } = new();
        public ColorNode ErrorNotificationColor { get; set; } = new();
        public ColorNode FatalNotificationColor { get; set; } = new();
        public Categories CategoriesSettings { get; set; }

        public PluginNotificationSettings()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    [Menu("Load source plugins in parallel", "Requires restart to apply. When you use a lot of plugins this option can improve hud load time.")]
    public ToggleNode MultiThreadLoadPlugins { get; set; } = new();

    [Menu("Avoid locking plugin dlls", "Requires restart to apply. Only enable this if you need to do live dll replacement without restarting the HUD.")]
    public ToggleNode AvoidLockingDllFiles { get; set; } = new();

    [Menu(null, "Requires restart to apply. Load plugins from source even if there is a compiled plugin with the same name")]
    public ToggleNode PreferSourcePlugins { get; set; } = new();

    [Menu(null, "Start one build using a graph with all plugins included. Decreases total build time")]
    [JsonProperty("BuildAllPluginsAtOnce_v2")]
    public ToggleNode BuildAllPluginsAtOnce { get; set; } = new();
    public ToggleNode CachePluginCompilationResults { get; set; } = new();

    [Menu(null, "You probably don't want to enable this")]
    public ToggleNode IgnoreReferenceChangesForCompilationCache { get; set; } = new();

    [JsonIgnore]
    public ButtonNode ResetCompilationCache { get; set; } = new();
    public PluginFolderSettings FolderSettings { get; set; }
    public PluginNotificationSettings NotificationSettings { get; set; }

    public CorePluginSettings()
    {
        _ = 0;
        _ = 0;
        _ = 0;
        _ = 1;
        _ = 1;
        _ = 0;
    }
}