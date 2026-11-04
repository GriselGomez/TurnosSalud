using System.Globalization;
using SQLite;

namespace TurnosSalud.Models;

// MODELO: datos de un profesional. En SQL Server están repartidos en tablas
// normalizadas (MEDICO, ESPECIALIDAD, SEDE, MEDICO_DIA) y la API los junta
// en un solo objeto con esta forma, para que la app no tuviera que cambiar.
/// <summary>Profesional que atiende turnos.</summary>
[Table("Medicos")]
public class Medico
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>"Dr." o "Dra."</summary>
    [MaxLength(5)]
    public string Titulo { get; set; } = "Dr.";

    [NotNull, MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [NotNull, MaxLength(60)]
    public string Apellido { get; set; } = string.Empty;

    [Indexed]
    public int EspecialidadId { get; set; }

    /// <summary>Matrícula (ej: MP 12345).</summary>
    [MaxLength(20)]
    public string Matricula { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Sede { get; set; } = string.Empty;

    public double Calificacion { get; set; }

    public int CantidadResenas { get; set; }

    /// <summary>Días de atención como números de DayOfWeek separados por coma. Ej: "1,2,3,4,5".</summary>
    [MaxLength(20)]
    public string DiasAtencion { get; set; } = "1,2,3,4,5";

    /// <summary>Hora del primer turno (HH:mm).</summary>
    [MaxLength(5)]
    public string HoraInicio { get; set; } = "08:00";

    /// <summary>Hora del último turno (HH:mm), inclusive.</summary>
    [MaxLength(5)]
    public string HoraUltimoTurno { get; set; } = "12:00";

    public int DuracionTurnoMinutos { get; set; } = 30;

    [MaxLength(9)]
    public string ColorAvatar { get; set; } = "#0B5ED7";

    // Estas propiedades calculadas son las que se enlazan en las pantallas:
    // {Binding NombreCompleto}, {Binding Iniciales}, {Binding CalificacionCorta}...
    // ---------- Propiedades calculadas (no se guardan) ----------

    /// <summary>Se completa desde el servicio para mostrar en pantalla.</summary>
    [Ignore]
    public string EspecialidadNombre { get; set; } = string.Empty;

    [Ignore]
    public string NombreCompleto => $"{Titulo} {Nombre} {Apellido}";

    [Ignore]
    public string Iniciales => $"{Nombre[..1]}{Apellido[..1]}".ToUpperInvariant();

    [Ignore]
    public Color AvatarColor => Color.FromArgb(ColorAvatar);

    [Ignore]
    public string CalificacionCorta =>
        $"{Calificacion.ToString("0.0", CultureInfo.InvariantCulture)} ({CantidadResenas})";

    [Ignore]
    public string CalificacionLarga =>
        $"{Calificacion.ToString("0.0", CultureInfo.InvariantCulture)} ({CantidadResenas} reseñas)";

    [Ignore]
    public IReadOnlyList<DayOfWeek> Dias =>
        DiasAtencion.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => (DayOfWeek)int.Parse(d, CultureInfo.InvariantCulture))
                    .ToList();

    [Ignore]
    public string HorarioTexto => $"{Helpers.Formatos.DiasTexto(Dias)} · {HoraInicio} a {HoraUltimoTurno} hs";
}
