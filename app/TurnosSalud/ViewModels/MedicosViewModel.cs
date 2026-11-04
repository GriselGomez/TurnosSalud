using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// ViewModel de la pantalla Médicos (Views/MedicosPage.xaml).
// Sirve para dos casos: médicos de una especialidad o resultados de una búsqueda.
public sealed partial class MedicosViewModel(ICatalogoService catalogo, INavigationService nav, IDialogService dialog)
    : BaseViewModel, IQueryAttributable
{
    private int? _especialidadId;
    private string? _busqueda;
    private bool _cargado;

    // Medicos → ItemsSource de la lista.
    public ObservableCollection<Medico> Medicos { get; } = [];

    // Seleccionado → SelectedItem. ResultadosTexto y Encabezado → textos del encabezado.
    [ObservableProperty] private Medico? seleccionado;
    [ObservableProperty] private string resultadosTexto = string.Empty;
    [ObservableProperty] private string encabezado = "Médicos disponibles";

    // Recibe "especialidadId" (desde Inicio o Especialidades) o "busqueda" (desde el buscador).
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("especialidadId", out var id) && id is int esp)
            _especialidadId = esp;
        if (query.TryGetValue("busqueda", out var b) && b is string texto)
            _busqueda = texto;
    }

    // Pide los médicos a la API: por especialidad (GET api/Catalogo/Medicos?especialidadId=)
    // o por texto (GET api/Catalogo/Buscar?texto=). _cargado evita repetir la carga al volver.
    public override async Task OnAppearingAsync()
    {
        if (_cargado) return;

        IsBusy = true;
        try
        {
            List<Medico> lista;
            if (_especialidadId is int id)
            {
                var especialidad = await catalogo.GetEspecialidadAsync(id);
                Title = especialidad?.Nombre ?? "Médicos";
                lista = await catalogo.GetMedicosPorEspecialidadAsync(id);
            }
            else
            {
                Title = "Resultados";
                Encabezado = $"Resultados para \"{_busqueda}\"";
                lista = await catalogo.BuscarMedicosAsync(_busqueda ?? string.Empty);
            }

            Medicos.Clear();
            foreach (var m in lista) Medicos.Add(m);

            ResultadosTexto = lista.Count == 1 ? "1 resultado" : $"{lista.Count} resultados";
            _cargado = true;
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

    // Comando al tocar un médico: abre su agenda (MedicoDetalle) con su Id.
    [RelayCommand]
    private async Task AbrirAsync()
    {
        if (Seleccionado is not { } m) return;
        // Se limpia la selección para poder volver a tocar el mismo médico.
        Seleccionado = null;
        await nav.IrAAsync(Routes.MedicoDetalle, new ShellNavigationQueryParameters { { "medicoId", m.Id } });
    }
}
