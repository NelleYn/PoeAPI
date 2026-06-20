using System;
using System.Collections.Generic;

namespace ExileCore.Shared.Nodes;
internal interface IContentNodeBase
{
    IEnumerable<object> Content { get; }

    bool EnableControls { get; }

    Action SpawnItem { get; }

    bool EnableItemCollapsing { get; }

    bool UseFlatItems { get; }

    bool Remove(object item);
}