// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore;
public partial class PagedMemoryBackend
{
    public System.Collections.Concurrent.ConcurrentDictionary<nint, System.Buffers.IMemoryOwner<System.Byte>> _cachedPages;
    public System.Collections.Concurrent.ConcurrentDictionary<nint, System.Boolean> _pagesRequestedOnLastIteration;
    public System.Threading.CancellationTokenSource _nextFrameCts;
    public System.Boolean _disposed;
    public System.Threading.ThreadLocal<ExileCore.PagedMemoryBackend.BoolHolder> _disabled;
    public static System.Int32 PageSize;
    public ExileCore.IMemoryBackend _pageBackend;
    public System.Threading.ReaderWriterLockSlim _lock;
    public nint GetPageAddress(nint address)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Buffers.IMemoryOwner<System.Byte> GetRealPage(nint address)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Buffers.IMemoryOwner<System.Byte> GetRealPage(nint address, System.Int32 pageCount)
    {
        throw new global::System.NotImplementedException();
    }

    public System.IDisposable DisableCaching()
    {
        throw new global::System.NotImplementedException();
    }

    public System.Boolean TryReadMemory(nint address, System.Span<System.Byte> target)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Buffers.IMemoryOwner<System.Byte> GetPage(nint pageAddress)
    {
        throw new global::System.NotImplementedException();
    }

    public void NotifyFrame()
    {
        throw new global::System.NotImplementedException();
    }

    public void DropRentedPages()
    {
        throw new global::System.NotImplementedException();
    }

    public void Dispose()
    {
        throw new global::System.NotImplementedException();
    }
}