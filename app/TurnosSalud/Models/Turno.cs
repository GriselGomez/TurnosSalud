using SQLite;

namespace TurnosSalud.Models;

// MODELO: un turno (tabla TURNO en SQL Server). Guarda solo los Id del
// usuario y del médico (claves foráneas), no sus datos repetidos.
/// <summary>Turno reservado por un paciente.</summary>
[Table("Turnos")]
public class Turno
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UsuarioId { get; set; }

    [Indexed]
    public int MedicoId { get; set; }

    /// <summary>Fecha y hora de inicio del turno (hora local).</summary>
    [Indexed]
    public DateTime Inicio { get; set; }

    /// <summary>Ver <see cref="EstadosTurno"/>.</summary>
    [NotNull, MaxLength(15)]
    public string Estado { get; set; } = EstadosTurno.Activo;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaCancelacion { get; set; }
}

// Estados posibles de un turno (en SQL Server lo controla un CHECK).
public static class EstadosTurno
{
    public const string Activo = "Activo";
    public const string Cancelado = "Cancelado";
    public const string Reprogramado = "Reprogramado";
}
