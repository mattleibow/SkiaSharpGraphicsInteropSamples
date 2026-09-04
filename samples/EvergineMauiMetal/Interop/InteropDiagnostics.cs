namespace EvergineMauiMetal.Interop;

public sealed class InteropDiagnostics
{
    public InteropDiagnostics(nint nativeTextureHandle, string backend)
    {
        if (nativeTextureHandle == 0)
        {
            throw new ArgumentException("The native texture handle must not be null.", nameof(nativeTextureHandle));
        }

        NativeTextureHandle = nativeTextureHandle;
        Backend = backend;
    }

    public string Backend { get; }

    public nint NativeTextureHandle { get; }

    public long FrameCount { get; private set; }

    public bool IsNativeHandleStable { get; private set; } = true;

    public void CompleteFrame(nint currentHandle)
    {
        FrameCount++;
        IsNativeHandleStable &= currentHandle == NativeTextureHandle;
    }
}
