namespace TurnosSalud.WinUI;

/// <summary>
/// Punto de entrada de la aplicación en Windows.
/// </summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
