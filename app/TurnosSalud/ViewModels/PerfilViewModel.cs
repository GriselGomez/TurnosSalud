using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de Mi perfil (Views/PerfilPage.xaml). Es un menú: casi todo son
// comandos enlazados a tarjetas (TapGestureRecognizer Command="{Binding ...}").
// =====================================================================
public sealed partial class PerfilViewModel(
    SessionService session,
    IAuthService auth,
    INavigationService nav,
    IDialogService dialog) : BaseViewModel
{
    // Clave con la que se guarda en el celular si los recordatorios están activos.
    public const string ClaveRecordatorios = "recordatorios_activos";

    // Datos que muestra la tarjeta del usuario y el menú (todos enlazados con {Binding}).
    [ObservableProperty] private string nombreCompleto = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string iniciales = string.Empty;
    [ObservableProperty] private string obraSocial = string.Empty;
    [ObservableProperty] private string estadoNotificaciones = string.Empty;

    // Versión de la app (solo lectura).
    public string Version => $"Turnos Salud v{AppInfo.Current.VersionString} · Formosa Capital";

    // Copia los datos del usuario de la sesión a las propiedades enlazadas.
    // Al asignarlas, la pantalla se actualiza sola.
    public override Task OnAppearingAsync()
    {
        Title = "Mi perfil";
        var u = session.UsuarioActual;
        if (u is null) return nav.IrAAsync(Routes.Welcome);

        NombreCompleto = u.NombreCompleto;
        Email = u.Email;
        Iniciales = u.Iniciales;
        ObraSocial = u.ObraSocial;
        ActualizarEstadoNotificaciones();
        return Task.CompletedTask;
    }

    private void ActualizarEstadoNotificaciones() =>
        EstadoNotificaciones = Preferences.Default.Get(ClaveRecordatorios, true) ? "Activadas" : "Desactivadas";

    // Comandos de navegación del menú.
    [RelayCommand]
    private Task MisTurnosAsync() => nav.IrAAsync(Routes.MisTurnos);

    [RelayCommand]
    private Task DatosPersonalesAsync() => nav.IrAAsync(Routes.DatosPersonales);

    // Comando "Obras sociales": muestra la lista, guarda la elegida en la API
    // (PUT api/Cuentas/Editar) y si falla vuelve a dejar los valores anteriores.
    [RelayCommand]
    private async Task ObrasSocialesAsync()
    {
        var u = session.UsuarioActual;
        if (u is null) return;

        var elegida = await dialog.ElegirAsync($"Obra social actual: {u.ObraSocial}", "Cancelar", ObrasSociales.Lista.ToArray());
        if (elegida is null || elegida == u.ObraSocial) return;

        var anterior = (u.ObraSocial, u.NumeroAfiliado);
        u.ObraSocial = elegida;
        if (elegida == ObrasSociales.Particular) u.NumeroAfiliado = string.Empty;
        try
        {
            await auth.ActualizarDatosAsync(u);
        }
        catch (Exception ex)
        {
            (u.ObraSocial, u.NumeroAfiliado) = anterior;
            await dialog.MostrarAsync("No se pudo guardar", ex.Message);
            return;
        }
        ObraSocial = elegida;
        await dialog.MostrarAsync("Obra social actualizada", $"Ahora figurás con {elegida}. Podés cargar tu número de afiliado en Datos personales.");
    }

    // Comando "Notificaciones": activa o desactiva los recordatorios (se guarda en el celular).
    [RelayCommand]
    private async Task NotificacionesAsync()
    {
        var activas = Preferences.Default.Get(ClaveRecordatorios, true);
        var cambiar = await dialog.ConfirmarAsync("Recordatorios de turnos",
            activas
                ? "Los recordatorios están ACTIVADOS: te avisamos tu próximo turno desde la campana del inicio.\n¿Querés desactivarlos?"
                : "Los recordatorios están DESACTIVADOS.\n¿Querés activarlos?",
            activas ? "Desactivar" : "Activar", "Cancelar");

        if (!cambiar) return;
        Preferences.Default.Set(ClaveRecordatorios, !activas);
        ActualizarEstadoNotificaciones();
    }

    // Comando "Ayuda".
    [RelayCommand]
    private Task AyudaAsync() => dialog.MostrarAsync("Ayuda",
        "¿Cómo saco un turno?\n" +
        "1. Elegí la especialidad en Inicio.\n" +
        "2. Seleccioná el profesional.\n" +
        "3. Elegí día y horario disponible.\n" +
        "4. Confirmá el turno.\n\n" +
        "Podés cancelar o reprogramar hasta 2 horas antes desde \"Mis turnos\".\n\n" +
        "Emergencias médicas: 107  ·  Policía: 911");

    // Comando "Cerrar sesión": borra la sesión guardada y vuelve a Bienvenida.
    [RelayCommand]
    private async Task CerrarSesionAsync()
    {
        var ok = await dialog.ConfirmarAsync("Cerrar sesión", "¿Querés cerrar tu sesión?", "Cerrar sesión", "Cancelar");
        if (!ok) return;

        auth.CerrarSesion();
        await nav.IrAAsync(Routes.Welcome);
    }
}
