using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.SomeMagic;
using SharpDX;

namespace ExileCore;

/// <summary>
/// A simple registry that lets plugins share named delegates/objects with one another by name.
/// </summary>
public class PluginBridge
{
    private readonly Dictionary<string, object> methods = new Dictionary<string, object>();

    /// <summary>Retrieves a previously saved method/object by name, cast to <typeparamref name="T"/>, or null.</summary>
    public T GetMethod<T>(string name) where T : class
    {
        if (methods.TryGetValue(name, out var result)) return result as T;

        return null;
    }

    /// <summary>Registers a method/object under the given name.</summary>
    public void SaveMethod(string name, object method)
    {
        methods[name] = method;
    }
}

/// <summary>
/// Central façade for an attached game session: owns the game state, area controller, window,
/// entity list, memory and settings, and drives the per-frame <see cref="Tick"/>.
/// </summary>
public class GameController : IDisposable
{
    private readonly CoreSettings _settings;
    private readonly DebugInformation debClearCache;
    private readonly DebugInformation debDeltaTime;
    private readonly TimeCache<Vector2> LeftCornerMap;
    private readonly TimeCache<Vector2> UnderCornerMap;
    private bool IsForeGroundLast;

    /// <summary>Bridge used by plugins to share named methods/objects.</summary>
    public PluginBridge PluginBridge;

    /// <summary>Wires up the game session components (game state, area, window, entities) and starts work.</summary>
    public GameController(Memory memory, SoundController soundController, SettingsContainer settings,
        MultiThreadManager multiThreadManager)
    {
        _settings = settings.CoreSettings;
        Memory = memory;
        SoundController = soundController;
        Settings = settings;
        MultiThreadManager = multiThreadManager;

        try
        {
            Cache = new Cache();
            Game = new TheGame(memory, Cache);
            Area = new AreaController(Game);
            Window = new GameWindow(memory.Process);
            Files = Game.Files;
            EntityListWrapper = new EntityListWrapper(this, _settings, multiThreadManager);
        }
        catch (Exception e)
        {
            DebugWindow.LogError(e.ToString());
        }

        PluginBridge = new PluginBridge();

        IsForeGroundCache = WinApi.IsForegroundWindow(Window.Process.MainWindowHandle);
        var values = Enum.GetValues(typeof(IconPriority));

        LeftPanel = new PluginPanel(GetLeftCornerMap());
        UnderPanel = new PluginPanel(GetUnderCornerMap());

        var debParseFile = new DebugInformation("Parse files", false);
        debClearCache = new DebugInformation("Clear cache", false);

        debDeltaTime = Core.DebugInformations.FirstOrDefault(x => x.Name == "Delta Time");

        NativeMethods.LogError = _settings.LogReadMemoryError;

        _settings.LogReadMemoryError.OnValueChanged +=
            (obj, b) => NativeMethods.LogError = _settings.LogReadMemoryError;

        LeftCornerMap = new TimeCache<Vector2>(GetLeftCornerMap, 500);
        UnderCornerMap = new TimeCache<Vector2>(GetUnderCornerMap, 500);

        eIsForegroundChanged += b =>
        {
            if (b)
            {
                Core.MainRunner.ResumeCoroutines(Core.MainRunner.Coroutines);
                Core.ParallelRunner.ResumeCoroutines(Core.ParallelRunner.Coroutines);
            }
            else
            {
                Core.MainRunner.PauseCoroutines(Core.MainRunner.Coroutines);
                Core.ParallelRunner.PauseCoroutines(Core.ParallelRunner.Coroutines);
            }
        };

        _settings.RefreshArea.OnPressed += () => { Area.ForceRefreshArea(_settings.AreaChangeMultiThread); };
        Area.RefreshState();
        EntityListWrapper.StartWork();
        Initialized = true;
    }

    private Stopwatch sw { get; } = Stopwatch.StartNew();

    /// <summary>Milliseconds elapsed since this controller was created.</summary>
    public long ElapsedMs => sw.ElapsedMilliseconds;

    /// <summary>The root game state.</summary>
    public TheGame Game { get; }

    /// <summary>The area controller tracking the current area.</summary>
    public AreaController Area { get; }

    /// <summary>The game window wrapper.</summary>
    public GameWindow Window { get; }

    /// <summary>The current in-game UI/world state.</summary>
    public IngameState IngameState => Game.IngameState;

    /// <summary>The parsed game data files.</summary>
    public FilesContainer Files { get; }

    /// <summary>The local player entity.</summary>
    public Entity Player => EntityListWrapper.Player;

    /// <summary>Cached foreground state of the game/overlay window.</summary>
    public bool IsForeGroundCache { get; set; }

    /// <summary>Whether the player is currently in-game (not in a loading screen/menu).</summary>
    public bool InGame { get; private set; }

    /// <summary>Whether the game is currently on a loading screen.</summary>
    public bool IsLoading { get; private set; }

    /// <summary>Drawing panel anchored at the map's left corner.</summary>
    public PluginPanel LeftPanel { get; }

    /// <summary>Drawing panel anchored under the map.</summary>
    public PluginPanel UnderPanel { get; }

    /// <summary>The process-memory reader.</summary>
    public IMemory Memory { get; }

    /// <summary>The sound controller.</summary>
    public SoundController SoundController { get; }

    /// <summary>The settings container.</summary>
    public SettingsContainer Settings { get; }

    /// <summary>The worker thread pool manager.</summary>
    public MultiThreadManager MultiThreadManager { get; }

    /// <summary>The live entity list wrapper.</summary>
    public EntityListWrapper EntityListWrapper { get; }

    /// <summary>The shared memory-object cache.</summary>
    public Cache Cache { get; set; }

    /// <summary>The most recent frame delta time.</summary>
    public double DeltaTime => debDeltaTime.Tick;

    /// <summary>Whether the controller finished initializing successfully.</summary>
    public bool Initialized { get; }

    /// <summary>All currently cached entities.</summary>
    public ICollection<Entity> Entities => EntityListWrapper.Entities;

    /// <summary>Ad-hoc debug values keyed by name, shared between components.</summary>
    public Dictionary<string, object> Debug { get; } = new Dictionary<string, object>();

    /// <summary>Disposes the underlying memory reader.</summary>
    public void Dispose()
    {
        Memory?.Dispose();
    }

    /// <summary>Raised when the game/overlay foreground state changes.</summary>
    public static event Action<bool> eIsForegroundChanged = delegate { };

    /// <summary>Per-frame update: refreshes foreground/area/loading state and panel anchors.</summary>
    public void Tick()
    {
        try
        {
            if (IsForeGroundLast != IsForeGroundCache)
            {
                IsForeGroundLast = IsForeGroundCache;
                eIsForegroundChanged(IsForeGroundCache);
            }

            AreaInstance.CurrentHash = Game.CurrentAreaHash;
            if (LeftPanel.Used) LeftPanel.StartDrawPoint = LeftCornerMap.Value;
            if (UnderPanel.Used) UnderPanel.StartDrawPoint = UnderCornerMap.Value;

            //Every 3 frame check area change and force garbage collect every new area
            if (Core.FramesCount % 3 == 0 && Area.RefreshState())
                debClearCache.TickAction(() => { RemoteMemoryObject.Cache.TryClearCache(); });

            InGame = Game.InGame; //Game.IngameState.InGame;
            IsLoading = Game.IsLoading;
            if (InGame) CachedValue.Latency = Game.IngameState.CurLatency;
        }
        catch (Exception e)
        {
            DebugWindow.LogError(e.ToString());
        }
    }

    /// <summary>Computes the screen position of the map's left corner, accounting for diagnostic UI offsets.</summary>
    public Vector2 GetLeftCornerMap()
    {
        if (!InGame) return Vector2.Zero;
        var ingameState = Game.IngameState;
        var clientRect = ingameState.IngameUi.Map.SmallMiniMap.GetClientRectCache;
        var diagnosticElement = ingameState.LatencyRectangle;
        var ingameUiSulphit = ingameState.IngameUi.Sulphit;

        switch (ingameState.DiagnosticInfoType)
        {
            case DiagnosticInfoType.Off:

                if (ingameUiSulphit != null && ingameUiSulphit.IsVisibleLocal)
                    clientRect.X -= ingameUiSulphit.GetClientRectCache.Width;

                break;
            case DiagnosticInfoType.Short:
                clientRect.X -= diagnosticElement.X + 30;
                break;

            case DiagnosticInfoType.Full:

                if (ingameUiSulphit != null && ingameUiSulphit.IsVisibleLocal)
                    clientRect.X -= ingameUiSulphit.GetClientRectCache.Width;

                clientRect.Y += diagnosticElement.Y + diagnosticElement.Height;
                var fpsRectangle = ingameState.FPSRectangle;
                break;
        }

        return new Vector2(clientRect.X, clientRect.Y);
    }

    private Vector2 GetUnderCornerMap()
    {
        if (!InGame) return Vector2.Zero;

        var gemPanel = Game.IngameState.IngameUi.GemLvlUpPanel.Parent;
        var clientRect = gemPanel.GetClientRectCache;
        return new Vector2(clientRect.X + clientRect.Width, clientRect.Y + clientRect.Height);
    }
}
