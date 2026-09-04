using CoreAnimation;
using CoreGraphics;
using Metal;
using UIKit;

namespace EvergineMauiMetal.Controls;

public sealed class MetalHostView : UIView
{
    public MetalHostView()
    {
        MetalLayer = new CAMetalLayer
        {
            FramebufferOnly = true,
            Opaque = true,
            PixelFormat = MTLPixelFormat.BGRA8Unorm,
        };
        Layer.AddSublayer(MetalLayer);
    }

    public Action? LayoutChanged { get; set; }

    public CAMetalLayer MetalLayer { get; }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        MetalLayer.Frame = Bounds;
        var scale = Window?.Screen.Scale ?? UIScreen.MainScreen.Scale;
        MetalLayer.ContentsScale = scale;
        MetalLayer.DrawableSize = new CGSize(
            Math.Max(1, Bounds.Width * scale),
            Math.Max(1, Bounds.Height * scale));
        LayoutChanged?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            MetalLayer.RemoveFromSuperLayer();
            MetalLayer.Dispose();
        }

        base.Dispose(disposing);
    }
}
