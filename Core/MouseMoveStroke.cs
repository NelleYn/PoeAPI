using System.Collections.Generic;

namespace ExileCore;

/// <summary>An ordered set of mouse-move points describing a single mouse stroke.</summary>
public sealed record MouseMoveStroke(List<MouseMoveStrokePoint> Points);
