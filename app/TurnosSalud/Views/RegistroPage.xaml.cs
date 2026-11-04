using TurnosSalud.ViewModels;

namespace TurnosSalud.Views;

// CÓDIGO DETRÁS (code-behind) de RegistroPage.xaml.
// En MVVM este archivo queda casi vacío A PROPÓSITO: no maneja botones ni textos.
// Solo hace dos cosas: 1) conectar la vista con su ViewModel y 2) avisarle
// cuando la pantalla aparece. Toda la lógica está en ViewModels/RegistroViewModel.cs.
// "partial": la otra mitad de la clase la genera el compilador a partir del .xaml.
public partial class RegistroPage : ContentPage
{
    private readonly RegistroViewModel _vm;

    // El ViewModel llega por el constructor: lo crea la inyección de dependencias
    // (MauiProgram.cs registra la página y su ViewModel).
    public RegistroPage(RegistroViewModel viewModel)
    {
        // Dibuja los controles definidos en el .xaml.
        InitializeComponent();
        // LA LÍNEA CLAVE DEL ENLACE DE DATOS: BindingContext = el ViewModel.
        // Desde acá, cada {Binding Algo} del .xaml busca la propiedad Algo en este ViewModel.
        BindingContext = _vm = viewModel;
    }

    // Evento de MAUI: se dispara cada vez que la pantalla se muestra.
    // Le delega al ViewModel la carga de datos (OnAppearingAsync).
    // Si algo falla, se escribe en la ventana de depuración en vez de cerrar la app.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.OnAppearingAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RegistroPage] {ex}");
        }
    }
}
