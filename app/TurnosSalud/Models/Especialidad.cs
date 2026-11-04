using SQLite;

namespace TurnosSalud.Models;

// MODELO: especialidad médica (tabla ESPECIALIDAD en SQL Server).
/// <summary>Especialidad médica (Clínica Médica, Pediatría, etc.).</summary>
[Table("Especialidades")]
public class Especialidad
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Nombre del archivo de ícono (ej: ic_clinica.png).</summary>
    [MaxLength(40)]
    public string Icono { get; set; } = string.Empty;

    /// <summary>Color de fondo suave en formato #RRGGBB.</summary>
    [MaxLength(9)]
    public string ColorFondo { get; set; } = "#EAF2FF";

    /// <summary>Color del borde de la tarjeta en formato #RRGGBB.</summary>
    [MaxLength(9)]
    public string ColorBorde { get; set; } = "#C7DCFF";

    public int Orden { get; set; }

    /// <summary>Si aparece como acceso rápido en la pantalla de inicio.</summary>
    public bool Destacada { get; set; }

    // Fondo y Borde convierten el texto "#EAF2FF" a un Color, porque
    // {Binding Fondo} en BackgroundColor necesita un Color, no un texto.
    [Ignore]
    public Color Fondo => Color.FromArgb(ColorFondo);

    [Ignore]
    public Color Borde => Color.FromArgb(ColorBorde);
}
