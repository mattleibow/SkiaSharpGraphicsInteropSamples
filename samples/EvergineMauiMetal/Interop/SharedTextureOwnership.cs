namespace EvergineMauiMetal.Interop;

public sealed class SharedTextureOwnership
{
    public SharedTextureOwnership(nint nativeTextureHandle)
    {
        if (nativeTextureHandle == 0)
        {
            throw new ArgumentException("The native texture handle must not be null.", nameof(nativeTextureHandle));
        }

        NativeTextureHandle = nativeTextureHandle;
    }

    public nint NativeTextureHandle { get; }

    public bool IsEngineTextureReleased { get; private set; }

    public bool IsSkiaWrapperAttached { get; private set; }

    public bool IsEngineOwned => !IsEngineTextureReleased;

    public void AttachSkiaWrapper(nint currentHandle)
    {
        ValidateHandle(currentHandle);

        if (IsEngineTextureReleased)
        {
            throw new InvalidOperationException("The engine texture has already been released.");
        }

        if (IsSkiaWrapperAttached)
        {
            throw new InvalidOperationException("A Skia wrapper is already attached.");
        }

        IsSkiaWrapperAttached = true;
    }

    public void DetachSkiaWrapper(nint currentHandle)
    {
        ValidateHandle(currentHandle);

        if (!IsSkiaWrapperAttached)
        {
            throw new InvalidOperationException("No Skia wrapper is attached.");
        }

        IsSkiaWrapperAttached = false;
    }

    public void ReleaseEngineTexture(nint currentHandle)
    {
        ValidateHandle(currentHandle);

        if (IsSkiaWrapperAttached)
        {
            throw new InvalidOperationException("Skia wrappers must be detached before the engine texture is released.");
        }

        if (IsEngineTextureReleased)
        {
            throw new InvalidOperationException("The engine texture has already been released.");
        }

        IsEngineTextureReleased = true;
    }

    public void ValidateHandle(nint currentHandle)
    {
        if (currentHandle != NativeTextureHandle)
        {
            throw new InvalidOperationException("The native texture handle changed.");
        }
    }
}
