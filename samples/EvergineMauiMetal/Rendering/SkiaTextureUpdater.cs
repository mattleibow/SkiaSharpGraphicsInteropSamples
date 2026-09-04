using System.Diagnostics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Metal;
using EvergineMauiMetal.Interop;

namespace EvergineMauiMetal.Rendering;

internal sealed class SkiaTextureUpdater : Evergine.Framework.Behavior
{
    private const string BackendName = "Evergine Metal + Skia Ganesh Metal";

    private readonly MTLGraphicsContext graphicsContext;
    private readonly MTLTexture engineTexture;
    private readonly SharedTextureOwnership textureOwnership;
    private readonly InteropFrameContract frameContract = new();
    private readonly InteropDiagnostics diagnostics;
    private readonly Stopwatch elapsed = Stopwatch.StartNew();

    [BindService]
    private GraphicsPresenter? graphicsPresenter = null!;

    private SkiaMetalTextureBridge? skiaBridge;
    private GraphicsPresenter? attachedPresenter;

    public SkiaTextureUpdater(
        MTLGraphicsContext graphicsContext,
        MTLTexture engineTexture,
        SharedTextureOwnership textureOwnership)
    {
        this.graphicsContext = graphicsContext;
        this.engineTexture = engineTexture;
        this.textureOwnership = textureOwnership;
        diagnostics = new InteropDiagnostics(engineTexture.NativePointer, BackendName);
    }

    protected override bool OnAttached()
    {
        skiaBridge = new SkiaMetalTextureBridge(graphicsContext, engineTexture);
        textureOwnership.AttachSkiaWrapper(engineTexture.NativePointer);
        attachedPresenter = graphicsPresenter
            ?? throw new InvalidOperationException("Evergine did not bind GraphicsPresenter.");
        attachedPresenter.OnPresented += OnEverginePresented;

        WriteDiagnostic(
            $"Interop initialized: backend={diagnostics.Backend}, " +
            $"shared nativeTexture=0x{diagnostics.NativeTextureHandle:X}.");

        return base.OnAttached();
    }

    protected override void Update(TimeSpan gameTime)
    {
        textureOwnership.ValidateHandle(engineTexture.NativePointer);
        graphicsPresenter!.GraphicsCommandQueue.WaitIdle();

        frameContract.BeginSkia();
        skiaBridge!.RenderDashboard(diagnostics, elapsed.Elapsed);
        frameContract.CompleteSkiaSynchronously();
        frameContract.BeginEvergine();
    }

    protected override void OnDestroy()
    {
        if (skiaBridge is not null)
        {
            attachedPresenter!.OnPresented -= OnEverginePresented;
            attachedPresenter.GraphicsCommandQueue.WaitIdle();
            skiaBridge.Dispose();
            skiaBridge = null;
            attachedPresenter = null;
            textureOwnership.DetachSkiaWrapper(engineTexture.NativePointer);
        }

        base.OnDestroy();
    }

    private void OnEverginePresented(object? sender, EventArgs e)
    {
        frameContract.CompleteEvergineSynchronously();
        diagnostics.CompleteFrame(engineTexture.NativePointer);
        textureOwnership.ValidateHandle(engineTexture.NativePointer);

        if (!diagnostics.IsNativeHandleStable)
        {
            throw new InvalidOperationException("The Evergine-owned native texture handle changed.");
        }

        if (diagnostics.FrameCount % 300 == 0)
        {
            WriteDiagnostic(
                $"Interop status: frame={diagnostics.FrameCount}, " +
                $"native handle stable={diagnostics.IsNativeHandleStable}, backend={diagnostics.Backend}.");
        }
    }

    private static void WriteDiagnostic(string message)
    {
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }
}
