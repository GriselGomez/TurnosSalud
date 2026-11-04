using TurnosSalud.Helpers;

namespace TurnosSalud.Services;

/// <summary>
/// Flujo completo de "Continuar con Google", compartido por Bienvenida, Login y Registro.
/// - Si la cuenta ya existe → entra directo.
/// - Si no existe → avisa y abre el Registro con nombre y correo de Google ya cargados.
/// </summary>
// Lógica compartida del botón "Continuar con Google" (lo usan Bienvenida, Login y Registro).
public sealed class IngresoConGoogle(
    IGoogleAuthService google,
    IAuthService auth,
    INavigationService nav,
    IDialogService dialog)
{
    public bool Disponible => google.Disponible;

    /// <summary>Muestra por qué no se puede usar Google todavía. Devuelve true si está disponible.</summary>
    // ¿Está configurado y disponible Google en esta plataforma? Si no, avisa.
    public async Task<bool> VerificarDisponibleAsync()
    {
        if (google.Disponible) return true;

        await dialog.MostrarAsync("Ingreso con Google",
            "Todavía no está configurado en esta app. Hay que cargar las credenciales de Google Cloud en " +
            "Services/GoogleAuthConfig.cs (ver el README). Mientras tanto, ingresá con tu DNI.");
        return false;
    }

    /// <summary>Abre la pantalla de Google. Devuelve null si el usuario cancela.</summary>
    // Abre la pantalla de Google y devuelve los datos de la cuenta (o null si canceló).
    public async Task<GoogleUsuario?> ObtenerCuentaAsync()
    {
        try
        {
            return await google.IniciarAsync();
        }
        catch (Exception ex)
        {
            await dialog.MostrarAsync("No se pudo ingresar con Google", ex.Message);
            return null;
        }
    }

    /// <summary>Botón "Continuar con Google" (Bienvenida y Login).</summary>
    // Si la cuenta ya existe, ingresa a Inicio; si no, abre Registro con los datos de Google.
    public async Task ContinuarAsync()
    {
        if (!await VerificarDisponibleAsync()) return;

        var cuenta = await ObtenerCuentaAsync();
        if (cuenta is null) return;

        var r = await auth.IniciarSesionConGoogleAsync(cuenta);
        if (r.Estado == LoginEstado.Exitoso)
        {
            await nav.IrAAsync(Routes.Home);
            return;
        }

        // Primera vez con esa cuenta: se completa el registro.
        await AvisarDatosVinculadosAsync(cuenta);
        await nav.IrAAsync(Routes.Registro, ParametrosRegistro(cuenta));
    }

    public Task AvisarDatosVinculadosAsync(GoogleUsuario cuenta) => dialog.MostrarAsync(
        "Datos de Google vinculados",
        $"Hola {cuenta.Nombre}, cargamos tu nombre y correo ({cuenta.Email}).\n\n" +
        "Solo resta completar tu DNI, celular y obra social para registrarte.");

    public static ShellNavigationQueryParameters ParametrosRegistro(GoogleUsuario cuenta) => new()
    {
        { "googleId", cuenta.Id },
        { "email", cuenta.Email },
        { "nombre", cuenta.Nombre },
        { "apellido", cuenta.Apellido },
        { "desdeLogin", false }
    };
}
