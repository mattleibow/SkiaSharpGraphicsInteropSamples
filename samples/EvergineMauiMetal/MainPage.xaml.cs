using EvergineMauiMetal.Game;

namespace EvergineMauiMetal;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        EvergineViewport.ApplicationFactory = static () => new EvergineGameApplication();
    }
}
