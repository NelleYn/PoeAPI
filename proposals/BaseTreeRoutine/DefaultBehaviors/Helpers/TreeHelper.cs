// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

namespace ExileCore.TreeRoutine.DefaultBehaviors.Helpers;

/// <summary>
/// Gating helper ported from upstream <c>TreeRoutine/DefaultBehaviors/Helpers/TreeHelper.CanTick()</c>,
/// rewritten against this fork. Answers "should the tree run right now?" — used as the guard of the
/// tree's root <c>Decorator</c>. Every member below is a real fork member (see the README table).
/// </summary>
public static class TreeHelper
{
    /// <summary>
    /// <c>true</c> only when it is safe to read player state and drive input: not loading, connected
    /// and in game, a valid local player, the game window focused, and not standing in town/hideout.
    /// </summary>
    public static bool CanTick(GameController gameController)
    {
        if (gameController == null)
            return false;

        if (gameController.IsLoading)
            return false;

        // Connected and in a game instance.
        if (!gameController.Game.IngameState.ServerData.IsInGame)
            return false;

        // Local player entity resolved and readable.
        var player = gameController.Player;
        if (player == null || player.Address == 0 || !player.IsValid)
            return false;

        // Never drive input into a background window.
        if (!gameController.Window.IsForeground())
            return false;

        // No combat routines while safe. This fork has no AreaInstance.IsPeaceful; gate on town/hideout.
        var area = gameController.Area?.CurrentArea;
        if (area == null || area.IsTown || area.IsHideout)
            return false;

        return true;
    }
}
