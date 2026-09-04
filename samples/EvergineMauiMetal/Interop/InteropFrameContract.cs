namespace EvergineMauiMetal.Interop;

public enum InteropFrameStage
{
    ReadyForSkia,
    SkiaRendering,
    ReadyForEvergine,
    EvergineSampling,
}

public sealed class InteropFrameContract
{
    public InteropFrameStage Stage { get; private set; } = InteropFrameStage.ReadyForSkia;

    public void BeginSkia() => Transition(InteropFrameStage.ReadyForSkia, InteropFrameStage.SkiaRendering);

    public void CompleteSkiaSynchronously() => Transition(InteropFrameStage.SkiaRendering, InteropFrameStage.ReadyForEvergine);

    public void BeginEvergine() => Transition(InteropFrameStage.ReadyForEvergine, InteropFrameStage.EvergineSampling);

    public void CompleteEvergineSynchronously() => Transition(InteropFrameStage.EvergineSampling, InteropFrameStage.ReadyForSkia);

    private void Transition(InteropFrameStage expected, InteropFrameStage next)
    {
        if (Stage != expected)
        {
            throw new InvalidOperationException($"Expected {expected}, but the frame is {Stage}.");
        }

        Stage = next;
    }
}
