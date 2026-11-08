using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Data;
using TurnosSalud.Helpers;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

/// <summary>
/// Ingreso con DNI + contraseña (lo más simple para cualquier edad).
/// Si el DNI no está registrado, ofrece crear la cuenta con el DNI ya cargado.
/// </summary>
public sealed partial class LoginViewModel(
    IAuthService auth,
    IngresoConGoogle ingresoGoogle,
    INavigationService nav,
    IDialogService dialog)
    // IQueryAttributable: permite RECIBIR datos al navegar a esta pantalla
    // (por ejemplo, el DNI que viene desde Registro). Ver ApplyQueryAttributes.
    : BaseViewModel, IQueryAttributable
{
    // true después del primer intento: desde ahí se valida mientras se escribe.
    private bool _intentoEnviar;

    // PROPIEDADES ENLAZADAS (cada [ObservableProperty] genera la propiedad con mayúscula):
    //   Dni, Password       → Text="{Binding Dni}" / "{Binding Password}" (doble vía)
    //   DniError, PasswordError → Error="{Binding DniError}" (texto rojo bajo la caja)
    //   ErrorGeneral        → recuadro rojo (por ejemplo, bloqueo por intentos)
    [ObservableProperty] private string dni = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string? dniError;
    [ObservableProperty] private string? passwordError;
    [ObservableProperty] private string? errorGeneral;

    // Solo lectura (=>): se muestran en el recuadro de la cuenta de demostración.
    public string DniDemo => SeedData.DniDemo;
    public string PasswordDemo => SeedData.PasswordDemo;

    // El recuadro de la cuenta de demostración solo se muestra al probar desde
    // Visual Studio (Debug). En el APK (Release) queda oculto.
#if DEBUG
    public bool MostrarCuentaDemo => true;
#else
    public bool MostrarCuentaDemo => false;
#endif
    // Recibe parámetros de navegación. Si viene "dni", lo carga en la caja de texto
    // (al asignar Dni, el enlace lo muestra solo en pantalla).
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("dni", out var d) && d is string documento)
            Dni = documento;
    }

    // OnDniChanged lo llama AUTOMÁTICAMENTE el código generado por [ObservableProperty]
    // cada vez que cambia Dni (o sea, cada vez que el usuario escribe una letra).
    // "partial void": nosotros escribimos el cuerpo, el Toolkit lo invoca.
    // Validación en vivo una vez que el usuario intentó ingresar.
    partial void OnDniChanged(string value)
    {
        ErrorGeneral = null;
        if (_intentoEnviar) DniError = Validador.Dni(value);
    }

    partial void OnPasswordChanged(string value)
    {
        ErrorGeneral = null;
        if (_intentoEnviar) PasswordError = Validador.PasswordLogin(value);
    }

    // Comando "Ingresar" (IngresarCommand en la vista).
    // Pasos: 1) valida los campos, 2) llama a la API a través de IAuthService,
    // 3) según la respuesta navega a Inicio o muestra el error que corresponda.
    [RelayCommand]
    private async Task IngresarAsync()
    {
        _intentoEnviar = true;
        DniError = Validador.Dni(Dni);
        PasswordError = Validador.PasswordLogin(Password);
        if (DniError is not null || PasswordError is not null) return;

        IsBusy = true;
        try
        {
            // auth es ApiAuthService → POST api/Cuentas/Login. La respuesta trae un Estado.
            var r = await auth.IniciarSesionAsync(Dni, Password);
            switch (r.Estado)
            {
                case LoginEstado.Exitoso:
                    _intentoEnviar = false;
                    Password = string.Empty;
                    PasswordError = null;
                    await nav.IrAAsync(Routes.Home);
                    break;

                case LoginEstado.UsuarioNoRegistrado:
                    var registrarse = await dialog.ConfirmarAsync(
                        "DNI no registrado",
                        $"Todavía no hay una cuenta con el DNI {Validador.LimpiarDni(Dni)}.\n¿Querés registrarte ahora? Solo se hace una vez.",
                        "Registrarme", "Revisar DNI");
                    if (registrarse)
                        await IrARegistroAsync();
                    else
                        DniError = "Verificá que el DNI esté bien escrito.";
                    break;

                case LoginEstado.PasswordIncorrecta:
                    Password = string.Empty;
                    PasswordError = r.IntentosRestantes <= 2
                        ? $"Contraseña incorrecta. Te quedan {r.IntentosRestantes} intento(s)."
                        : "Contraseña incorrecta.";
                    break;

                case LoginEstado.Bloqueado:
                    Password = string.Empty;
                    PasswordError = null;
                    ErrorGeneral = $"Demasiados intentos fallidos. Esperá {r.SegundosBloqueo} segundos y volvé a intentar.";
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorGeneral = $"Ocurrió un error inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Comando "Creá tu cuenta": navega a Registro LLEVANDO el DNI escrito
    // (ShellNavigationQueryParameters = parámetros de navegación).
    [RelayCommand]
    private Task IrARegistroAsync() =>
        nav.IrAAsync(Routes.Registro, new ShellNavigationQueryParameters
        {
            { "dni", Validador.LimpiarDni(Dni) },
            { "desdeLogin", true }
        });

    // Comando "Continuar con Google".
    [RelayCommand]
    private async Task ContinuarConGoogleAsync()
    {
        ErrorGeneral = null;
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

    // Comando "Completar automáticamente": carga la cuenta demo. Como Dni y Password
    // están enlazados en doble vía, las cajas se llenan solas.
    [RelayCommand]
    private void UsarCuentaDemo()
    {
        Dni = SeedData.DniDemo;
        Password = SeedData.PasswordDemo;
    }

    // Comando "¿Olvidaste tu contraseña?": por ahora solo muestra un aviso.
    [RelayCommand]
    private Task OlvideMiPasswordAsync() => dialog.MostrarAsync(
        "Recuperar contraseña",
        "La recuperación de contraseña por correo todavía no está disponible. " +
        "Por ahora, comunicate con el centro de salud para que te ayuden a recuperar tu cuenta.");
}
