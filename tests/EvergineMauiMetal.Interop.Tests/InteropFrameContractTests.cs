using EvergineMauiMetal.Interop;

namespace EvergineMauiMetal.Interop.Tests;

public class InteropFrameContractTests
{
    [Fact]
    public void CompleteFrameReturnsToSkia()
    {
        var contract = new InteropFrameContract();

        contract.BeginSkia();
        contract.CompleteSkiaSynchronously();
        contract.BeginEvergine();
        contract.CompleteEvergineSynchronously();

        Assert.Equal(InteropFrameStage.ReadyForSkia, contract.Stage);
    }

    [Fact]
    public void EngineCannotPresentBeforeSkiaCompletes()
    {
        var contract = new InteropFrameContract();
        contract.BeginSkia();

        var exception = Assert.Throws<InvalidOperationException>(contract.BeginEvergine);

        Assert.Contains(nameof(InteropFrameStage.ReadyForEvergine), exception.Message);
    }

    [Fact]
    public void OutOfOrderUseIsRejected()
    {
        var contract = new InteropFrameContract();

        var exception = Assert.Throws<InvalidOperationException>(contract.BeginEvergine);

        Assert.Contains(nameof(InteropFrameStage.ReadyForEvergine), exception.Message);
    }
}
