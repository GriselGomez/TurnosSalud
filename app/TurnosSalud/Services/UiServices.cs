namespace TurnosSalud.Services;

// ¿Por qué no usar DisplayAlert directo en el ViewModel? Porque el ViewModel
// NO debe conocer las pantallas (regla de MVVM). Pide un IDialogService y
// esta clase es la que realmente muestra el cartel. Lo mismo con la navegación.
/// <summary>Diálogos desacoplados de las vistas (facilita pruebas y mantiene MVVM).</summary>
public interface IDialogService
{
    Task MostrarAsync(string titulo, string mensaje, string aceptar = "Aceptar");
    Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí", string cancelar = "No");
    Task<string?> ElegirAsync(string titulo, string cancelar, params string[] opciones);
}

// Implementación real de los diálogos. MainThread asegura que se muestren en
// el hilo de la interfaz (si no, la app puede fallar).
public sealed class DialogService : IDialogService
{
    private static Page PaginaActual =>
        Shell.Current ?? throw new InvalidOperationException("La interfaz todavía no está lista.");

    // Cartel con un botón (Aceptar).
    public Task MostrarAsync(string titulo, string mensaje, string aceptar = "Aceptar") =>
        MainThread.InvokeOnMainThreadAsync(() => PaginaActual.DisplayAlertAsync(titulo, mensaje, aceptar));

    // Cartel con dos botones (Sí / No). Devuelve true si eligió el primero.
    public Task<bool> ConfirmarAsync(string titulo, string mensaje, string aceptar = "Sí", string cancelar = "No") =>
        MainThread.InvokeOnMainThreadAsync(() => PaginaActual.DisplayAlertAsync(titulo, mensaje, aceptar, cancelar));

    // Lista de opciones (por ejemplo, las obras sociales). Devuelve null si canceló.
    public async Task<string?> ElegirAsync(string titulo, string cancelar, params string[] opciones)
    {
        var elegido = await MainThread.InvokeOnMainThreadAsync(
            () => PaginaActual.DisplayActionSheetAsync(titulo, cancelar, null, opciones));
        return elegido == cancelar ? null : elegido;
    }
}

// Navegación sin que el ViewModel conozca Shell. Los ViewModels llaman a nav.IrAAsync(...).
/// <summary>Navegación de Shell desacoplada de las vistas.</summary>
public interface INavigationService
{
    Task IrAAsync(string ruta);
    Task IrAAsync(string ruta, ShellNavigationQueryParameters parametros);
    Task VolverAsync();
    /// <summary>Vuelve a la raíz de la pestaña actual y luego navega a la ruta absoluta indicada.</summary>
    Task ReiniciarEnAsync(string rutaAbsoluta);
}

public sealed class NavigationService : INavigationService
{
    // GoToAsync = navegar con Shell a una ruta (ver Helpers/Routes.cs).
    public Task IrAAsync(string ruta) =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(ruta));

    // Igual, pero llevando parámetros (los recibe ApplyQueryAttributes del ViewModel destino).
    public Task IrAAsync(string ruta, ShellNavigationQueryParameters parametros) =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(ruta, parametros));

    // ".." = volver a la pantalla anterior.
    public Task VolverAsync() =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));

    // Limpia la pila de pantallas y va a la ruta indicada (para que "atrás" no vuelva a pantallas viejas).
    public Task ReiniciarEnAsync(string rutaAbsoluta) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current.Navigation.NavigationStack.Count > 1)
                await Shell.Current.Navigation.PopToRootAsync(false);
            await Shell.Current.GoToAsync(rutaAbsoluta);
        });
}
