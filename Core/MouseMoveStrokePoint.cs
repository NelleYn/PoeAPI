using System;
using System.Numerics;

namespace ExileCore;

/// <summary>A single point of a <see cref="MouseMoveStroke"/>, with the delay before it is applied.</summary>
public sealed record MouseMoveStrokePoint(Vector2 Point, TimeSpan Delay);
