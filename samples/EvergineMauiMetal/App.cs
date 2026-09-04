using EvergineMauiMetal.Controls;

namespace EvergineMauiMetal;

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new ContentPage
        {
            BackgroundColor = Color.FromArgb("#070B16"),
            Content = new EvergineView(),
        });
}