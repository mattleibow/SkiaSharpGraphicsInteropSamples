using EvergineMauiMetal.Controls;
using Microsoft.Extensions.Logging;

namespace EvergineMauiMetal;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureMauiHandlers(handlers => handlers.AddHandler<EvergineView, EvergineViewHandler>());

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
