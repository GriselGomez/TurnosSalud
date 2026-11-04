using TurnosSalud.Helpers;
using TurnosSalud.Views;

namespace TurnosSalud;

// =====================================================================
// AppShell: la NAVEGACIÓN de la app (Shell de MAUI).
// En AppShell.xaml están las pantallas "raíz" (Bienvenida y las 3 pestañas).
// Acá se registran las pantallas que se APILAN encima (Login, Médicos, etc.).
// Registrar una ruta = darle un nombre corto ("login") a una página, para
// después navegar con Shell.Current.GoToAsync("login"). Los nombres están
// centralizados en Helpers/Routes.cs para no escribirlos mal.
// =====================================================================
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Páginas que se apilan sobre la pestaña actual.
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.Registro, typeof(RegistroPage));
        Routing.RegisterRoute(Routes.Especialidades, typeof(EspecialidadesPage));
        Routing.RegisterRoute(Routes.Medicos, typeof(MedicosPage));
        Routing.RegisterRoute(Routes.MedicoDetalle, typeof(MedicoDetallePage));
        Routing.RegisterRoute(Routes.ConfirmarTurno, typeof(ConfirmarTurnoPage));
        Routing.RegisterRoute(Routes.TurnoConfirmado, typeof(TurnoConfirmadoPage));
        Routing.RegisterRoute(Routes.DatosPersonales, typeof(DatosPersonalesPage));
    }
}
