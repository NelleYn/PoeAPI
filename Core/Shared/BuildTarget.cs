using System.Collections;
using System.Runtime.CompilerServices;

namespace ExileCore.Shared;
public class BuildTarget
{
    public string Name { get; set; }
    public string File { get; set; }
    public bool Succeeded { get; set; }
    public IEnumerable Outputs { get; set; }
}