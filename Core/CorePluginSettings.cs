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
            public Guid Id
            {
                [CompilerGenerated]
                get
                {
                    return (Guid)this;
                }

                [CompilerGenerated]
                init
                {
                }
            }
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

        public ToggleNode Show
        {
            [CompilerGenerated]
            get
            {
                return (ToggleNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ToggleNode ShowWhenNoNotifications
        {
            [CompilerGenerated]
            get
            {
                return (ToggleNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ToggleNode MoveWhenSettingsOpen
        {
            [CompilerGenerated]
            get
            {
                return (ToggleNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ListNode MinMapEventLevel
        {
            [CompilerGenerated]
            get
            {
                return (ListNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ListNode MinEventLevel
        {
            [CompilerGenerated]
            get
            {
                return (ListNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ColorNode VerboseNotificationColor
        {
            [CompilerGenerated]
            get
            {
                return (ColorNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ColorNode DebugNotificationColor
        {
            [CompilerGenerated]
            get
            {
                return (ColorNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ColorNode InfoNotificationColor
        {
            [CompilerGenerated]
            get
            {
                return (ColorNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ColorNode WarningNotificationColor
        {
            [CompilerGenerated]
            get
            {
                return (ColorNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ColorNode ErrorNotificationColor
        {
            [CompilerGenerated]
            get
            {
                return (ColorNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public ColorNode FatalNotificationColor
        {
            [CompilerGenerated]
            get
            {
                return (ColorNode)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public Categories CategoriesSettings
        {
            [CompilerGenerated]
            get
            {
                return (Categories)(object)this;
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public PluginNotificationSettings()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    [Menu("Load source plugins in parallel", "Requires restart to apply. When you use a lot of plugins this option can improve hud load time.")]
    public ToggleNode MultiThreadLoadPlugins
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [Menu("Avoid locking plugin dlls", "Requires restart to apply. Only enable this if you need to do live dll replacement without restarting the HUD.")]
    public ToggleNode AvoidLockingDllFiles
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [Menu(null, "Requires restart to apply. Load plugins from source even if there is a compiled plugin with the same name")]
    public ToggleNode PreferSourcePlugins
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [Menu(null, "Start one build using a graph with all plugins included. Decreases total build time")]
    [JsonProperty("BuildAllPluginsAtOnce_v2")]
    public ToggleNode BuildAllPluginsAtOnce
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public ToggleNode CachePluginCompilationResults
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [Menu(null, "You probably don't want to enable this")]
    public ToggleNode IgnoreReferenceChangesForCompilationCache
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [JsonIgnore]
    public ButtonNode ResetCompilationCache
    {
        [CompilerGenerated]
        get
        {
            return (ButtonNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public PluginFolderSettings FolderSettings
    {
        [CompilerGenerated]
        get
        {
            return (PluginFolderSettings)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public PluginNotificationSettings NotificationSettings
    {
        [CompilerGenerated]
        get
        {
            return (PluginNotificationSettings)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

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