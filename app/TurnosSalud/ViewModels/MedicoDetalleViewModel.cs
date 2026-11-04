using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de la agenda del médico (Views/MedicoDetallePage.xaml).
// Flujo: recibe medicoId → trae el médico → arma los días de atención →
// al elegir un día pide los horarios a la API → al elegir horario habilita
// el botón → "Continuar" navega a Confirmar turno.
// =====================================================================
public sealed partial class MedicoDetalleViewModel(
    ICatalogoService catalogo,
    IAgendaService agenda,
    SessionService session,
    INavigationService nav,
    IDialogService dialog) : BaseViewModel, IQueryAttributable
{
    // Datos recibidos por navegación. Si _reprogramaTurnoId tiene valor, es una reprogramación.
    private int _medicoId;
    private int? _reprogramaTurnoId;

    // Dias → lista horizontal de días. Horarios → grilla de horarios del día elegido.
    public ObservableCollection<DiaItem> Dias { get; } = [];
    public ObservableCollection<HorarioItem> Horarios { get; } = [];

    // Medico → todos los {Binding Medico.Algo} del perfil del profesional.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneMedico))]
    private Medico? medico;

    // Al cambiar el horario elegido, avisa también PuedeContinuar y TextoBoton
    // (el botón de abajo se habilita y cambia su texto solo).
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeContinuar), nameof(TextoBoton))]
    private HorarioItem? horarioSeleccionado;

    // DiaSeleccionado → texto con la fecha elegida. SinHorarios → aviso "No quedan horarios".
    // EsReprogramacion → aviso naranja de reprogramación.
    [ObservableProperty] private DiaItem? diaSeleccionado;
    [ObservableProperty] private bool sinHorarios;
    [ObservableProperty] private bool esReprogramacion;

    // Propiedades CALCULADAS (=>): no guardan nada, se calculan a partir de otras.
    // PuedeContinuar → IsEnabled del botón. TextoBoton → Text del botón.
    public bool TieneMedico => Medico is not null;
    public bool PuedeContinuar => HorarioSeleccionado is not null;

    public string TextoBoton => HorarioSeleccionado is { } h && DiaSeleccionado is { } d
        ? $"Seleccionar turno · {d.DiaSemana} {d.Numero} {h.Hora}"
        : "Elegí un horario";

    // Recibe "medicoId" y, si viene de "Reprogramar", "reprogramaTurnoId".
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("medicoId", out var id) && id is int medicoId)
            _medicoId = medicoId;
        if (query.TryGetValue("reprogramaTurnoId", out var t) && t is int turnoId)
        {
            _reprogramaTurnoId = turnoId;
            EsReprogramacion = true;
        }
    }

    // Al aparecer: trae el médico (GET api/Catalogo/Medico/{id}) la primera vez,
    // y los horarios SIEMPRE, porque otro paciente pudo haber reservado mientras tanto.
    public override async Task OnAppearingAsync()
    {
        IsBusy = true;
        try
        {
            if (Medico is null)
            {
                Medico = await catalogo.GetMedicoAsync(_medicoId);
                if (Medico is null)
                {
                    await dialog.MostrarAsync("Profesional no encontrado", "No pudimos cargar los datos del profesional.");
                    await nav.VolverAsync();
                    return;
                }
                Title = Medico.NombreCompleto;
                CargarDias();
            }

            // Se recargan siempre: otro turno pudo haber ocupado un horario.
            await CargarHorariosAsync();
        }
        catch (Exception ex)
        {
            await dialog.MostrarAsync("Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Arma los próximos días en que atiende el médico (tabla MEDICO_DIA) y marca el primero.
    private void CargarDias()
    {
        Dias.Clear();
        foreach (var fecha in agenda.GetDiasDeAtencion(Medico!))
            // A cada DiaItem le pasamos qué hacer cuando lo toquen (SeleccionarDiaAsync).
            Dias.Add(new DiaItem(fecha, d => _ = SeleccionarDiaAsync(d)));

        var primero = Dias.FirstOrDefault();
        if (primero is not null)
        {
            primero.IsSelected = true;
            DiaSeleccionado = primero;
        }
    }

    // Se ejecuta al tocar un día: marca ese, desmarca los demás y recarga horarios.
    // Cambiar d.IsSelected repinta los días solos (DataTrigger en la vista).
    private async Task SeleccionarDiaAsync(DiaItem dia)
    {
        if (ReferenceEquals(dia, DiaSeleccionado)) return;

        foreach (var d in Dias) d.IsSelected = d == dia;
        DiaSeleccionado = dia;
        await CargarHorariosAsync();
    }

    // Pide a la API los horarios del día (GET api/Turnos/Horarios). La API devuelve
    // cada horario con Disponible true/false según los turnos guardados en SQL Server.
    // Si el usuario ya había elegido una hora y sigue libre, la deja marcada.
    private async Task CargarHorariosAsync()
    {
        var usuario = session.UsuarioActual;
        if (Medico is null || DiaSeleccionado is null || usuario is null)
        {
            Horarios.Clear();
            SinHorarios = true;
            return;
        }

        var horaPrevia = HorarioSeleccionado?.Inicio;
        var horarios = await agenda.GetHorariosAsync(Medico, DiaSeleccionado.Fecha, usuario.Id, _reprogramaTurnoId);

        Horarios.Clear();
        HorarioSeleccionado = null;
        foreach (var h in horarios)
        {
            var item = new HorarioItem(h.Inicio, h.Disponible, SeleccionarHorario);
            if (h.Disponible && h.Inicio == horaPrevia)
            {
                item.IsSelected = true;
                HorarioSeleccionado = item;
            }
            Horarios.Add(item);
        }

        SinHorarios = !horarios.Any(h => h.Disponible);
    }

    // Se ejecuta al tocar un horario. Si está ocupado avisa; si está libre lo marca.
    private void SeleccionarHorario(HorarioItem horario)
    {
        if (!horario.Disponible)
        {
            _ = dialog.MostrarAsync("Horario ocupado", $"El horario de las {horario.Hora} no está disponible. Elegí otro.");
            return;
        }

        foreach (var h in Horarios) h.IsSelected = h == horario;
        HorarioSeleccionado = horario;
    }

    // Cuando cambia el día elegido, el texto del botón también debe cambiar.
    partial void OnDiaSeleccionadoChanged(DiaItem? value) => OnPropertyChanged(nameof(TextoBoton));

    // Comando "Seleccionar turno": navega a Confirmar turno con médico, hora
    // y (si corresponde) el turno que se reprograma.
    [RelayCommand]
    private async Task ContinuarAsync()
    {
        if (Medico is null || HorarioSeleccionado is null)
        {
            await dialog.MostrarAsync("Falta elegir horario", "Seleccioná un día y un horario disponible.");
            return;
        }

        var parametros = new ShellNavigationQueryParameters
        {
            { "medicoId", Medico.Id },
            { "inicio", HorarioSeleccionado.Inicio }
        };
        if (_reprogramaTurnoId is int id)
            parametros.Add("reprogramaTurnoId", id);

        await nav.IrAAsync(Routes.ConfirmarTurno, parametros);
    }
}
