using EvergineMauiMetal.Interop;

namespace EvergineMauiMetal.Interop.Tests;

public class SharedTextureOwnershipTests
{
    [Fact]
    public void SkiaWrapperNeverTakesEngineTextureOwnership()
    {
        var ownership = new SharedTextureOwnership(42);

        ownership.AttachSkiaWrapper(42);
        ownership.DetachSkiaWrapper(42);

        Assert.True(ownership.IsEngineOwned);
        Assert.False(ownership.IsEngineTextureReleased);
    }

    [Fact]
    public void EngineTextureCannotBeReleasedWhileSkiaWrapperIsAttached()
    {
        var ownership = new SharedTextureOwnership(42);
        ownership.AttachSkiaWrapper(42);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ownership.ReleaseEngineTexture(42));

        Assert.Contains("detached", exception.Message);
    }

    [Fact]
    public void EngineReleasesTextureAfterSkiaWrapper()
    {
        var ownership = new SharedTextureOwnership(42);
        ownership.AttachSkiaWrapper(42);
        ownership.DetachSkiaWrapper(42);

        ownership.ReleaseEngineTexture(42);

        Assert.True(ownership.IsEngineTextureReleased);
        Assert.False(ownership.IsEngineOwned);
    }

    [Fact]
    public void NativeHandleMustRemainStable()
    {
        var ownership = new SharedTextureOwnership(42);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ownership.ValidateHandle(43));

        Assert.Contains("changed", exception.Message);
    }
}
