using System.Diagnostics;
using Evergine.Common.Graphics;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.iOS;
using Microsoft.Maui.Handlers;
using UIKit;

namespace EvergineMauiMetal.Controls;

public sealed class EvergineViewHandler : ViewHandler<EvergineView, UIView>
{
    private static readonly IPropertyMapper<EvergineView, EvergineViewHandler> PropertyMapper =
        new PropertyMapper<EvergineView, EvergineViewHandler>(ViewMapper);

    private global::Evergine.Framework.Application? evergineApplication;
    private EvergineAppViewController? evergineViewController;
    private bool isViewLoaded;
    private bool isEvergineInitialized;

    public EvergineViewHandler()
        : base(PropertyMapper)
    {
    }

    protected override UIView CreatePlatformView()
    {
        evergineViewController = new EvergineAppViewController();
        ViewController = evergineViewController;
        return evergineViewController.View!;
    }

    protected override void ConnectHandler(UIView platformView)
    {
        base.ConnectHandler(platformView);
        isViewLoaded = false;
        evergineViewController!.ViewDidLayout += OnViewDidLayout;
    }

    protected override void DisconnectHandler(UIView platformView)
    {
        var viewController = evergineViewController;
        if (viewController is not null)
        {
            viewController.ViewDidLayout -= OnViewDidLayout;
            viewController.StopRendering();
        }

        evergineApplication?.Dispose();
        evergineApplication = null;
        isEvergineInitialized = false;
        isViewLoaded = false;

        base.DisconnectHandler(platformView);

        viewController?.Dispose();
        evergineViewController = null;
    }

    private void OnViewDidLayout(object? sender, EventArgs e)
    {
        isViewLoaded = true;
        StartApplication(VirtualView);
    }

    private void StartApplication(EvergineView view)
    {
        if (!isViewLoaded || isEvergineInitialized)
        {
            return;
        }

        var applicationFactory = view.ApplicationFactory
            ?? throw new InvalidOperationException(
                $"{nameof(EvergineView)} requires an {nameof(EvergineView.ApplicationFactory)}.");
        var application = applicationFactory();
        evergineApplication = application;

        var windowsSystem = new IOSWindowsSystem(evergineViewController!);
        application.Container.RegisterInstance(windowsSystem as WindowsSystem);
        var surface = windowsSystem.CreateSurface(0, 0);

        ConfigureGraphics(application, surface);

        var frameClock = Stopwatch.StartNew();
        isEvergineInitialized = true;
        windowsSystem.Run(
            application.Initialize,
            () =>
            {
                var gameTime = frameClock.Elapsed;
                frameClock.Restart();
                application.UpdateFrame(gameTime);
                application.DrawFrame(gameTime);
            });
        evergineViewController!.LoadAction?.Invoke();
    }

    private static void ConfigureGraphics(
        global::Evergine.Framework.Application application,
        Surface surface)
    {
        GraphicsContext graphicsContext = new global::Evergine.Metal.MTLGraphicsContext();
        graphicsContext.CreateDevice();

        var swapChainDescription = new SwapChainDescription
        {
            SurfaceInfo = surface.SurfaceInfo,
            Width = surface.Width,
            Height = surface.Height,
            ColorTargetFormat = PixelFormat.B8G8R8A8_UNorm,
            ColorTargetFlags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
            DepthStencilTargetFormat = PixelFormat.D32_Float,
            DepthStencilTargetFlags = TextureFlags.DepthStencil,
            SampleCount = TextureSampleCount.None,
            IsWindowed = true,
            RefreshRate = 60,
        };

        var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
        swapChain.VerticalSync = true;
        swapChain.FrameBuffer.IntermediateBufferAssociated = false;

        var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
        graphicsPresenter.AddDisplay("DefaultDisplay", new Display(surface, swapChain));
        application.Container.RegisterInstance(graphicsContext);
    }
}
