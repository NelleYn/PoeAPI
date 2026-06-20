using System.Collections;

namespace ExileCore.Shared.Interfaces;

/// <summary>
/// Marker interface for coroutine yield conditions that are both enumerable and enumerator.
/// </summary>
public interface IYieldBase : IEnumerable, IEnumerator
{
}
