using EvergineMauiMetal.Interop;

namespace EvergineMauiMetal.Interop.Tests;

public class InteropDiagnosticsTests
{
    [Fact]
    public void MatchingHandleKeepsNativeHandleStable()
    {
        var diagnostics = new InteropDiagnostics(42, "Metal");

        diagnostics.CompleteFrame(42);

        Assert.True(diagnostics.IsNativeHandleStable);
        Assert.Equal(1, diagnostics.FrameCount);
    }

    [Fact]
    public void ChangedHandleMarksNativeHandleUnstable()
    {
        var diagnostics = new InteropDiagnostics(42, "Metal");

        diagnostics.CompleteFrame(43);

        Assert.False(diagnostics.IsNativeHandleStable);
    }
}
