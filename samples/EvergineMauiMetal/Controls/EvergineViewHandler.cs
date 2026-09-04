using EvergineMauiMetal.Rendering;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace EvergineMauiMetal.Controls;

public sealed class EvergineViewHandler : ViewHandler<EvergineView, MetalHostView>
{
    private MetalInteropRenderer? renderer;
    private NSTimer? frameTimer;

    public EvergineViewHandler()
        : base(ViewMapper)
    {
    }

    protected override MetalHostView CreatePlatformView() =>
        new()
        {
            BackgroundColor = UIColor.FromRGB(7, 11, 22),
            Opaque = true,
        };

    protected override void ConnectHandler(MetalHostView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.LayoutChanged = OnLayoutChanged;
        frameTimer = NSTimer.CreateRepeatingScheduledTimer(
            TimeSpan.FromSeconds(1d / 60d),
            _ => OnFrameTimer());
    }

    protected override void DisconnectHandler(MetalHostView platformView)
    {
        platformView.LayoutChanged = null;
        frameTimer?.Invalidate();
        frameTimer?.Dispose();
        frameTimer = null;
        renderer?.Dispose();
        renderer = null;
        base.DisconnectHandler(platformView);
    }

    private void OnLayoutChanged()
    {
        ResizeRenderer();
    }

    private void OnFrameTimer()
    {
        if (PlatformView.Window is null)
        {
            return;
        }

        if (renderer is null)
        {
            ResizeRenderer();
        }

        renderer?.DrawFrame();
    }

    private void ResizeRenderer()
    {
        var width = PlatformView.Bounds.Width;
        var height = PlatformView.Bounds.Height;
        if (PlatformView.Window is null || width < 1 || height < 1)
        {
            return;
        }

        renderer ??= new MetalInteropRenderer(PlatformView);
        renderer.Resize();
    }
}
