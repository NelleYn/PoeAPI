// EXPERIMENTAL candidate — see proposals/Compat/README.md. Not part of the build.

using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.Shared.Compat;

/// <summary>
/// <see cref="Entity"/> extensions that bridge control-flow shapes ExileApi-Compiled plugins
/// expect, without adding any new data accessor.
/// </summary>
public static class EntityCompat
{
    /// <summary>
    /// Emulates upstream <c>Entity.TryGetComponent&lt;T&gt;(out T component)</c>.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">
    /// Set to the resolved component, or <c>null</c> when the entity does not have one.
    /// </param>
    /// <returns><c>true</c> when the entity has the component; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// The fork's <c>Entity.GetComponent&lt;T&gt;()</c>
    /// (<c>Core/PoEMemory/MemoryObjects/Entity.cs:609</c>) already returns <c>null</c> when the
    /// component is absent; this is a thin <c>bool</c>/<c>out</c> wrapper over it, matching the
    /// upstream call-site pattern (e.g. <c>WAYG/WhereAreYouGoing.cs:179</c>
    /// <c>player.TryGetComponent&lt;Positioned&gt;(out var p)</c>). Compatibility doc, "Entity &amp;
    /// EntityListWrapper".
    /// </remarks>
    public static bool TryGetComponent<T>(this Entity entity, out T component) where T : Component, new()
    {
        component = entity.GetComponent<T>();
        return component != null;
    }
}
