using CommunityToolkit.Mvvm.ComponentModel;

namespace TurnosSalud.ViewModels;

// =====================================================================
// BaseViewModel: la clase "madre" de TODOS los ViewModels de la app.
// Lo que está acá lo heredan todos (IsBusy, IsNotBusy, Title, OnAppearingAsync).
//
// ObservableObject (de CommunityToolkit.Mvvm) es la pieza clave del enlace
// de datos: implementa INotifyPropertyChanged, el "aviso" que la pantalla
// escucha. Cada vez que una propiedad cambia, ObservableObject dispara
// PropertyChanged("NombreDeLaPropiedad") y MAUI actualiza los controles
// que tengan {Binding NombreDeLaPropiedad}.
//
// "partial" es obligatorio: el Toolkit genera (al compilar) la otra mitad
// de la clase con el código repetitivo que antes escribíamos a mano.
// =====================================================================
public abstract partial class BaseViewModel : ObservableObject
{
    // [ObservableProperty] sobre un campo privado "isBusy" (minúscula) GENERA
    // una propiedad pública "IsBusy" (mayúscula) que avisa a la pantalla al cambiar.
    // Escribimos 1 línea y el Toolkit escribe por nosotros esto:
    //     public bool IsBusy { get => isBusy; set { isBusy = value; OnPropertyChanged(); } }
    // [NotifyPropertyChangedFor(nameof(IsNotBusy))]: cuando cambia IsBusy,
    // avisa TAMBIÉN que cambió IsNotBusy (porque depende de IsBusy).
    // Uso en las vistas: IsRunning="{Binding IsBusy}" e IsEnabled="{Binding IsNotBusy}".
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    // Title: título de la página. Se enlaza en el .xaml con Title="{Binding Title}".
    [ObservableProperty]
    private string title = string.Empty;

    // Propiedad calculada: lo contrario de IsBusy (para desactivar botones mientras se espera).
    public bool IsNotBusy => !IsBusy;

    // "virtual": cada ViewModel la puede reemplazar (override) para cargar sus datos.
    // La llama el .xaml.cs de cada página en OnAppearing().
    /// <summary>Se llama cada vez que la página aparece en pantalla.</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;
}
