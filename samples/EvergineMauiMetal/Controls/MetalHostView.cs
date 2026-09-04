using CoreAnimation;
using CoreGraphics;
using Foundation;
using Metal;
using ObjCRuntime;
using UIKit;

namespace EvergineMauiMetal.Controls;

public sealed class MetalHostView : UIView
{
    public MetalHostView()
    {
        MetalLayer.FramebufferOnly = true;
        MetalLayer.Opaque = true;
        MetalLayer.PixelFormat = MTLPixelFormat.BGRA8Unorm;
    }

    [Export("layerClass")]
    public static Class LayerClass() => new(typeof(CAMetalLayer));

    public Action? LayoutChanged { get; set; }

    public CAMetalLayer MetalLayer => (CAMetalLayer)Layer;

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
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
            LayoutChanged = null;
        }

        base.Dispose(disposing);
    }
}
