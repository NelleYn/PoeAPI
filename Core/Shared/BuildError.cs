using System;
using System.Runtime.CompilerServices;

namespace ExileCore.Shared;
public class BuildError
{
    public string File { get; set; }
    public DateTime Timestamp { get; set; }
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string ProjectFile { get; set; }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}