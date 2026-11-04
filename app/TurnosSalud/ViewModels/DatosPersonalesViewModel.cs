using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de Datos personales (Views/DatosPersonalesPage.xaml).
// Patrón de cada campo: una propiedad con el dato (Text, doble vía) y otra
// con su error (Error). Validar = poner texto en XxxError; la vista lo pinta.
// =====================================================================
public sealed partial class DatosPersonalesViewModel(
    SessionService session,
    IAuthService auth,
    INavigationService nav,
    IDialogService dialog) : BaseViewModel
{
    // true cuando ya se cargaron los datos (para no pisar lo que el usuario está editando).
    private bool _cargado;

    // Opciones del Picker de obra social (ItemsSource).
    public List<string> ObrasSocialesDisponibles { get; } = [.. ObrasSociales.Lista];

    // Datos del formulario (doble vía con las cajas de texto).
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string apellido = string.Empty;
    [ObservableProperty] private string dni = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string telefono = string.Empty;
    [ObservableProperty] private string? obraSocial;
    [ObservableProperty] private string numeroAfiliado = string.Empty;

    // Mensajes de error (null = sin error, el texto rojo no se ve).
    [ObservableProperty] private string? nombreError;
    [ObservableProperty] private string? apellidoError;
    [ObservableProperty] private string? emailError;
    [ObservableProperty] private string? telefonoError;
    [ObservableProperty] private string? obraSocialError;
    [ObservableProperty] private string? numeroAfiliadoError;

    /// <summary>Cuenta creada con Google: puede crear una contraseña sin conocer la anterior.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PideActual), nameof(TituloPassword))]
    private bool esCuentaGoogle;

    // Calculadas a partir de EsCuentaGoogle: PideActual → IsVisible del campo
    // "Contraseña actual"; TituloPassword → título de la sección.
    public bool PideActual => !EsCuentaGoogle;
    public string TituloPassword => EsCuentaGoogle ? "Contraseña para ingresar con DNI (opcional)" : "Cambiar contraseña";

    // Campos de contraseña y sus errores.
    [ObservableProperty] private string passwordActual = string.Empty;
    [ObservableProperty] private string passwordNueva = string.Empty;
    [ObservableProperty] private string passwordConfirmacion = string.Empty;
    [ObservableProperty] private string? passwordActualError;
    [ObservableProperty] private string? passwordNuevaError;
    [ObservableProperty] private string? passwordConfirmacionError;

    // Carga los datos del usuario de la sesión en el formulario (una sola vez).
    public override Task OnAppearingAsync()
    {
        Title = "Datos personales";
        if (_cargado) return Task.CompletedTask;

        var u = session.UsuarioActual;
        if (u is null) return nav.IrAAsync(Routes.Welcome);

        Nombre = u.Nombre;
        Apellido = u.Apellido;
        Dni = u.Dni;
        Email = u.Email;
        EsCuentaGoogle = !string.IsNullOrEmpty(u.GoogleId);
        Telefono = u.Telefono;
        ObraSocial = u.ObraSocial;
        NumeroAfiliado = u.NumeroAfiliado;
        _cargado = true;
        return Task.CompletedTask;
    }

    // Comando "Guardar cambios": valida todo con Helpers/Validador.cs, revisa
    // que el correo no sea de otra cuenta y guarda en la API (PUT api/Cuentas/Editar).
    [RelayCommand]
    private async Task GuardarAsync()
    {
        NombreError = Validador.Nombre(Nombre, "nombre");
        ApellidoError = Validador.Nombre(Apellido, "apellido");
        EmailError = Validador.Email(Email);
        TelefonoError = Validador.Telefono(Telefono);
        ObraSocialError = Validador.ObraSocial(ObraSocial);
        NumeroAfiliadoError = Validador.NumeroAfiliado(NumeroAfiliado);

        // Si algún error no es null, no se guarda (los errores ya se ven en pantalla).
        string?[] errores = [NombreError, ApellidoError, EmailError, TelefonoError, ObraSocialError, NumeroAfiliadoError];
        if (errores.Any(e => e is not null))
            return;

        var u = session.UsuarioActual;
        if (u is null) return;

        // El correo (para avisos) puede cambiarse, pero no puede pertenecer a otra cuenta.
        var otro = await auth.BuscarPorEmailAsync(Email);
        if (otro is not null && otro.Id != u.Id)
        {
            EmailError = "Este correo ya está usado por otra cuenta.";
            return;
        }

        IsBusy = true;
        try
        {
            u.Nombre = Nombre;
            u.Email = Validador.NormalizarEmail(Email);
            u.Apellido = Apellido;
            u.Telefono = Telefono;
            u.ObraSocial = ObraSocial!;
            u.NumeroAfiliado = NumeroAfiliado;
            await auth.ActualizarDatosAsync(u);

            Nombre = u.Nombre;
            Apellido = u.Apellido;
            Email = u.Email;
            await dialog.MostrarAsync("Datos guardados", "Tus datos se actualizaron correctamente.");
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

    // Comando "Actualizar contraseña" (PUT api/Cuentas/CambiarPassword).
    // Cuenta de Google: CREA una contraseña (no pide la actual).
    // Cuenta normal: verifica la actual; si es incorrecta la API responde 401.
    [RelayCommand]
    private async Task CambiarPasswordAsync()
    {
        PasswordActualError = EsCuentaGoogle ? null : Validador.PasswordLogin(PasswordActual);
        PasswordNuevaError = Validador.PasswordNueva(PasswordNueva);
        PasswordConfirmacionError = Validador.ConfirmarPassword(PasswordNueva, PasswordConfirmacion);

        if (!EsCuentaGoogle && PasswordNuevaError is null && PasswordNueva == PasswordActual)
            PasswordNuevaError = "La nueva contraseña debe ser distinta de la actual.";

        if (PasswordActualError is not null || PasswordNuevaError is not null || PasswordConfirmacionError is not null)
            return;

        var u = session.UsuarioActual;
        if (u is null) return;

        IsBusy = true;
        try
        {
            if (EsCuentaGoogle)
            {
                await auth.EstablecerPasswordAsync(u.Id, PasswordNueva);
                PasswordNueva = PasswordConfirmacion = string.Empty;
                await dialog.MostrarAsync("Contraseña creada",
                    $"Listo. Ahora también podés ingresar con tu DNI ({u.Dni}) y esta contraseña.");
                return;
            }

            if (!await auth.CambiarPasswordAsync(u.Id, PasswordActual, PasswordNueva))
            {
                PasswordActualError = "La contraseña actual no es correcta.";
                return;
            }

            PasswordActual = PasswordNueva = PasswordConfirmacion = string.Empty;
            await dialog.MostrarAsync("Contraseña actualizada", "Tu contraseña se cambió correctamente.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
