using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de Confirmar turno (Views/ConfirmarTurnoPage.xaml).
// Prepara los textos del resumen y, al confirmar, guarda el turno en la API.
// =====================================================================
public sealed partial class ConfirmarTurnoViewModel(
    ICatalogoService catalogo,
    IAgendaService agenda,
    SessionService session,
    INavigationService nav,
    IDialogService dialog) : BaseViewModel, IQueryAttributable
{
    // Datos recibidos desde la agenda del médico.
    private int _medicoId;
    private DateTime _inicio;
    private int? _reprogramaTurnoId;

    // Cada propiedad es un texto YA ARMADO para la vista (FechaTexto, HoraTexto,
    // PacienteTexto...). La vista solo hace {Binding FechaTexto}; no calcula nada.
    [ObservableProperty] private Medico? medico;
    [ObservableProperty] private string fechaTexto = string.Empty;
    [ObservableProperty] private string horaTexto = string.Empty;
    [ObservableProperty] private string pacienteTexto = string.Empty;
    [ObservableProperty] private string obraSocialTexto = string.Empty;
    [ObservableProperty] private bool esReprogramacion;
    [ObservableProperty] private string textoBoton = "Confirmar turno";

    // Recibe medicoId, inicio (DateTime) y opcionalmente reprogramaTurnoId.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("medicoId", out var m) && m is int medicoId) _medicoId = medicoId;
        if (query.TryGetValue("inicio", out var i) && i is DateTime inicio) _inicio = inicio;
        if (query.TryGetValue("reprogramaTurnoId", out var t) && t is int turnoId)
        {
            _reprogramaTurnoId = turnoId;
            EsReprogramacion = true;
        }
    }

    // Arma el título, el texto del botón y el resumen (médico, fecha, paciente, cobertura).
    public override async Task OnAppearingAsync()
    {
        Title = EsReprogramacion ? "Confirmar reprogramación" : "Confirmar turno";
        TextoBoton = EsReprogramacion ? "Confirmar nuevo horario" : "Confirmar turno";

        if (Medico is not null) return;

        Medico = await catalogo.GetMedicoAsync(_medicoId);
        FechaTexto = Formatos.FechaLarga(_inicio);
        HoraTexto = $"{_inicio:HH:mm} hs";

        if (session.UsuarioActual is { } u)
        {
            PacienteTexto = $"{u.NombreCompleto} · DNI {u.Dni}";
            ObraSocialTexto = string.IsNullOrWhiteSpace(u.NumeroAfiliado)
                ? u.ObraSocial
                : $"{u.ObraSocial} · Nº {u.NumeroAfiliado}";
        }
    }

    // Comando "Confirmar turno". Llama a la API (POST api/Turnos/Reservar).
    // Si la API rechaza (por ejemplo 409: alguien reservó ese horario hace un instante),
    // muestra el motivo y vuelve a la agenda, que se actualiza sola.
    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        var usuario = session.UsuarioActual;
        if (usuario is null || Medico is null) return;

        IsBusy = true;
        try
        {
            var r = await agenda.ReservarAsync(usuario.Id, Medico.Id, _inicio, _reprogramaTurnoId);
            if (!r.Exito)
            {
                await dialog.MostrarAsync("No se pudo reservar", r.Error ?? "Intentá nuevamente.");
                await nav.VolverAsync(); // vuelve a la agenda, que se actualiza sola
                return;
            }

            // Reemplaza esta página por la de confirmación (así "atrás" no vuelve a confirmar).
            // "../" = sacar esta página de la pila y abrir la de confirmación.
            await nav.IrAAsync($"../{Routes.TurnoConfirmado}", new ShellNavigationQueryParameters
            {
                { "turnoId", r.Turno!.Id },
                { "reprogramado", EsReprogramacion }
            });
        }
        catch (Exception ex)
        {
            await dialog.MostrarAsync("Error", $"No se pudo guardar el turno: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Comando "Cancelar": vuelve atrás sin guardar.
    [RelayCommand]
    private Task CancelarAsync() => nav.VolverAsync();
}
