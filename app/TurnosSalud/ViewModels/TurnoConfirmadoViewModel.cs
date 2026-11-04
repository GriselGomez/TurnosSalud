using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// ViewModel del comprobante (Views/TurnoConfirmadoPage.xaml).
public sealed partial class TurnoConfirmadoViewModel(IAgendaService agenda, INavigationService nav)
    : BaseViewModel, IQueryAttributable
{
    private int _turnoId;

    // Turno → {Binding Turno.FechaTexto}, {Binding Turno.Codigo}, etc.
    // Titulo y Subtitulo cambian si fue una reprogramación.
    [ObservableProperty] private TurnoDetalle? turno;
    [ObservableProperty] private string titulo = "¡Turno confirmado!";
    [ObservableProperty] private string subtitulo = "Tu turno ha sido registrado correctamente.";

    // Recibe el Id del turno guardado y si fue reprogramado.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("turnoId", out var t) && t is int id) _turnoId = id;
        if (query.TryGetValue("reprogramado", out var r) && r is true)
        {
            Titulo = "¡Turno reprogramado!";
            Subtitulo = "Tu turno anterior quedó liberado y el nuevo horario está confirmado.";
        }
    }

    // ??= significa "si Turno está vacío, cargalo": pide el detalle a la API (GET api/Turnos/Obtener/{id}).
    public override async Task OnAppearingAsync()
    {
        Turno ??= await agenda.GetTurnoDetalleAsync(_turnoId);
    }

    // Comandos de los botones. ReiniciarEnAsync deja la pantalla elegida como raíz
    // (así "atrás" no vuelve al comprobante ni a la confirmación).
    [RelayCommand]
    private Task VerMisTurnosAsync() => nav.ReiniciarEnAsync(Routes.MisTurnos);

    [RelayCommand]
    private Task VolverAlInicioAsync() => nav.ReiniciarEnAsync(Routes.Home);
}
