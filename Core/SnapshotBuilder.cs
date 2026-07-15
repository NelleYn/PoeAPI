// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore;
public partial class SnapshotBuilder
{
    [System.Runtime.InteropServices.DllImport("ntdll.dll")]
    public static extern System.Int32 NtSuspendProcess(nint processHandle);

    [System.Runtime.InteropServices.DllImport("ntdll.dll")]
    public static extern System.Int32 NtResumeProcess(nint processHandle);

    public System.Collections.Generic.SortedList<System.Int64, System.Byte[]> GetSnapshot(System.Boolean freezeProcess)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Collections.Generic.List<Vanara.PInvoke.Kernel32.MEMORY_BASIC_INFORMATION> GetProcessSections(nint handle)
    {
        throw new global::System.NotImplementedException();
    }
}