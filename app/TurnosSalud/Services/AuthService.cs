using SQLite;
using TurnosSalud.Data;
using TurnosSalud.Helpers;
using TurnosSalud.Models;

namespace TurnosSalud.Services;

// Tipos de resultado compartidos (los usan tanto la versión SQLite como la versión API).
public enum LoginEstado { Exitoso, UsuarioNoRegistrado, PasswordIncorrecta, Bloqueado }

public sealed record LoginResultado(LoginEstado Estado, Usuario? Usuario = null, int IntentosRestantes = 0, int SegundosBloqueo = 0);

public enum RegistroEstado { Exitoso, EmailDuplicado, DniDuplicado }

public sealed record RegistroResultado(RegistroEstado Estado, Usuario? Usuario = null);

public sealed class NuevoUsuario
{
    public required string Nombre { get; init; }
    public required string Apellido { get; init; }
    public required string Dni { get; init; }
    public required string Email { get; init; }
    public required string Telefono { get; init; }
    public required string ObraSocial { get; init; }
    public string NumeroAfiliado { get; init; } = string.Empty;
    public required string Password { get; init; }

    /// <summary>Si se registra con Google, el id de esa cuenta (no se pide contraseña).</summary>
    public string? GoogleId { get; init; }
}

/// <summary>
/// Registro, inicio de sesión y gestión de la cuenta.
/// Incluye bloqueo temporal tras varios intentos fallidos.
/// Implementación con SQLite local de <see cref="IAuthService"/>.
/// </summary>
// =====================================================================
// VERSIÓN ANTERIOR (SQLite, base dentro del celular). Hoy NO se usa:
// en MauiProgram.cs está registrada ApiAuthService. Queda como plan B
// para hacer una demo sin internet (ver comentario en MauiProgram.cs).
// =====================================================================
public sealed class AuthService(DatabaseService database, SessionService session) : IAuthService
{
    private const int MaxIntentos = 5;
    private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(1);

    private readonly Dictionary<string, (int Fallos, DateTime? BloqueadoHasta)> _intentos = new();

    public async Task<Usuario?> BuscarPorEmailAsync(string email)
    {
        var db = await database.GetConexionAsync();
        var normalizado = Validador.NormalizarEmail(email);
        return await db.Table<Usuario>().FirstOrDefaultAsync(u => u.Email == normalizado);
    }

    public async Task<bool> EmailRegistradoAsync(string email) => await BuscarPorEmailAsync(email) is not null;

    public async Task<bool> DniRegistradoAsync(string dni)
    {
        var db = await database.GetConexionAsync();
        var limpio = Validador.LimpiarDni(dni);
        return await db.Table<Usuario>().CountAsync(u => u.Dni == limpio) > 0;
    }

    public async Task<Usuario?> BuscarPorDniAsync(string dni)
    {
        var db = await database.GetConexionAsync();
        var limpio = Validador.LimpiarDni(dni);
        return await db.Table<Usuario>().FirstOrDefaultAsync(u => u.Dni == limpio);
    }

    /// <summary>Ingreso con DNI + contraseña.</summary>
    public async Task<LoginResultado> IniciarSesionAsync(string dni, string password)
    {
        var clave = Validador.LimpiarDni(dni);

        if (_intentos.TryGetValue(clave, out var info) && info.BloqueadoHasta is { } hasta && hasta > DateTime.Now)
            return new LoginResultado(LoginEstado.Bloqueado, SegundosBloqueo: (int)Math.Ceiling((hasta - DateTime.Now).TotalSeconds));

        var usuario = await BuscarPorDniAsync(clave);
        if (usuario is null)
            return new LoginResultado(LoginEstado.UsuarioNoRegistrado);

        // PBKDF2 es intencionalmente costoso: se ejecuta fuera del hilo de UI.
        var ok = await Task.Run(() => PasswordHasher.Verificar(password, usuario.PasswordHash, usuario.PasswordSalt));
        if (!ok)
        {
            var fallos = (_intentos.TryGetValue(clave, out var previo) ? previo.Fallos : 0) + 1;
            if (fallos >= MaxIntentos)
            {
                _intentos[clave] = (0, DateTime.Now.Add(DuracionBloqueo));
                return new LoginResultado(LoginEstado.Bloqueado, SegundosBloqueo: (int)DuracionBloqueo.TotalSeconds);
            }

            _intentos[clave] = (fallos, null);
            return new LoginResultado(LoginEstado.PasswordIncorrecta, IntentosRestantes: MaxIntentos - fallos);
        }

        _intentos.Remove(clave);
        session.Iniciar(usuario);
        return new LoginResultado(LoginEstado.Exitoso, usuario);
    }

    public async Task<RegistroResultado> RegistrarAsync(NuevoUsuario datos)
    {
        var db = await database.GetConexionAsync();
        var email = Validador.NormalizarEmail(datos.Email);
        var dni = Validador.LimpiarDni(datos.Dni);

        if (await db.Table<Usuario>().CountAsync(u => u.Dni == dni) > 0)
            return new RegistroResultado(RegistroEstado.DniDuplicado);
        if (await db.Table<Usuario>().CountAsync(u => u.Email == email) > 0)
            return new RegistroResultado(RegistroEstado.EmailDuplicado);

        // Con Google no se elige contraseña: se guarda una aleatoria que nadie conoce.
        var clave = string.IsNullOrEmpty(datos.GoogleId)
            ? datos.Password
            : Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var (hash, salt) = await Task.Run(() => PasswordHasher.Hash(clave));

        var usuario = new Usuario
        {
            Nombre = Formatos.Capitalizar(datos.Nombre),
            Apellido = Formatos.Capitalizar(datos.Apellido),
            Dni = dni,
            Email = email,
            Telefono = Validador.LimpiarTelefono(datos.Telefono),
            ObraSocial = datos.ObraSocial,
            NumeroAfiliado = datos.NumeroAfiliado.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            GoogleId = datos.GoogleId,
            FechaRegistro = DateTime.Now
        };

        try
        {
            await db.InsertAsync(usuario);
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
        {
            // Respaldo ante un registro simultáneo: los índices únicos lo detectan.
            return new RegistroResultado(ex.Message.Contains("Dni", StringComparison.OrdinalIgnoreCase)
                ? RegistroEstado.DniDuplicado
                : RegistroEstado.EmailDuplicado);
        }

        session.Iniciar(usuario);
        return new RegistroResultado(RegistroEstado.Exitoso, usuario);
    }

    /// <summary>
    /// Ingreso con una cuenta de Google ya verificada por Google.
    /// Busca primero por la cuenta vinculada y, si no, por el correo (y la vincula).
    /// </summary>
    public async Task<Usuario?> BuscarCuentaGoogleAsync(GoogleUsuario google)
    {
        var db = await database.GetConexionAsync();
        return await db.Table<Usuario>().FirstOrDefaultAsync(u => u.GoogleId == google.Id)
               ?? await BuscarPorEmailAsync(google.Email);
    }

    public async Task<LoginResultado> IniciarSesionConGoogleAsync(GoogleUsuario google)
    {
        var db = await database.GetConexionAsync();
        var usuario = await BuscarCuentaGoogleAsync(google);

        if (usuario is null)
            return new LoginResultado(LoginEstado.UsuarioNoRegistrado);

        if (usuario.GoogleId != google.Id)
        {
            usuario.GoogleId = google.Id;
            await db.UpdateAsync(usuario);
        }

        session.Iniciar(usuario);
        return new LoginResultado(LoginEstado.Exitoso, usuario);
    }

    /// <summary>Restaura la sesión guardada al abrir la app.</summary>
    public async Task<bool> RestaurarSesionAsync()
    {
        var id = session.UsuarioGuardadoId;
        if (id <= 0) return false;

        var db = await database.GetConexionAsync();
        var usuario = await db.FindAsync<Usuario>(id);
        if (usuario is null)
        {
            session.Cerrar();
            return false;
        }

        session.Iniciar(usuario);
        return true;
    }

    public async Task ActualizarDatosAsync(Usuario usuario)
    {
        var db = await database.GetConexionAsync();
        usuario.Nombre = Formatos.Capitalizar(usuario.Nombre);
        usuario.Apellido = Formatos.Capitalizar(usuario.Apellido);
        usuario.Telefono = Validador.LimpiarTelefono(usuario.Telefono);
        usuario.NumeroAfiliado = usuario.NumeroAfiliado.Trim();
        await db.UpdateAsync(usuario);
        session.Actualizar(usuario);
    }

    public async Task<bool> CambiarPasswordAsync(int usuarioId, string actual, string nueva)
    {
        var db = await database.GetConexionAsync();
        var usuario = await db.FindAsync<Usuario>(usuarioId);
        if (usuario is null) return false;

        var ok = await Task.Run(() => PasswordHasher.Verificar(actual, usuario.PasswordHash, usuario.PasswordSalt));
        if (!ok) return false;

        var (hash, salt) = await Task.Run(() => PasswordHasher.Hash(nueva));
        usuario.PasswordHash = hash;
        usuario.PasswordSalt = salt;
        await db.UpdateAsync(usuario);
        session.Actualizar(usuario);
        return true;
    }

    /// <summary>Crea una contraseña sin pedir la anterior (cuentas creadas con Google).</summary>
    public async Task EstablecerPasswordAsync(int usuarioId, string nueva)
    {
        var db = await database.GetConexionAsync();
        var usuario = await db.FindAsync<Usuario>(usuarioId)
                      ?? throw new InvalidOperationException("No se encontró la cuenta.");

        var (hash, salt) = await Task.Run(() => PasswordHasher.Hash(nueva));
        usuario.PasswordHash = hash;
        usuario.PasswordSalt = salt;
        await db.UpdateAsync(usuario);
        session.Actualizar(usuario);
    }

    public void CerrarSesion() => session.Cerrar();
}
