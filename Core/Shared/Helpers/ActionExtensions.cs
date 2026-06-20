using System;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Provides extension methods to invoke delegates while swallowing and logging any exceptions.
/// </summary>
public static class ActionExtensions
{
    /// <summary>
    /// Invokes the supplied action, logging any thrown exception instead of propagating it.
    /// </summary>
    /// <param name="action">The action to invoke. Ignored if <see langword="null" />.</param>
    public static void SafeTryInvoke(this Action action)
    {
        if (action != null)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.ToString());
            }
        }
    }

    /// <summary>
    /// Invokes the supplied action, logging any thrown exception instead of propagating it.
    /// </summary>
    /// <param name="action">The action to invoke. Ignored if <see langword="null" />.</param>
    /// <param name="arg">The argument to pass to the action.</param>
    public static void SafeTryInvoke<T>(this Action<T> action, T arg)
    {
        if (action != null)
        {
            try
            {
                action(arg);
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.ToString());
            }
        }
    }

    /// <summary>
    /// Invokes the supplied action, logging any thrown exception instead of propagating it.
    /// </summary>
    /// <param name="action">The action to invoke. Ignored if <see langword="null" />.</param>
    /// <param name="arg1">The first argument to pass to the action.</param>
    /// <param name="arg2">The second argument to pass to the action.</param>
    public static void SafeTryInvoke<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        if (action != null)
        {
            try
            {
                action(arg1, arg2);
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.ToString());
            }
        }
    }

    /// <summary>
    /// Invokes the supplied action, logging any thrown exception instead of propagating it.
    /// </summary>
    /// <param name="action">The action to invoke. Ignored if <see langword="null" />.</param>
    /// <param name="arg1">The first argument to pass to the action.</param>
    /// <param name="arg2">The second argument to pass to the action.</param>
    /// <param name="arg3">The third argument to pass to the action.</param>
    public static void SafeTryInvoke<T1, T2, T3>(this Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
        if (action != null)
        {
            try
            {
                action(arg1, arg2, arg3);
            }
            catch (Exception e)
            {
                DebugWindow.LogError(e.ToString());
            }
        }
    }
}