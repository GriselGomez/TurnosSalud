using System.Globalization;
using System.Text;

namespace TurnosSalud.Api.Servicios;

/// <summary>Ayudas de texto copiadas de Helpers/Formatos.cs de la app.</summary>
// Ayudantes de texto: quitar tildes para buscar y capitalizar nombres.
public static class Texto
{
    private static readonly CultureInfo Cultura = new("es-AR");

    /// <summary>Quita tildes y pasa a minúsculas para comparar búsquedas ("pediatria" = "Pediatría").</summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>"maría josé" → "María José".</summary>
    public static string Capitalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var palabras = texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => char.ToUpper(p[0], Cultura) + p[1..].ToLower(Cultura));
        return string.Join(' ', palabras);
    }
}
