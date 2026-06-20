using System;

namespace ExileCore.Shared.Attributes;

/// <summary>
/// Marks a property, field or method so that it is excluded when members are enumerated via reflection.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
public class HideInReflectionAttribute : Attribute
{
}
