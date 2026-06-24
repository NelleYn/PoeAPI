using System;
using System.IO.Pipes;

namespace ExileCore;
public class NamedPipeHandler : IDisposable
{
    [Serializable]
    public class InputCommand
    {
        public short? LeftThumbX;
        public short? LeftThumbY;
        public short? RightThumbX;
        public short? RightThumbY;
        public byte? LeftTrigger;
        public byte? RightTrigger;
        public bool? A;
        public bool? B;
        public bool? X;
        public bool? Y;
        public bool? LB;
        public bool? RB;
        public bool? LS;
        public bool? RS;
        public bool? Back;
        public bool? Start;
        public bool? DpadUp;
        public bool? DpadRight;
        public bool? DpadLeft;
        public bool? DpadDown;
    }

    private static readonly string NAMED_PIPE_PREFIX;
    private string _pipeName;
    private PipeDirection _direction;
    private NamedPipeClientStream namedPipeClientStream;
    public NamedPipeHandler(int pipeNumber, PipeDirection direction)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public bool WriteToPipe(InputCommand command)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private bool CheckConnection()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public void Dispose()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}