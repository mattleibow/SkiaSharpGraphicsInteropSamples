using Evergine.iOS;
using ObjCRuntime;
using UIKit;

namespace EvergineMauiMetal.Controls;

internal sealed class EvergineAppViewController : EvergineViewController
{
    private bool renderLoopStopped;

    public EvergineAppViewController()
    {
    }

    public EvergineAppViewController(NativeHandle handle)
        : base(handle)
    {
    }

    public event EventHandler? ViewDidLayout;

    public override void LoadView()
    {
        base.LoadView();
        View = new UIView
        {
            BackgroundColor = UIColor.FromRGB(7, 11, 22),
            Opaque = true,
        };
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        ViewDidLayout?.Invoke(this, EventArgs.Empty);
    }

    internal void StopRendering()
    {
        if (renderLoopStopped)
        {
            return;
        }

        renderLoopStopped = true;
        Timer?.Invalidate();
        Timer?.Dispose();
        Timer = null!;

        SurfaceDestroy?.Invoke();
        LoadAction = null!;
        RenderAction = null!;
        SurfaceInitialized = null!;
        SurfaceSizeChange = null!;
        SurfaceInfoChange = null!;
        SurfaceDestroy = null!;
    }
}
