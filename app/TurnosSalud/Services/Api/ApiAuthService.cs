using System.Net;
using TurnosSalud.Models;

namespace TurnosSalud.Services.Api;

/// <summary>
/// Cuentas de pacientes contra la API (reemplaza a AuthService, que usaba SQLite).
/// Cumple la misma interfaz IAuthService: por eso ninguna pantalla cambia.
/// </summary>
public sealed class ApiAuthService(ApiClient api, SessionService session) : IAuthService
{
    // Formas de las respuestas de Login y Registrar (ver Dtos.cs de la API).
    private sealed class LoginApi
    {
        public string Estado { get; set; } = "";
        public Usuario? Usuario { get; set; }
        public int IntentosRestantes { get; set; }
        public int SegundosBloqueo { get; set; }
    }

    private sealed class RegistroApi
    {
        public string Estado { get; set; } = "";
        public Usuario? Usuario { get; set; }
    }

    // GET api/Cuentas/BuscarPorDni?dni=... (Uri.EscapeDataString protege caracteres especiales en la URL).
    public async Task<Usuario?> BuscarPorDniAsync(string dni) =>
        DatosONull(await api.GetAsync<Usuario>($"api/Cuentas/BuscarPorDni?dni={Uri.EscapeDataString(dni)}"));

    // GET api/Cuentas/BuscarPorEmail?email=...
    public async Task<Usuario?> BuscarPorEmailAsync(string email) =>
        DatosONull(await api.GetAsync<Usuario>($"api/Cuentas/BuscarPorEmail?email={Uri.EscapeDataString(email)}"));

    public async Task<bool> DniRegistradoAsync(string dni) => await BuscarPorDniAsync(dni) is not null;

    public async Task<bool> EmailRegistradoAsync(string email) => await BuscarPorEmailAsync(email) is not null;

    // POST api/Cuentas/Login. La API compara la contraseña con el hash guardado (PBKDF2)
    // y responde Exitoso, UsuarioNoRegistrado, PasswordIncorrecta o Bloqueado.
    // Si es Exitoso, se guarda el usuario en la sesión.
    public async Task<LoginResultado> IniciarSesionAsync(string dni, string password)
    {
        var r = await api.PostAsync<LoginApi>("api/Cuentas/Login", new { dni, password });
        if (r.Datos is null)
            throw new InvalidOperationException(r.Mensaje);

        var estado = Enum.Parse<LoginEstado>(r.Datos.Estado);
        if (estado == LoginEstado.Exitoso && r.Datos.Usuario is { } usuario)
            session.Iniciar(usuario);

        return new LoginResultado(estado, r.Datos.Usuario, r.Datos.IntentosRestantes, r.Datos.SegundosBloqueo);
    }

    // POST api/Cuentas/Registrar. Respuesta: Exitoso, DniDuplicado o EmailDuplicado.
    public async Task<RegistroResultado> RegistrarAsync(NuevoUsuario datos)
    {
        var r = await api.PostAsync<RegistroApi>("api/Cuentas/Registrar", datos);
        if (r.Datos is null)
            throw new InvalidOperationException(r.Mensaje);   // por ejemplo, un dato inválido

        var estado = Enum.Parse<RegistroEstado>(r.Datos.Estado);
        if (estado == RegistroEstado.Exitoso && r.Datos.Usuario is { } usuario)
            session.Iniciar(usuario);

        return new RegistroResultado(estado, r.Datos.Usuario);
    }

    // POST api/Cuentas/BuscarGoogle: ¿ya existe una cuenta con esta cuenta de Google o su correo?
    public async Task<Usuario?> BuscarCuentaGoogleAsync(GoogleUsuario google) =>
        DatosONull(await api.PostAsync<Usuario>("api/Cuentas/BuscarGoogle", google));

    // POST api/Cuentas/LoginGoogle. 404 = no hay cuenta, hay que registrarse.
    public async Task<LoginResultado> IniciarSesionConGoogleAsync(GoogleUsuario google)
    {
        var r = await api.PostAsync<Usuario>("api/Cuentas/LoginGoogle", google);
        if (r.Codigo == HttpStatusCode.NotFound)
            return new LoginResultado(LoginEstado.UsuarioNoRegistrado);
        if (!r.Exito || r.Datos is null)
            throw new InvalidOperationException(r.Mensaje);

        session.Iniciar(r.Datos);
        return new LoginResultado(LoginEstado.Exitoso, r.Datos);
    }

    // PUT api/Cuentas/CambiarPassword con actual = null: crear contraseña (solo cuentas de Google).
    public async Task EstablecerPasswordAsync(int usuarioId, string nueva)
    {
        var r = await api.PutAsync<object>("api/Cuentas/CambiarPassword", new { usuarioId, actual = (string?)null, nueva });
        if (!r.Exito)
            throw new InvalidOperationException(r.Mensaje);
    }

    // Al abrir la app: si hay un Id guardado en el celular, pide ese usuario a la API.
    public async Task<bool> RestaurarSesionAsync()
    {
        var id = session.UsuarioGuardadoId;
        if (id <= 0) return false;

        var r = await api.GetAsync<Usuario>($"api/Cuentas/Obtener/{id}");
        if (r.Exito && r.Datos is not null)
        {
            session.Iniciar(r.Datos);
            return true;
        }

        if (r.Codigo == HttpStatusCode.NotFound)
            session.Cerrar();   // la cuenta ya no existe
        return false;
    }

    // PUT api/Cuentas/Editar: guarda los datos personales en SQL Server.
    public async Task ActualizarDatosAsync(Usuario usuario)
    {
        var r = await api.PutAsync<Usuario>("api/Cuentas/Editar", new
        {
            id = usuario.Id,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Email,
            usuario.Telefono,
            usuario.ObraSocial,
            usuario.NumeroAfiliado
        });
        if (!r.Exito || r.Datos is null)
            throw new InvalidOperationException(r.Mensaje);

        // La API devuelve los datos ya normalizados (nombres capitalizados, correo en minúsculas).
        usuario.Nombre = r.Datos.Nombre;
        usuario.Apellido = r.Datos.Apellido;
        usuario.Email = r.Datos.Email;
        usuario.Telefono = r.Datos.Telefono;
        usuario.ObraSocial = r.Datos.ObraSocial;
        usuario.NumeroAfiliado = r.Datos.NumeroAfiliado;
        session.Actualizar(usuario);
    }

    // PUT api/Cuentas/CambiarPassword. 401 = la contraseña actual no coincide.
    public async Task<bool> CambiarPasswordAsync(int usuarioId, string actual, string nueva)
    {
        var r = await api.PutAsync<object>("api/Cuentas/CambiarPassword", new { usuarioId, actual, nueva });
        if (r.Exito) return true;
        if (r.Codigo == HttpStatusCode.Unauthorized) return false;   // la contraseña actual no es correcta
        throw new InvalidOperationException(r.Mensaje);
    }

    // Cerrar sesión no llama a la API: solo borra la sesión del celular.
    public void CerrarSesion() => session.Cerrar();

    /// <summary>404 = no existe (null). Cualquier otro error se informa.</summary>
    private static T? DatosONull<T>(ResultadoApi<T> r) where T : class
    {
        if (r.Exito) return r.Datos;
        if (r.Codigo == HttpStatusCode.NotFound) return null;
        throw new InvalidOperationException(r.Mensaje);
    }
}
