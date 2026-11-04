using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de la pantalla de Bienvenida (Views/WelcomePage.xaml).
// Constructor primario: los parámetros entre paréntesis (auth, ingresoGoogle,
// nav, dialog) los entrega la INYECCIÓN DE DEPENDENCIAS configurada en
// MauiProgram.cs. El ViewModel no hace "new AuthService()": pide una
// INTERFAZ (IAuthService) y recibe la implementación registrada (ApiAuthService).
// Por eso pudimos pasar de SQLite a la API sin tocar este archivo.
// =====================================================================
public sealed partial class WelcomeViewModel(
    IAuthService auth,
    IngresoConGoogle ingresoGoogle,
    INavigationService nav,
    IDialogService dialog) : BaseViewModel
{
    // Listo → en la vista: IsVisible="{Binding Listo}" (muestra los botones al terminar de revisar la sesión).
    [ObservableProperty]
    private bool listo;

    // Se ejecuta cada vez que aparece la pantalla.
    // Si había una sesión guardada en el celular, entra directo a Inicio.
    public override async Task OnAppearingAsync()
    {
        if (Listo) return;

        IsBusy = true;
        try
        {
            // Si el usuario ya había iniciado sesión, entra directo al inicio.
            if (await auth.RestaurarSesionAsync())
            {
                await Task.Delay(150); // deja terminar la primera presentación de Shell
                await nav.IrAAsync(Routes.Home);
                return;
            }
        }
        catch (Exception ex)
        {
            await dialog.MostrarAsync("Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
            Listo = true;
        }
    }

    // [RelayCommand] sobre el método ContinuarConGoogleAsync GENERA la propiedad
    // ContinuarConGoogleCommand (le saca "Async" y le agrega "Command").
    // En la vista: Command="{Binding ContinuarConGoogleCommand}".
    [RelayCommand]
    private async Task ContinuarConGoogleAsync()
    {
        IsBusy = true;
        try
        {
            await ingresoGoogle.ContinuarAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Comando → vista: botón "Ingresar con DNI". nav.IrAAsync navega con Shell (Helpers/Routes.cs).
    [RelayCommand]
    private Task IrALoginAsync() => nav.IrAAsync(Routes.Login);

    // Comando → vista: texto "Creala acá".
    [RelayCommand]
    private Task IrARegistroAsync() => nav.IrAAsync(Routes.Registro);
}
