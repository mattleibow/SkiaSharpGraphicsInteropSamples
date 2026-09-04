using EvergineMauiMetal.Interop;

namespace EvergineMauiMetal.Interop.Tests;

public class ZeroCopyDiagnosticsTests
{
    [Fact]
    public void MatchingHandleKeepsZeroCopyProofStable()
    {
        var diagnostics = new ZeroCopyDiagnostics(42, "Metal");

        diagnostics.CompleteFrame(42);

        Assert.True(diagnostics.IsNativeHandleStable);
        Assert.Equal(1, diagnostics.FrameCount);
        Assert.Equal(0, diagnostics.CpuReadbacks);
        Assert.Equal(0, diagnostics.CpuUploadsAfterCreation);
    }

    [Fact]
    public void ChangedHandleInvalidatesStabilityProof()
    {
        var diagnostics = new ZeroCopyDiagnostics(42, "Metal");

        diagnostics.CompleteFrame(43);

        Assert.False(diagnostics.IsNativeHandleStable);
    }
}
