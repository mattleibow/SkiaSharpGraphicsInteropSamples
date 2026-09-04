using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Services;

namespace EvergineMauiMetal.Game;

internal sealed class EvergineGameApplication : global::Evergine.Framework.Application
{
    private bool initialized;

    public EvergineGameApplication()
    {
        Container.Register<Settings>();
        Container.Register<Clock>();
        Container.Register<TimerFactory>();
        Container.Register<Evergine.Framework.Services.Random>();
        Container.Register<ErrorHandler>();
        Container.Register<ScreenContextManager>();
        Container.Register<GraphicsPresenter>();
        Container.Register<AssetsDirectory>();
        Container.Register<AssetsService>();
        Container.Register<ForegroundTaskSchedulerService>();
        Container.Register<WorkActionScheduler>();
    }

    public override void Initialize()
    {
        // The hosted iOS surface can repeat its load callback during view setup.
        if (initialized)
        {
            return;
        }

        initialized = true;
        base.Initialize();
        Container.Resolve<ScreenContextManager>().To(
            new ScreenContext(new DashboardScene()));
    }
}
