using System.Globalization;

namespace TurnosSalud;

// =====================================================================
// App: la aplicación en sí. App.xaml (el otro archivo) carga los recursos
// compartidos: colores (Resources/Styles/Colors.xaml) y estilos
// (Resources/Styles/Styles.xaml). Por eso cualquier página puede usar
// {StaticResource Primary} o Style="{StaticResource Card}".
// =====================================================================
public partial class App : Application
{
    // Constructor: configura idioma y tema al iniciar.
    public App()
    {
        InitializeComponent();

        // Español de Argentina para formatos de números y fechas.
        var cultura = new CultureInfo("es-AR");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        CultureInfo.CurrentCulture = cultura;
        CultureInfo.CurrentUICulture = cultura;

        // La paleta está pensada para tema claro (contraste garantizado).
        UserAppTheme = AppTheme.Light;
    }

    // Crea la ventana principal con AppShell (la estructura de navegación).
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell()) { Title = "Turnos Salud" };

#if WINDOWS
        // En Windows se muestra con tamaño de teléfono para la demo.
        window.Width = 430;
        window.Height = 880;
        window.MinimumWidth = 360;
        window.MinimumHeight = 640;
#endif
        return window;
    }
}
