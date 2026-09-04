using System.Diagnostics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Metal;

namespace EvergineMauiMetal.Rendering;

internal sealed class SkiaTextureUpdater : Evergine.Framework.Behavior
{
    private const string BackendName = "Evergine Metal + Skia Ganesh Metal";

    private readonly MTLGraphicsContext graphicsContext;
    private readonly MTLTexture engineTexture;
    private readonly nint nativeTextureHandle;
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private long frameCount;

    [BindService]
    private GraphicsPresenter? graphicsPresenter = null!;

    private SkiaMetalTextureBridge? skiaBridge;
    private GraphicsPresenter? attachedPresenter;

    public SkiaTextureUpdater(
        MTLGraphicsContext graphicsContext,
        MTLTexture engineTexture)
    {
        this.graphicsContext = graphicsContext;
        this.engineTexture = engineTexture;
        nativeTextureHandle = engineTexture.NativePointer;

        if (nativeTextureHandle == 0)
        {
            throw new ArgumentException("The native texture handle must not be null.", nameof(engineTexture));
        }
    }

    protected override bool OnAttached()
    {
        skiaBridge = new SkiaMetalTextureBridge(graphicsContext, engineTexture);
        attachedPresenter = graphicsPresenter
            ?? throw new InvalidOperationException("Evergine did not bind GraphicsPresenter.");
        attachedPresenter.OnPresented += OnEverginePresented;

        WriteDiagnostic(
            $"Interop initialized: backend={BackendName}, " +
            $"shared nativeTexture=0x{nativeTextureHandle:X}.");

        return base.OnAttached();
    }

    protected override void Update(TimeSpan gameTime)
    {
        ValidateNativeHandle();
        graphicsPresenter!.GraphicsCommandQueue.WaitIdle();

        skiaBridge!.RenderDashboard(frameCount, BackendName, elapsed.Elapsed);
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
        }

        base.OnDestroy();
    }

    private void OnEverginePresented(object? sender, EventArgs e)
    {
        ValidateNativeHandle();
        frameCount++;

        if (frameCount % 300 == 0)
        {
            WriteDiagnostic(
                $"Interop status: frame={frameCount}, " +
                $"native handle stable=True, backend={BackendName}.");
        }
    }

    private void ValidateNativeHandle()
    {
        if (engineTexture.NativePointer != nativeTextureHandle)
        {
            throw new InvalidOperationException("The Evergine-owned native texture handle changed.");
        }
    }

    private static void WriteDiagnostic(string message)
    {
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }
}
