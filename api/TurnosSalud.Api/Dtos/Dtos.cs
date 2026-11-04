namespace TurnosSalud.Api.Dtos;

// =====================================================================================
//  DTO (Data Transfer Object): la forma exacta de los datos que viajan en JSON entre la
//  API y la app. Tienen los mismos nombres que los modelos de la app (Models/*.cs), así
//  la app los recibe sin cambiar ninguna pantalla. Nunca incluyen PasswordHash ni PasswordSalt.
// =====================================================================================

// Convención de nombres: los "...Pedido" llegan DESDE la app (cuerpo del POST o PUT);
// los "...Dto" y "...Respuesta" vuelven HACIA la app.
/// <summary>Respuesta común de todos los endpoints, igual que en el modelo de la cátedra.</summary>
public sealed record Respuesta<T>(string Mensaje, T? Response);

public sealed class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string ObraSocial { get; set; } = "";
    public string NumeroAfiliado { get; set; } = "";
    public string? GoogleId { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public sealed class EspecialidadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Icono { get; set; } = "";
    public string ColorFondo { get; set; } = "#EAF2FF";
    public string ColorBorde { get; set; } = "#C7DCFF";
    public int Orden { get; set; }
    public bool Destacada { get; set; }
}

/// <summary>Médico con el nombre de su sede y sus días ya armados, como los espera la app.</summary>
public sealed class MedicoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public int EspecialidadId { get; set; }
    public string EspecialidadNombre { get; set; } = "";
    public string Matricula { get; set; } = "";
    public string Sede { get; set; } = "";
    public double Calificacion { get; set; }
    public int CantidadResenas { get; set; }
    public string DiasAtencion { get; set; } = "";      // ej. "1,2,3,4,5"
    public string HoraInicio { get; set; } = "";        // "HH:mm"
    public string HoraUltimoTurno { get; set; } = "";   // "HH:mm"
    public int DuracionTurnoMinutos { get; set; }
    public string ColorAvatar { get; set; } = "#0B5ED7";
}

public sealed class TurnoDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int MedicoId { get; set; }
    public DateTime Inicio { get; set; }
    public string Estado { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
}

/// <summary>Turno + médico, igual que Models/TurnoDetalle.cs de la app.</summary>
public sealed class TurnoDetalleDto
{
    public TurnoDto Turno { get; set; } = new();
    public MedicoDto Medico { get; set; } = new();
}

public sealed record HorarioDto(DateTime Inicio, bool Disponible);

// ---------------------- Pedidos (lo que manda la app) ----------------------

public sealed class LoginPedido
{
    public string Dni { get; set; } = "";
    public string Password { get; set; } = "";
}

/// <summary>Resultado del login: Exitoso, UsuarioNoRegistrado, PasswordIncorrecta o Bloqueado.</summary>
public sealed class LoginRespuesta
{
    public string Estado { get; set; } = "";
    public UsuarioDto? Usuario { get; set; }
    public int IntentosRestantes { get; set; }
    public int SegundosBloqueo { get; set; }
}

public sealed class RegistroPedido
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string ObraSocial { get; set; } = "";
    public string NumeroAfiliado { get; set; } = "";
    public string Password { get; set; } = "";
    public string? GoogleId { get; set; }
}

/// <summary>Resultado del registro: Exitoso, DniDuplicado o EmailDuplicado.</summary>
public sealed class RegistroRespuesta
{
    public string Estado { get; set; } = "";
    public UsuarioDto? Usuario { get; set; }
}

public sealed class GooglePedido
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
}

public sealed class EditarPedido
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string ObraSocial { get; set; } = "";
    public string NumeroAfiliado { get; set; } = "";
}

/// <summary>Si Actual es null, se crea la contraseña (solo para cuentas creadas con Google).</summary>
public sealed class CambiarPasswordPedido
{
    public int UsuarioId { get; set; }
    public string? Actual { get; set; }
    public string Nueva { get; set; } = "";
}

public sealed class ReservaPedido
{
    public int UsuarioId { get; set; }
    public int MedicoId { get; set; }
    public DateTime Inicio { get; set; }
    public int? ReprogramaTurnoId { get; set; }
}
