namespace TurnosSalud.Api.Models;

// =====================================================================================
//  ENTIDADES: una clase por tabla de TurnosSaludDB (ver db/TurnosSaludDB.sql).
//  Son las mismas clases que genera Scaffold-DbContext a partir de la base (database first),
//  escritas a mano y con comentarios para poder explicarlas.
// =====================================================================================

// CÓMO LEERLAS: las propiedades "...Navigation" y las colecciones (virtual ICollection)
// son las RELACIONES entre tablas: desde un Medico se llega a su Especialidad y a su
// Sede sin escribir el JOIN (se cargan con Include en los controladores).
// Las propiedades "Id..." con el comentario FK son las claves foráneas.
/// <summary>Tabla OBRA_SOCIAL (IASEP, PAMI, OSDE...).</summary>
public partial class ObraSocial
{
    public int IdObraSocial { get; set; }
    public string Nombre { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

/// <summary>Tabla ESPECIALIDAD (Clínica Médica, Pediatría...).</summary>
public partial class Especialidad
{
    public int IdEspecialidad { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Icono { get; set; }
    public string? ColorFondo { get; set; }
    public string? ColorBorde { get; set; }
    public int Orden { get; set; }
    public bool Destacada { get; set; }

    public virtual ICollection<Medico> Medicos { get; set; } = new List<Medico>();
}

/// <summary>Tabla SEDE (hospitales y centros de salud).</summary>
public partial class Sede
{
    public int IdSede { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Direccion { get; set; }

    public virtual ICollection<Medico> Medicos { get; set; } = new List<Medico>();
}

/// <summary>Tabla MEDICO.</summary>
public partial class Medico
{
    public int IdMedico { get; set; }
    public string Titulo { get; set; } = "Dr.";
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public int IdEspecialidad { get; set; }          // FK → ESPECIALIDAD
    public int IdSede { get; set; }                  // FK → SEDE
    public string Matricula { get; set; } = null!;
    public decimal Calificacion { get; set; }
    public int CantidadResenas { get; set; }
    public TimeSpan HoraInicio { get; set; }         // primer turno del día
    public TimeSpan HoraUltimoTurno { get; set; }    // último turno del día
    public int DuracionTurnoMinutos { get; set; }
    public string? ColorAvatar { get; set; }

    public virtual Especialidad IdEspecialidadNavigation { get; set; } = null!;
    public virtual Sede IdSedeNavigation { get; set; } = null!;
    public virtual ICollection<MedicoDia> Dias { get; set; } = new List<MedicoDia>();
    public virtual ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
// Clave primaria compuesta (IdMedico, DiaSemana): un médico no puede repetir un día.
// Esta tabla aparece al normalizar: antes los días estaban juntos en un texto "1,2,3".

/// <summary>Tabla MEDICO_DIA: un día de atención por fila (0 = domingo ... 6 = sábado).</summary>
public partial class MedicoDia
{
    public int IdMedico { get; set; }
    public byte DiaSemana { get; set; }

    public virtual Medico IdMedicoNavigation { get; set; } = null!;
}

/// <summary>Tabla USUARIO (paciente registrado).</summary>
public partial class Usuario
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Dni { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Telefono { get; set; } = "";
    public int IdObraSocial { get; set; }            // FK → OBRA_SOCIAL
    public string NumeroAfiliado { get; set; } = "";
    public string PasswordHash { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;
    public string? GoogleId { get; set; }
    public DateTime FechaRegistro { get; set; }

    public virtual ObraSocial IdObraSocialNavigation { get; set; } = null!;
    public virtual ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}

/// <summary>Tabla TURNO.</summary>
public partial class Turno
{
    public int IdTurno { get; set; }
    public int IdUsuario { get; set; }               // FK → USUARIO
    public int IdMedico { get; set; }                // FK → MEDICO
    public DateTime Inicio { get; set; }
    public string Estado { get; set; } = EstadosTurno.Activo;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
    public virtual Medico IdMedicoNavigation { get; set; } = null!;
}

/// <summary>Valores posibles de TURNO.Estado (los mismos del CHECK de la base y de la app).</summary>
public static class EstadosTurno
{
    public const string Activo = "Activo";
    public const string Cancelado = "Cancelado";
    public const string Reprogramado = "Reprogramado";
}
