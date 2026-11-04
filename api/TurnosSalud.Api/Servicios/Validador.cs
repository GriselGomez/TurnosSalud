using System.Text.RegularExpressions;

namespace TurnosSalud.Api.Servicios;

/// <summary>
/// Reglas de validación (copiadas de la app: la API vuelve a validar todo, nunca confía solo en la app). Cada método devuelve null si el valor es válido,
/// o el mensaje de error a mostrar debajo del campo.
/// </summary>
// Copia de las validaciones de la app: el servidor vuelve a revisar los datos
// porque alguien podría llamar a la API sin usar la app (por ejemplo desde Swagger).
public static partial class Validador
{
    [GeneratedRegex(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ' ]+$")]
    private static partial Regex NombreRegex();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex SoloDigitos();

    public static string? Email(string? valor)
    {
        var v = valor?.Trim() ?? string.Empty;
        if (v.Length == 0) return "Ingresá tu correo electrónico.";
        if (v.Length > 120) return "El correo es demasiado largo.";
        if (!EmailRegex().IsMatch(v) || v.Contains("..")) return "Ingresá un correo válido (ej: nombre@email.com).";
        return null;
    }

    public static string? PasswordLogin(string? valor) =>
        string.IsNullOrEmpty(valor) ? "Ingresá tu contraseña." : null;

    /// <summary>Mínimo 8 caracteres, con al menos una letra y un número.</summary>
    public static string? PasswordNueva(string? valor)
    {
        var v = valor ?? string.Empty;
        if (v.Length == 0) return "Creá una contraseña.";
        if (v.Length < 8) return "Debe tener al menos 8 caracteres.";
        if (v.Length > 64) return "Debe tener como máximo 64 caracteres.";
        if (!v.Any(char.IsLetter) || !v.Any(char.IsDigit)) return "Debe combinar letras y números.";
        if (v.Contains(' ')) return "No puede contener espacios.";
        return null;
    }

    public static string? ConfirmarPassword(string? password, string? confirmacion)
    {
        if (string.IsNullOrEmpty(confirmacion)) return "Repetí la contraseña.";
        return password == confirmacion ? null : "Las contraseñas no coinciden.";
    }

    public static string? Nombre(string? valor, string campo)
    {
        var v = valor?.Trim() ?? string.Empty;
        if (v.Length == 0) return $"Ingresá tu {campo}.";
        if (v.Length < 2) return $"El {campo} es demasiado corto.";
        if (v.Length > 60) return $"El {campo} es demasiado largo.";
        if (!NombreRegex().IsMatch(v)) return $"El {campo} solo puede contener letras.";
        return null;
    }

    /// <summary>DNI argentino: 7 u 8 dígitos, sin puntos.</summary>
    public static string? Dni(string? valor)
    {
        var v = (valor ?? string.Empty).Replace(".", string.Empty).Trim();
        if (v.Length == 0) return "Ingresá tu DNI.";
        if (!SoloDigitos().IsMatch(v)) return "El DNI solo puede contener números.";
        if (v.Length is < 7 or > 8) return "El DNI debe tener 7 u 8 dígitos.";
        if (v.TrimStart('0').Length == 0) return "Ingresá un DNI válido.";
        return null;
    }

    /// <summary>Celular con código de área sin 0 ni 15. Ej: 3704123456 (10 dígitos).</summary>
    public static string? Telefono(string? valor)
    {
        var v = LimpiarTelefono(valor);
        if (v.Length == 0) return "Ingresá tu número de celular.";
        if (!SoloDigitos().IsMatch(v)) return "El teléfono solo puede contener números.";
        if (v.Length != 10) return "Ingresá 10 dígitos: código de área sin 0 + número sin 15 (ej: 3704123456).";
        return null;
    }

    public static string? ObraSocial(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "Seleccioná tu obra social (o Particular)." : null;

    public static string? NumeroAfiliado(string? valor)
    {
        var v = valor?.Trim() ?? string.Empty;
        if (v.Length == 0) return null; // opcional
        if (v.Length > 30) return "El número de afiliado es demasiado largo.";
        return v.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '/') ? null : "Usá solo letras, números, - o /.";
    }

    public static string LimpiarDni(string? valor) => (valor ?? string.Empty).Replace(".", string.Empty).Trim();

    public static string LimpiarTelefono(string? valor) =>
        new((valor ?? string.Empty).Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '(' && c != ')').ToArray());

    public static string NormalizarEmail(string? valor) => (valor ?? string.Empty).Trim().ToLowerInvariant();
}
