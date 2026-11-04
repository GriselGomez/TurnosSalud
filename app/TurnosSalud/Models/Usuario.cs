using SQLite;

namespace TurnosSalud.Models;

// MODELO (la "M" de MVVM): una clase que solo guarda datos, sin lógica de pantalla.
// Los atributos entre corchetes ([Table], [PrimaryKey], [MaxLength]...) son de SQLite
// (versión anterior, plan B). Con la API no molestan: el JSON se convierte a esta
// misma clase. La tabla real en SQL Server es USUARIO (db/TurnosSaludDB.sql).
/// <summary>Paciente registrado en la aplicación.</summary>
[Table("Usuarios")]
public class Usuario
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [NotNull, MaxLength(60)]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>DNI sin puntos (7 u 8 dígitos). Único.</summary>
    [Unique, NotNull, MaxLength(8)]
    public string Dni { get; set; } = string.Empty;

    /// <summary>Email normalizado en minúsculas. Único.</summary>
    [Unique, NotNull, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Teléfono con código de área, sin 0 ni 15 (10 dígitos).</summary>
    [MaxLength(10)]
    public string Telefono { get; set; } = string.Empty;

    [MaxLength(60)]
    public string ObraSocial { get; set; } = string.Empty;

    [MaxLength(30)]
    public string NumeroAfiliado { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2-SHA256 en Base64. Nunca se guarda la contraseña en texto plano.</summary>
    [NotNull]
    public string PasswordHash { get; set; } = string.Empty;

    [NotNull]
    public string PasswordSalt { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    /// <summary>Identificador de la cuenta de Google vinculada (null si no usa Google).</summary>
    [Indexed, MaxLength(64)]
    public string? GoogleId { get; set; }

    // [Ignore] = propiedad CALCULADA: no se guarda, se arma con otras.
    // Se usan en los {Binding} de las pantallas (NombreCompleto, Iniciales, PrimerNombre).
    [Ignore]
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();

    [Ignore]
    public string Iniciales =>
        $"{(Nombre.Length > 0 ? Nombre[0] : ' ')}{(Apellido.Length > 0 ? Apellido[0] : ' ')}".Trim().ToUpperInvariant();

    [Ignore]
    public string PrimerNombre => Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? Nombre;
}
