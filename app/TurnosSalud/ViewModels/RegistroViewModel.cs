using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Services;

namespace TurnosSalud.ViewModels;

// =====================================================================
// ViewModel de Crear cuenta (Views/RegistroPage.xaml).
// Dos modos: registro normal (con contraseña) o con Google (sin contraseña).
// =====================================================================
public sealed partial class RegistroViewModel(
    IAuthService auth,
    IngresoConGoogle ingresoGoogle,
    INavigationService nav,
    IDialogService dialog)
    : BaseViewModel, IQueryAttributable
{
    // _intentoEnviar: desde el primer intento se valida en vivo.
    // _desdeLogin: para volver al Login sin duplicar pantallas.
    // _googleId: identificador de la cuenta de Google (si se registra con Google).
    private bool _intentoEnviar;
    private bool _desdeLogin;
    private string? _googleId;

    /// <summary>True cuando la cuenta se crea con Google: no se pide contraseña y el correo es el de Google.</summary>
    // EsGoogle → IsVisible de los avisos de Google e IsReadOnly del correo.
    // Al cambiar avisa también PideContrasena (lo contrario), que muestra u oculta
    // los campos de contraseña. Un solo cambio reacomoda toda la pantalla.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PideContrasena))]
    private bool esGoogle;

    public bool PideContrasena => !EsGoogle;

    // Opciones del Picker de obra social.
    public List<string> ObrasSocialesDisponibles { get; } = [.. ObrasSociales.Lista];

    // Datos del formulario: cada uno enlazado en doble vía con su caja (Text="{Binding Nombre}"...).
    // AceptaTerminos → IsChecked del CheckBox.
    [ObservableProperty] private string nombre = string.Empty;
    [ObservableProperty] private string apellido = string.Empty;
    [ObservableProperty] private string dni = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string telefono = string.Empty;
    [ObservableProperty] private string? obraSocial;
    [ObservableProperty] private string numeroAfiliado = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmarPassword = string.Empty;
    [ObservableProperty] private bool aceptaTerminos;

    // Errores de cada campo → Error="{Binding NombreError}"...
    [ObservableProperty] private string? nombreError;
    [ObservableProperty] private string? apellidoError;
    [ObservableProperty] private string? dniError;
    [ObservableProperty] private string? emailError;
    [ObservableProperty] private string? telefonoError;
    [ObservableProperty] private string? obraSocialError;
    [ObservableProperty] private string? numeroAfiliadoError;
    [ObservableProperty] private string? passwordError;
    [ObservableProperty] private string? confirmarPasswordError;
    [ObservableProperty] private string? terminosError;

    // Recibe parámetros: el DNI desde Login, o los datos de Google
    // (googleId, email, nombre, apellido) desde el ingreso con Google.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("dni", out var d0) && d0 is string documento && string.IsNullOrWhiteSpace(Dni))
            Dni = documento;
        if (query.TryGetValue("desdeLogin", out var d) && d is bool desde)
            _desdeLogin = desde;

        if (query.TryGetValue("googleId", out var g) && g is string googleId && googleId.Length > 0)
        {
            _googleId = googleId;
            EsGoogle = true;
            if (query.TryGetValue("email", out var e) && e is string mail) Email = mail;
            if (query.TryGetValue("nombre", out var n) && n is string nom && string.IsNullOrWhiteSpace(Nombre)) Nombre = nom;
            if (query.TryGetValue("apellido", out var a) && a is string ape && string.IsNullOrWhiteSpace(Apellido)) Apellido = ape;
        }
    }

    /// <summary>Vuelve al login (sin duplicar páginas en la pila) llevando el DNI cargado.</summary>
    private Task VolverAlLoginAsync()
    {
        var parametros = new ShellNavigationQueryParameters { { "dni", Validador.LimpiarDni(Dni) } };
        return _desdeLogin
            ? nav.IrAAsync("..", parametros)
            : nav.IrAAsync($"../{Routes.Login}", parametros);
    }

    // Métodos automáticos OnXxxChanged (los llama el código generado por [ObservableProperty]
    // cada vez que cambia el dato). Revalidan mientras se escribe y el error rojo
    // aparece o desaparece en vivo.
    // Revalida cada campo mientras se escribe (solo después del primer intento).
    partial void OnNombreChanged(string value) { if (_intentoEnviar) NombreError = Validador.Nombre(value, "nombre"); }
    partial void OnApellidoChanged(string value) { if (_intentoEnviar) ApellidoError = Validador.Nombre(value, "apellido"); }
    partial void OnDniChanged(string value) { if (_intentoEnviar) DniError = Validador.Dni(value); }
    partial void OnEmailChanged(string value) { if (_intentoEnviar) EmailError = Validador.Email(value); }
    partial void OnTelefonoChanged(string value) { if (_intentoEnviar) TelefonoError = Validador.Telefono(value); }
    partial void OnObraSocialChanged(string? value) { if (_intentoEnviar) ObraSocialError = Validador.ObraSocial(value); }
    partial void OnNumeroAfiliadoChanged(string value) { if (_intentoEnviar) NumeroAfiliadoError = Validador.NumeroAfiliado(value); }
    partial void OnPasswordChanged(string value)
    {
        if (!_intentoEnviar || EsGoogle) return;
        PasswordError = Validador.PasswordNueva(value);
        ConfirmarPasswordError = Validador.ConfirmarPassword(value, ConfirmarPassword);
    }
    partial void OnConfirmarPasswordChanged(string value)
    {
        if (_intentoEnviar && !EsGoogle) ConfirmarPasswordError = Validador.ConfirmarPassword(Password, value);
    }
    partial void OnAceptaTerminosChanged(bool value) { if (_intentoEnviar) TerminosError = value ? null : "Tenés que aceptar los términos para continuar."; }

    // Valida todos los campos y devuelve true si no hay ningún error.
    private bool ValidarTodo()
    {
        NombreError = Validador.Nombre(Nombre, "nombre");
        ApellidoError = Validador.Nombre(Apellido, "apellido");
        DniError = Validador.Dni(Dni);
        EmailError = Validador.Email(Email);
        TelefonoError = Validador.Telefono(Telefono);
        ObraSocialError = Validador.ObraSocial(ObraSocial);
        NumeroAfiliadoError = Validador.NumeroAfiliado(NumeroAfiliado);
        PasswordError = EsGoogle ? null : Validador.PasswordNueva(Password);
        ConfirmarPasswordError = EsGoogle ? null : Validador.ConfirmarPassword(Password, ConfirmarPassword);
        TerminosError = AceptaTerminos ? null : "Tenés que aceptar los términos para continuar.";

        return new[]
        {
            NombreError, ApellidoError, DniError, EmailError, TelefonoError, ObraSocialError,
            NumeroAfiliadoError, PasswordError, ConfirmarPasswordError, TerminosError
        }.All(e => e is null);
    }

    // Comando "Crear cuenta": valida y llama a la API (POST api/Cuentas/Registrar).
    // La API responde con un Estado: Exitoso, DniDuplicado o EmailDuplicado (409).
    [RelayCommand]
    private async Task RegistrarAsync()
    {
        _intentoEnviar = true;
        if (!ValidarTodo())
        {
            await dialog.MostrarAsync("Revisá los datos", "Hay campos con errores. Corregilos para continuar.");
            return;
        }

        IsBusy = true;
        try
        {
            var r = await auth.RegistrarAsync(new NuevoUsuario
            {
                Nombre = Nombre,
                Apellido = Apellido,
                Dni = Dni,
                Email = Email,
                Telefono = Telefono,
                ObraSocial = ObraSocial!,
                NumeroAfiliado = NumeroAfiliado,
                Password = Password,
                GoogleId = _googleId
            });

            switch (r.Estado)
            {
                case RegistroEstado.Exitoso:
                    await dialog.MostrarAsync("¡Cuenta creada!", $"Bienvenido/a, {r.Usuario!.PrimerNombre}. Ya podés sacar tus turnos.", "Comenzar");
                    await nav.IrAAsync(Routes.Home);
                    break;

                case RegistroEstado.DniDuplicado:
                    DniError = "Ya existe una cuenta con este DNI.";
                    if (await dialog.ConfirmarAsync("Ya tenés una cuenta",
                            "Ese DNI ya está registrado. ¿Querés ingresar con tu contraseña?", "Ingresar", "Cancelar"))
                    {
                        await VolverAlLoginAsync();
                    }
                    break;

                case RegistroEstado.EmailDuplicado:
                    EmailError = EsGoogle
                        ? "Ya existe una cuenta con este correo."
                        : "Este correo ya está usado por otra cuenta.";
                    break;
            }
        }
        catch (Exception ex)
        {
            await dialog.MostrarAsync("Error", $"No se pudo crear la cuenta: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// "Registrarse con Google": trae nombre y correo de Google al formulario.
    /// Si ese correo ya tiene cuenta, ofrece ingresar directamente.
    /// </summary>
    [RelayCommand]
    private async Task RegistrarseConGoogleAsync()
    {
        if (!await ingresoGoogle.VerificarDisponibleAsync()) return;

        IsBusy = true;
        try
        {
            var cuenta = await ingresoGoogle.ObtenerCuentaAsync();
            if (cuenta is null) return;

            var existente = await auth.BuscarCuentaGoogleAsync(cuenta);
            if (existente is not null)
            {
                var ingresar = await dialog.ConfirmarAsync("Ya tenés una cuenta",
                    $"El correo {cuenta.Email} ya está registrado a nombre de {existente.NombreCompleto}.\n\n" +
                    "¿Querés ingresar a tu cuenta directamente?",
                    "Ingresar", "Cancelar");
                if (!ingresar) return;

                var r = await auth.IniciarSesionConGoogleAsync(cuenta);
                if (r.Estado == LoginEstado.Exitoso)
                    await nav.IrAAsync(Routes.Home);
                return;
            }

            // Cuenta nueva: cargamos los datos de Google en este mismo formulario.
            _googleId = cuenta.Id;
            EsGoogle = true;
            Email = cuenta.Email;
            if (!string.IsNullOrWhiteSpace(cuenta.Nombre)) Nombre = cuenta.Nombre;
            if (!string.IsNullOrWhiteSpace(cuenta.Apellido)) Apellido = cuenta.Apellido;
            EmailError = NombreError = ApellidoError = PasswordError = ConfirmarPasswordError = null;

            await ingresoGoogle.AvisarDatosVinculadosAsync(cuenta);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Comando "Iniciá sesión".
    [RelayCommand]
    private Task IrALoginAsync() => VolverAlLoginAsync();

    // Comando "términos y la política de privacidad".
    [RelayCommand]
    private Task VerTerminosAsync() => dialog.MostrarAsync(
        "Términos y privacidad",
        "• Tus datos personales y de salud se usan solo para gestionar tus turnos.\n" +
        "• Tu contraseña se guarda cifrada y nunca se comparte.\n" +
        "• Podés cancelar o reprogramar tus turnos hasta 2 horas antes.\n" +
        "• Si no podés asistir, cancelá el turno para liberarlo a otro paciente.\n" +
        "(Ley 25.326 de Protección de Datos Personales)");
}
