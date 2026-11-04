using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de Mis turnos (Views/MisTurnosPage.xaml).
// Trae todos los turnos del usuario y los separa en Próximos e Historial.
// =====================================================================
public sealed partial class MisTurnosViewModel(
    IAgendaService agenda,
    SessionService session,
    INavigationService nav,
    IDialogService dialog) : BaseViewModel
{
    // Todos los turnos traídos de la API (GET api/Turnos/Usuario/{id}).
    private List<TurnoDetalle> _todos = [];

    // Turnos → ItemsSource de la lista (solo la solapa elegida).
    public ObservableCollection<TurnoItem> Turnos { get; } = [];

    // MostrandoProximos → DataTriggers de las solapas. Al cambiar avisa también
    // MostrandoHistorial y TextoVacio (que dependen de ella).
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoHistorial), nameof(TextoVacio))]
    private bool mostrandoProximos = true;

    // IsRefreshing → RefreshView (tirar hacia abajo). Resumen → "Tenés 2 turnos pendientes".
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string resumen = string.Empty;

    public bool MostrandoHistorial => !MostrandoProximos;

    public string TextoVacio => MostrandoProximos
        ? "No tenés turnos próximos.\nSacá uno en pocos pasos."
        : "Todavía no tenés turnos anteriores.";

    // Cada vez que aparece la pantalla, recarga (puede haber turnos nuevos o cancelados).
    public override async Task OnAppearingAsync()
    {
        Title = "Mis turnos";
        await CargarAsync();
    }

    // Comando de carga. Lo usa OnAppearingAsync y también el RefreshView de la vista.
    [RelayCommand]
    private async Task CargarAsync()
    {
        var usuario = session.UsuarioActual;
        if (usuario is null) return;

        try
        {
            _todos = await agenda.GetTurnosUsuarioAsync(usuario.Id);
            Filtrar();
        }
        catch (Exception ex)
        {
            await dialog.MostrarAsync("Error", ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // Separa: Próximos = activos y en el futuro (ordenados del más cercano).
    // Historial = el resto (pasados, cancelados y reprogramados), del más nuevo al más viejo.
    private void Filtrar()
    {
        var proximos = _todos.Where(t => t.EstaActivo && t.EsFuturo).OrderBy(t => t.Inicio).ToList();
        var historial = _todos.Except(proximos).OrderByDescending(t => t.Inicio).ToList();

        Resumen = proximos.Count switch
        {
            0 => "No tenés turnos pendientes",
            1 => "Tenés 1 turno pendiente",
            var n => $"Tenés {n} turnos pendientes"
        };

        Turnos.Clear();
        foreach (var t in MostrandoProximos ? proximos : historial)
            // Cada TurnoItem recibe los métodos para sus botones Cancelar y Reprogramar.
            Turnos.Add(new TurnoItem(t, CancelarTurnoAsync, ReprogramarTurnoAsync));
    }

    // Comandos de las solapas Próximos / Historial.
    [RelayCommand]
    private void VerProximos()
    {
        MostrandoProximos = true;
        Filtrar();
    }

    [RelayCommand]
    private void VerHistorial()
    {
        MostrandoProximos = false;
        Filtrar();
    }

    // Comando "+ Nuevo" y "Sacar un turno".
    [RelayCommand]
    private Task SacarTurnoAsync() => nav.IrAAsync(Routes.Especialidades);

    // Lo ejecuta el botón "Cancelar turno" de UNA tarjeta (CancelarCommand del TurnoItem).
    // Pide confirmación y llama a la API (PUT api/Turnos/Cancelar/{id}). La API
    // vuelve a verificar la regla de las 2 horas y que el turno sea del usuario.
    private async Task CancelarTurnoAsync(TurnoItem item)
    {
        var t = item.Detalle;
        if (!t.PuedeModificar)
        {
            await dialog.MostrarAsync("No se puede cancelar", "Los turnos solo se pueden cancelar hasta 2 horas antes.");
            return;
        }

        var confirmar = await dialog.ConfirmarAsync("Cancelar turno",
            $"¿Seguro que querés cancelar tu turno con {t.Medico.NombreCompleto} el {t.FechaTexto} a las {t.HoraTexto}?",
            "Sí, cancelar", "No");
        if (!confirmar) return;

        var r = await agenda.CancelarAsync(t.Id, session.UsuarioActual!.Id);
        if (!r.Exito)
        {
            await dialog.MostrarAsync("No se pudo cancelar", r.Error!);
            return;
        }

        await dialog.MostrarAsync("Turno cancelado", "El horario quedó liberado para otro paciente. ¡Gracias por avisar!");
        await CargarAsync();
    }

    // Lo ejecuta el botón "Reprogramar" de UNA tarjeta: abre la agenda del mismo
    // médico en modo reprogramación. El turno viejo se libera recién al confirmar el nuevo.
    private async Task ReprogramarTurnoAsync(TurnoItem item)
    {
        var t = item.Detalle;
        if (!t.PuedeModificar)
        {
            await dialog.MostrarAsync("No se puede reprogramar", "Los turnos solo se pueden reprogramar hasta 2 horas antes.");
            return;
        }

        await nav.IrAAsync(Routes.MedicoDetalle, new ShellNavigationQueryParameters
        {
            { "medicoId", t.Medico.Id },
            { "reprogramaTurnoId", t.Id }
        });
    }
}
