using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosSalud.Api.Dtos;
using TurnosSalud.Api.Models;
using TurnosSalud.Api.Servicios;

namespace TurnosSalud.Api.Controllers;

/// <summary>Módulo Cuentas: registro, ingreso con DNI o Google, datos personales y contraseña.</summary>
// =====================================================================
// Controlador de Cuentas: registro, login (DNI y Google), datos personales
// y contraseña. URL base: api/Cuentas.
// Recibe la base (db) y el control de intentos fallidos (intentos) por inyección.
// [FromBody] = el dato viene en el CUERPO del pedido, en JSON (POST y PUT).
// Códigos HTTP que usa: 200 OK · 400 dato inválido · 401 contraseña incorrecta
// o bloqueado · 404 no existe · 409 conflicto (DNI o correo ya registrados).
// =====================================================================
[Route("api/[controller]")]
[ApiController]
public class CuentasController(TurnosSaludContext db, IntentosLogin intentos) : ControllerBase
{
    // POST api/Cuentas/Login   → ingreso con DNI y contraseña (HU02)
    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] LoginPedido pedido)
    {
        var dni = Validador.LimpiarDni(pedido.Dni);
        if (Validador.Dni(dni) is { } errorDni)
            return StatusCode(StatusCodes.Status400BadRequest, new Respuesta<object>(errorDni, null));

        var bloqueo = intentos.SegundosBloqueo(dni);
        if (bloqueo > 0)
            return StatusCode(StatusCodes.Status401Unauthorized, new Respuesta<LoginRespuesta>(
                $"Demasiados intentos fallidos. Esperá {bloqueo} segundos.",
                new LoginRespuesta { Estado = "Bloqueado", SegundosBloqueo = bloqueo }));

        var usuario = await db.Usuarios.Include(u => u.IdObraSocialNavigation).FirstOrDefaultAsync(u => u.Dni == dni);
        if (usuario is null)
            return StatusCode(StatusCodes.Status404NotFound, new Respuesta<LoginRespuesta>(
                "No hay una cuenta con ese DNI.", new LoginRespuesta { Estado = "UsuarioNoRegistrado" }));

        if (!PasswordHasher.Verificar(pedido.Password, usuario.PasswordHash, usuario.PasswordSalt))
        {
            var restantes = intentos.RegistrarFallo(dni);
            return restantes == 0
                ? StatusCode(StatusCodes.Status401Unauthorized, new Respuesta<LoginRespuesta>(
                    "Demasiados intentos fallidos.",
                    new LoginRespuesta { Estado = "Bloqueado", SegundosBloqueo = IntentosLogin.SegundosDeBloqueoTotal }))
                : StatusCode(StatusCodes.Status401Unauthorized, new Respuesta<LoginRespuesta>(
                    "Contraseña incorrecta.",
                    new LoginRespuesta { Estado = "PasswordIncorrecta", IntentosRestantes = restantes }));
        }

        intentos.Limpiar(dni);
        return StatusCode(StatusCodes.Status200OK, new Respuesta<LoginRespuesta>("ok",
            new LoginRespuesta { Estado = "Exitoso", Usuario = Mapeo.ADto(usuario) }));
    }

    // POST api/Cuentas/Registrar   → crea la cuenta del paciente (HU01)
    [HttpPost]
    [Route("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistroPedido pedido)
    {
        var conGoogle = !string.IsNullOrWhiteSpace(pedido.GoogleId);

        // La API vuelve a validar todo: nunca confía solo en la app.
        var error = Validador.Nombre(pedido.Nombre, "nombre")
                    ?? Validador.Nombre(pedido.Apellido, "apellido")
                    ?? Validador.Dni(pedido.Dni)
                    ?? Validador.Email(pedido.Email)
                    ?? Validador.Telefono(pedido.Telefono)
                    ?? Validador.ObraSocial(pedido.ObraSocial)
                    ?? Validador.NumeroAfiliado(pedido.NumeroAfiliado)
                    ?? (conGoogle ? null : Validador.PasswordNueva(pedido.Password));
        if (error is not null)
            return StatusCode(StatusCodes.Status400BadRequest, new Respuesta<object>(error, null));

        var dni = Validador.LimpiarDni(pedido.Dni);
        var email = Validador.NormalizarEmail(pedido.Email);

        if (await db.Usuarios.AnyAsync(u => u.Dni == dni))
            return StatusCode(StatusCodes.Status409Conflict, new Respuesta<RegistroRespuesta>(
                "Ya existe una cuenta con este DNI.", new RegistroRespuesta { Estado = "DniDuplicado" }));
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            return StatusCode(StatusCodes.Status409Conflict, new Respuesta<RegistroRespuesta>(
                "Este correo ya está usado por otra cuenta.", new RegistroRespuesta { Estado = "EmailDuplicado" }));

        var obra = await db.ObrasSociales.FirstOrDefaultAsync(o => o.Nombre == pedido.ObraSocial);
        if (obra is null)
            return StatusCode(StatusCodes.Status400BadRequest, new Respuesta<object>("La obra social no existe.", null));

        // Con Google no se elige contraseña: se guarda una aleatoria que nadie conoce.
        var clave = conGoogle
            ? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            : pedido.Password;
        var (hash, salt) = PasswordHasher.Hash(clave);

        var usuario = new Usuario
        {
            Nombre = Texto.Capitalizar(pedido.Nombre),
            Apellido = Texto.Capitalizar(pedido.Apellido),
            Dni = dni,
            Email = email,
            Telefono = Validador.LimpiarTelefono(pedido.Telefono),
            IdObraSocial = obra.IdObraSocial,
            NumeroAfiliado = pedido.NumeroAfiliado.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            GoogleId = conGoogle ? pedido.GoogleId : null,
            FechaRegistro = Reloj.Ahora
        };

        try
        {
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Respaldo ante un registro simultáneo: los índices únicos de la base lo detectan.
            return StatusCode(StatusCodes.Status409Conflict, new Respuesta<RegistroRespuesta>(
                "Ya existe una cuenta con este DNI o correo.", new RegistroRespuesta { Estado = "DniDuplicado" }));
        }

        usuario.IdObraSocialNavigation = obra;
        return StatusCode(StatusCodes.Status200OK, new Respuesta<RegistroRespuesta>("ok",
            new RegistroRespuesta { Estado = "Exitoso", Usuario = Mapeo.ADto(usuario) }));
    }

    // POST api/Cuentas/BuscarGoogle   → cuenta vinculada a ese Google o con su mismo correo (HU14)
    [HttpPost]
    [Route("BuscarGoogle")]
    public async Task<IActionResult> BuscarGoogle([FromBody] GooglePedido pedido)
    {
        var usuario = await BuscarCuentaGoogle(pedido);
        return usuario is null
            ? StatusCode(StatusCodes.Status404NotFound, new Respuesta<object>("No hay una cuenta con ese correo.", null))
            : StatusCode(StatusCodes.Status200OK, new Respuesta<UsuarioDto>("ok", Mapeo.ADto(usuario)));
    }

    // POST api/Cuentas/LoginGoogle   → ingreso con la cuenta de Google ya verificada por Google (HU14)
    [HttpPost]
    [Route("LoginGoogle")]
    public async Task<IActionResult> LoginGoogle([FromBody] GooglePedido pedido)
    {
        var usuario = await BuscarCuentaGoogle(pedido);
        if (usuario is null)
            return StatusCode(StatusCodes.Status404NotFound, new Respuesta<object>("No hay una cuenta con ese correo.", null));

        if (usuario.GoogleId != pedido.Id)
        {
            usuario.GoogleId = pedido.Id;   // queda vinculada a Google
            await db.SaveChangesAsync();
        }
        return StatusCode(StatusCodes.Status200OK, new Respuesta<UsuarioDto>("ok", Mapeo.ADto(usuario)));
    }

    // GET api/Cuentas/Obtener/5   → datos del paciente (para recordar la sesión)
    [HttpGet]
    [Route("Obtener/{idUsuario:int}")]
    public async Task<IActionResult> ObtenerUsuario(int idUsuario)
    {
        var usuario = await db.Usuarios.Include(u => u.IdObraSocialNavigation).FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        return usuario is null
            ? NotFound(new Respuesta<object>("Paciente no encontrado", null))
            : StatusCode(StatusCodes.Status200OK, new Respuesta<UsuarioDto>("ok", Mapeo.ADto(usuario)));
    }

    // GET api/Cuentas/BuscarPorEmail?email=...   → para avisar si el correo ya es de otra cuenta
    [HttpGet]
    [Route("BuscarPorEmail")]
    public async Task<IActionResult> BuscarPorEmail([FromQuery] string email)
    {
        var normalizado = Validador.NormalizarEmail(email);
        var usuario = await db.Usuarios.Include(u => u.IdObraSocialNavigation).FirstOrDefaultAsync(u => u.Email == normalizado);
        return usuario is null
            ? NotFound(new Respuesta<object>("No hay una cuenta con ese correo.", null))
            : StatusCode(StatusCodes.Status200OK, new Respuesta<UsuarioDto>("ok", Mapeo.ADto(usuario)));
    }

    // GET api/Cuentas/BuscarPorDni?dni=...
    [HttpGet]
    [Route("BuscarPorDni")]
    public async Task<IActionResult> BuscarPorDni([FromQuery] string dni)
    {
        var limpio = Validador.LimpiarDni(dni);
        var usuario = await db.Usuarios.Include(u => u.IdObraSocialNavigation).FirstOrDefaultAsync(u => u.Dni == limpio);
        return usuario is null
            ? NotFound(new Respuesta<object>("No hay una cuenta con ese DNI.", null))
            : StatusCode(StatusCodes.Status200OK, new Respuesta<UsuarioDto>("ok", Mapeo.ADto(usuario)));
    }

    // PUT api/Cuentas/Editar   → datos personales y obra social (HU10)
    [HttpPut]
    [Route("Editar")]
    public async Task<IActionResult> Editar([FromBody] EditarPedido pedido)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == pedido.Id);
        if (usuario is null)
            return NotFound(new Respuesta<object>("Paciente no encontrado", null));

        var error = Validador.Nombre(pedido.Nombre, "nombre")
                    ?? Validador.Nombre(pedido.Apellido, "apellido")
                    ?? Validador.Email(pedido.Email)
                    ?? Validador.Telefono(pedido.Telefono)
                    ?? Validador.ObraSocial(pedido.ObraSocial)
                    ?? Validador.NumeroAfiliado(pedido.NumeroAfiliado);
        if (error is not null)
            return StatusCode(StatusCodes.Status400BadRequest, new Respuesta<object>(error, null));

        var email = Validador.NormalizarEmail(pedido.Email);
        if (await db.Usuarios.AnyAsync(u => u.Email == email && u.IdUsuario != usuario.IdUsuario))
            return StatusCode(StatusCodes.Status409Conflict, new Respuesta<object>("Este correo ya está usado por otra cuenta.", null));

        var obra = await db.ObrasSociales.FirstOrDefaultAsync(o => o.Nombre == pedido.ObraSocial);
        if (obra is null)
            return StatusCode(StatusCodes.Status400BadRequest, new Respuesta<object>("La obra social no existe.", null));

        usuario.Nombre = Texto.Capitalizar(pedido.Nombre);
        usuario.Apellido = Texto.Capitalizar(pedido.Apellido);
        usuario.Email = email;
        usuario.Telefono = Validador.LimpiarTelefono(pedido.Telefono);
        usuario.IdObraSocial = obra.IdObraSocial;
        usuario.NumeroAfiliado = pedido.NumeroAfiliado.Trim();
        await db.SaveChangesAsync();

        usuario.IdObraSocialNavigation = obra;
        return StatusCode(StatusCodes.Status200OK, new Respuesta<UsuarioDto>("ok", Mapeo.ADto(usuario)));
    }

    // PUT api/Cuentas/CambiarPassword   → cambia (o crea, si la cuenta es de Google) la contraseña (HU12)
    [HttpPut]
    [Route("CambiarPassword")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordPedido pedido)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == pedido.UsuarioId);
        if (usuario is null)
            return NotFound(new Respuesta<object>("Paciente no encontrado", null));

        if (Validador.PasswordNueva(pedido.Nueva) is { } error)
            return StatusCode(StatusCodes.Status400BadRequest, new Respuesta<object>(error, null));

        if (pedido.Actual is null)
        {
            // Crear contraseña sin la anterior: solo para cuentas vinculadas a Google.
            if (string.IsNullOrEmpty(usuario.GoogleId))
                return StatusCode(StatusCodes.Status400BadRequest, new Respuesta<object>("Ingresá tu contraseña actual.", null));
        }
        else if (!PasswordHasher.Verificar(pedido.Actual, usuario.PasswordHash, usuario.PasswordSalt))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new Respuesta<object>("La contraseña actual no es correcta.", null));
        }

        var (hash, salt) = PasswordHasher.Hash(pedido.Nueva);
        usuario.PasswordHash = hash;
        usuario.PasswordSalt = salt;
        await db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status200OK, new Respuesta<object>("ok", null));
    }

    // GET api/Cuentas/ObrasSociales   → lista para el registro
    [HttpGet]
    [Route("ObrasSociales")]
    public async Task<IActionResult> ListarObrasSociales()
    {
        var lista = await db.ObrasSociales.OrderBy(o => o.IdObraSocial).Select(o => o.Nombre).ToListAsync();
        return StatusCode(StatusCodes.Status200OK, new Respuesta<List<string>>("ok", lista));
    }

    private async Task<Usuario?> BuscarCuentaGoogle(GooglePedido pedido)
    {
        var email = Validador.NormalizarEmail(pedido.Email);
        return await db.Usuarios.Include(u => u.IdObraSocialNavigation).FirstOrDefaultAsync(u => u.GoogleId == pedido.Id)
               ?? await db.Usuarios.Include(u => u.IdObraSocialNavigation).FirstOrDefaultAsync(u => u.Email == email);
    }
}
