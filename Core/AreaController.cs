using System;
using System.Collections;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;

namespace ExileCore;

/// <summary>
/// Tracks the player's current area instance and raises <see cref="OnAreaChange"/> when the
/// area changes (detected via the current area hash).
/// </summary>
public class AreaController
{
    private const string areaChangeCoroutineName = "Area change";

    /// <summary>Creates an area controller bound to the supplied game state.</summary>
    public AreaController(TheGame theGameState)
    {
        TheGameState = theGameState;
    }

    /// <summary>The game state this controller reads area data from.</summary>
    public TheGame TheGameState { get; }

    /// <summary>The area the player is currently in.</summary>
    public AreaInstance CurrentArea { get; private set; }

    /// <summary>Raised when the current area changes.</summary>
    public event Action<AreaInstance> OnAreaChange;

    /// <summary>Rebuilds <see cref="CurrentArea"/> from game memory and raises the change event unconditionally.</summary>
    public void ForceRefreshArea(bool areaChangeMultiThread)
    {
        var ingameData = TheGameState.IngameState.Data;
        var clientsArea = ingameData.CurrentArea;
        var curAreaHash = TheGameState.CurrentAreaHash;
        CurrentArea = new AreaInstance(clientsArea, curAreaHash, ingameData.CurrentAreaLevel);
        if (CurrentArea.Name.Length == 0) return;
        ActionAreaChange();
    }

    /// <summary>
    /// Refreshes <see cref="CurrentArea"/> only if the area hash changed, raising the change event
    /// when it does. Returns true when the area changed.
    /// </summary>
    public bool RefreshState()
    {
        var ingameData = TheGameState.IngameState.Data;
        var clientsArea = ingameData.CurrentArea;
        var curAreaHash = TheGameState.CurrentAreaHash;

        if (CurrentArea != null && curAreaHash == CurrentArea.Hash)
            return false;

        CurrentArea = new AreaInstance(clientsArea, curAreaHash, ingameData.CurrentAreaLevel);
        if (CurrentArea.Name.Length == 0) return false;
        ActionAreaChange();
        return true;
    }

    //Before call areachange for plugins need wait some time because sometimes gam,e memory not ready because still loading.
    private IEnumerator CoroutineAreaChange(bool areaChangeMultiThread)
    {
        yield return new WaitFunction(() => TheGameState.IsLoading /*&& !TheGameState.InGame*/);
    }

    private void ActionAreaChange()
    {
        OnAreaChange?.Invoke(CurrentArea);
    }
}
