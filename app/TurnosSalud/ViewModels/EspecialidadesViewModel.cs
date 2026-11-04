using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// ViewModel de la pantalla Especialidades (Views/EspecialidadesPage.xaml).
public sealed partial class EspecialidadesViewModel(ICatalogoService catalogo, INavigationService nav, IDialogService dialog)
    : BaseViewModel
{
    // Copia completa de las especialidades traídas de la API (sin filtrar).
    private List<Especialidad> _todas = [];

    // Lo que se ve en la lista (ItemsSource). Se vacía y se vuelve a llenar al filtrar.
    public ObservableCollection<Especialidad> Especialidades { get; } = [];

    // Seleccionada → SelectedItem (doble vía). Filtro → caja de texto (doble vía).
    [ObservableProperty] private Especialidad? seleccionada;
    [ObservableProperty] private string filtro = string.Empty;

    // Carga las especialidades una sola vez (GET api/Catalogo/Especialidades).
    public override async Task OnAppearingAsync()
    {
        Title = "Especialidades";
        if (_todas.Count > 0) return;

        IsBusy = true;
        try
        {
            _todas = await catalogo.GetEspecialidadesAsync();
            AplicarFiltro();
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

    // Se ejecuta solo cada vez que cambia Filtro (cada letra escrita).
    partial void OnFiltroChanged(string value) => AplicarFiltro();

    // Normalizar quita tildes y mayúsculas: "pediatria" encuentra "Pediatría".
    private void AplicarFiltro()
    {
        var f = Formatos.Normalizar(Filtro);
        Especialidades.Clear();
        foreach (var e in _todas.Where(e => f.Length == 0 || Formatos.Normalizar(e.Nombre).Contains(f)))
            Especialidades.Add(e);
    }

    // Comando SelectionChangedCommand: abre los médicos de la especialidad tocada.
    [RelayCommand]
    private async Task AbrirAsync()
    {
        if (Seleccionada is not { } e) return;
        Seleccionada = null; // permite volver a tocar el mismo ítem
        await nav.IrAAsync(Routes.Medicos, new ShellNavigationQueryParameters { { "especialidadId", e.Id } });
    }
}
