using System;
using System.Collections.Generic;
using System.IO;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Interfaces;
using Newtonsoft.Json;
using SharpDX;

namespace ExileCore;

/// <summary>
/// Base class for plugins with strongly-typed settings of type <typeparamref name="TSettings"/>.
/// Handles loading/saving settings, building the settings menu drawers, lifecycle hooks and
/// access to the game/graphics APIs. Subclasses override the virtual hooks they need.
/// </summary>
/// <typeparam name="TSettings">The plugin's settings type.</typeparam>
public abstract class BaseSettingsPlugin<TSettings> : IPlugin where TSettings : ISettings, new()
{
    const string TEXTURES_FOLDER = "textures";
    private AtlasTexturesProcessor _atlasTextures;
    private PluginManager _pluginManager;

    protected BaseSettingsPlugin()
    {
        InternalName = GetType().Namespace;
        if (string.IsNullOrWhiteSpace(Name)) Name = InternalName;
        Drawers = new List<ISettingsHolder>();
    }

    public List<ISettingsHolder> Drawers { get; }
    public GameController GameController { get; private set; }
    public Graphics Graphics { get; private set; }
    public TSettings Settings => (TSettings) _Settings;
    public ISettings _Settings { get; private set; }
    public bool CanUseMultiThreading { get; protected set; }
    public string Description { get; protected set; }
    public string DirectoryName { get; set; }
    public string DirectoryFullName { get; set; }
    public bool Force { get; protected set; }
    public bool Initialized { get; set; }
    public string InternalName { get; }
    public string Name { get; set; }
    public int Order { get; protected set; }

    /// <summary>Loads the plugin settings from disk (creating defaults if missing) and builds the menu drawers.</summary>
    public void _LoadSettings()
    {
        var loadedFile = GameController.Settings.LoadSettings(this);

        if (loadedFile == null)
        {
            _Settings = new TSettings();
            _SaveSettings();
        }
        else
            _Settings = JsonConvert.DeserializeObject<TSettings>(loadedFile, SettingsContainer.jsonSettings);

        SettingsParser.Parse(_Settings, Drawers);
    }

    /// <summary>Persists the plugin settings to disk.</summary>
    public void _SaveSettings()
    {
        if (_Settings == null)
            throw new NullReferenceException("Plugin settings is null");

        GameController.Settings.SaveSettings(this);
    }

    /// <summary>Called when the player changes area. Override to react to area changes.</summary>
    public virtual void AreaChange(AreaInstance area)
    {
    }

    /// <summary>Disposes the plugin, saving settings via <see cref="OnClose"/> by default.</summary>
    public virtual void Dispose()
    {
        OnClose();
    }

    /// <summary>Draws the plugin's settings menu. By default draws all registered drawers.</summary>
    public virtual void DrawSettings()
    {
        foreach (var drawer in Drawers)
        {
            drawer.Draw();
        }
    }

    /// <summary>Called when a gameplay-relevant entity is added. Override to react.</summary>
    public virtual void EntityAdded(Entity entity)
    {
    }

    /// <summary>Called when any entity is added (including non-gameplay). Override to react.</summary>
    public virtual void EntityAddedAny(Entity entity)
    {
    }

    /// <summary>Called when an entity is ignored. Override to react.</summary>
    public virtual void EntityIgnored(Entity entity)
    {
    }

    /// <summary>Called when an entity is removed. Override to react.</summary>
    public virtual void EntityRemoved(Entity entity)
    {
    }

    /// <summary>Called once after the plugin is loaded. Override for one-time setup.</summary>
    public virtual void OnLoad()
    {
    }

    /// <summary>Called when the plugin is unloaded. Override for teardown.</summary>
    public virtual void OnUnload()
    {
    }

    /// <summary>Initializes the plugin. Return false to abort loading. Defaults to true.</summary>
    public virtual bool Initialise()
    {
        return true;
    }

    /// <summary>Logs an informational message to the debug overlay.</summary>
    public void LogMsg(string msg)
    {
        DebugWindow.LogMsg(msg);
    }

    /// <summary>Called when the plugin closes. By default saves settings.</summary>
    public virtual void OnClose()
    {
        _SaveSettings();
    }

    /// <summary>Receives an inter-plugin event. Override to handle published events.</summary>
    public virtual void ReceiveEvent(string eventId, object args)
    {
    }

    /// <summary>Publishes an inter-plugin event to the plugin manager.</summary>
    public void PublishEvent(string eventId, object args)
    {
        _pluginManager.ReceivePluginEvent(eventId, args, this);
    }

    /// <summary>Called when this plugin is selected in the menu. Override to react.</summary>
    public virtual void OnPluginSelectedInMenu()
    {
    }

    /// <summary>Per-frame logic hook. Returns an optional <see cref="Job"/> to run off the main thread.</summary>
    public virtual Job Tick()
    {
        return null;
    }

    /// <summary>Per-frame rendering hook. Override to draw on the overlay.</summary>
    public virtual void Render()
    {
    }

    /// <summary>Logs an error message to the debug overlay.</summary>
    public void LogError(string msg, float time = 1f)
    {
        DebugWindow.LogError(msg, time);
    }

    /// <summary>Logs a message to the debug overlay with an explicit color.</summary>
    public void LogMessage(string msg, float time, Color clr)
    {
        DebugWindow.LogMsg(msg, time, clr);
    }

    /// <summary>Logs a message to the debug overlay using the default highlight color.</summary>
    public void LogMessage(string msg, float time = 1f)
    {
        DebugWindow.LogMsg(msg, time, Color.GreenYellow);
    }

    /// <summary>Called before the plugin instance is destroyed during a hot reload. Override to react.</summary>
    public virtual void OnPluginDestroyForHotReload()
    {
    }

    /// <summary>Injects the core APIs (game controller, graphics and plugin manager) into the plugin.</summary>
    public void SetApi(GameController gameController, Graphics graphics, PluginManager pluginManager)
    {
        GameController = gameController;
        Graphics = graphics;
        _pluginManager = pluginManager;
    }

    #region Atlas Images

    /// <summary>
    /// Returns a named texture from the plugin's texture atlas, loading the atlas (config + png)
    /// from the plugin's <c>textures</c> folder on first use. Returns null when the atlas is missing.
    /// </summary>
    public AtlasTexture GetAtlasTexture(string textureName)
    {
        if (_atlasTextures == null)
        {
            var atlasDirectory = Path.Combine(DirectoryFullName, TEXTURES_FOLDER);
            var atlasConfigNames = Directory.GetFiles(atlasDirectory, "*.json");

            if (atlasConfigNames.Length == 0)
            {
                LogError($"Plugin '{Name}': Can't find atlas json config file in '{atlasDirectory}' " +
                         "(expecting config 'from Free texture packer' program)", 20);

                _atlasTextures = new AtlasTexturesProcessor("%AtlasNotFound%");
                return null;
            }

            var atlasName = Path.GetFileNameWithoutExtension(atlasConfigNames[0]);

            if (atlasConfigNames.Length > 1)
            {
                LogError($"Plugin '{Name}': Found multiple atlas configs in folder '{atlasDirectory}', " +
                         $"selecting the first one ''{atlasName}''", 20);
            }

            var atlasTexturePath = Path.Combine(DirectoryFullName, $"{TEXTURES_FOLDER}\\{atlasName}.png");

            if (!File.Exists(atlasTexturePath))
            {
                LogError($"Plugin '{Name}': Can't find atlas png texture file in '{atlasTexturePath}' ", 20);
                _atlasTextures = new AtlasTexturesProcessor(atlasName);
                return null;
            }

            _atlasTextures = new AtlasTexturesProcessor(configPath: atlasConfigNames[0], atlasPath: atlasTexturePath);
            Graphics.InitImage(atlasTexturePath, false);
        }

        var texture = _atlasTextures.GetTextureByName(textureName);

        return texture;
    }

    #endregion
}
