using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de la pantalla Inicio (Views/HomePage.xaml).
// Dependencias que recibe (inyección en MauiProgram.cs):
//   session  → el usuario que inició sesión
//   catalogo → especialidades y médicos (API: api/Catalogo)
//   agenda   → turnos (API: api/Turnos)
//   nav      → navegación entre pantallas
//   dialog   → ventanas de aviso
// =====================================================================
public sealed partial class HomeViewModel(
    SessionService session,
    ICatalogoService catalogo,
    IAgendaService agenda,
    INavigationService nav,
    IDialogService dialog) : BaseViewModel
{
    // ObservableCollection: lista que AVISA cuando se agregan o quitan elementos.
    // En la vista: BindableLayout.ItemsSource="{Binding Accesos}" → una tarjeta por elemento.
    // Con una List común, la pantalla no se enteraría de los cambios.
    public ObservableCollection<AccesoItem> Accesos { get; } = [];

    // Saludo → Text="{Binding Saludo}". TextoBusqueda → caja del buscador (doble vía).
    [ObservableProperty] private string saludo = "Hola";
    [ObservableProperty] private string textoBusqueda = string.Empty;

    // ProximoTurno → la tarjeta "PRÓXIMO TURNO". Al cambiar, avisa también TieneProximoTurno.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneProximoTurno))]
    private TurnoDetalle? proximoTurno;

    // TieneProximoTurno → IsVisible de la tarjeta y del punto naranja de la campana.
    public bool TieneProximoTurno => ProximoTurno is not null;

    // Carga la pantalla: saludo, accesos rápidos (5 especialidades destacadas + "Otras")
    // y el próximo turno del usuario.
    public override async Task OnAppearingAsync()
    {
        var usuario = session.UsuarioActual;
        if (usuario is null)
        {
            await nav.IrAAsync(Routes.Welcome);
            return;
        }

        Saludo = $"Hola, {usuario.PrimerNombre}";

        try
        {
            if (Accesos.Count == 0)
            {
                var especialidades = await catalogo.GetEspecialidadesAsync();
                foreach (var e in especialidades.Where(x => x.Destacada).Take(5))
                {
                    var id = e.Id;
                    // Cada tarjeta recibe la acción a ejecutar al tocarla (abrir esa especialidad).
                    Accesos.Add(new AccesoItem(e.Nombre, e.Icono, e.Fondo, e.Borde, () => AbrirEspecialidadAsync(id)));
                }

                Accesos.Add(new AccesoItem("Otras especialidades", "ic_otras.png",
                    Color.FromArgb("#F1F5F9"), Color.FromArgb("#E2E8F0"), () => nav.IrAAsync(Routes.Especialidades)));
            }

            ProximoTurno = await agenda.GetProximoTurnoAsync(usuario.Id);
        }
        catch (Exception ex)
        {
            await dialog.MostrarAsync("Error", $"No se pudieron cargar los datos: {ex.Message}");
        }
    }

    // Navega a Médicos pasando el Id de la especialidad como parámetro.
    private Task AbrirEspecialidadAsync(int especialidadId) =>
        nav.IrAAsync(Routes.Medicos, new ShellNavigationQueryParameters { { "especialidadId", especialidadId } });

    // Comando del buscador (lupa y tecla Buscar del teclado).
    [RelayCommand]
    private async Task BuscarAsync()
    {
        var texto = TextoBusqueda.Trim();
        if (texto.Length < 2)
        {
            await dialog.MostrarAsync("Buscar", "Escribí al menos 2 letras del médico o la especialidad.");
            return;
        }

        await nav.IrAAsync(Routes.Medicos, new ShellNavigationQueryParameters { { "busqueda", texto } });
    }

    // Comando de la tarjeta "Gestioná tus turnos".
    [RelayCommand]
    private Task GestionarTurnosAsync() => nav.IrAAsync(Routes.MisTurnos);

    // Comando de la tarjeta del próximo turno.
    [RelayCommand]
    private Task VerProximoTurnoAsync() => nav.IrAAsync(Routes.MisTurnos);

    // Comando de la campana: muestra el recordatorio del próximo turno.
    // Preferences = configuración guardada en el celular (recordatorios sí/no).
    [RelayCommand]
    private Task NotificacionesAsync()
    {
        if (!Preferences.Default.Get(PerfilViewModel.ClaveRecordatorios, true))
            return dialog.MostrarAsync("Notificaciones", "Los recordatorios están desactivados. Podés activarlos desde Mi perfil › Notificaciones.");

        if (ProximoTurno is { } t)
        {
            return dialog.MostrarAsync("Recordatorio",
                $"Tenés turno con {t.Medico.NombreCompleto} ({t.Medico.EspecialidadNombre}) el {t.FechaTexto} a las {t.HoraTexto} en {t.Medico.Sede}.\n\nRecordá llegar 10 minutos antes.");
        }

        return dialog.MostrarAsync("Notificaciones", "No tenés notificaciones nuevas.");
    }
}
