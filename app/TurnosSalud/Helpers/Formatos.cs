using System.Globalization;
using System.Text;

namespace TurnosSalud.Helpers;

/// <summary>
/// Formatos de fecha en español rioplatense, independientes de la configuración
/// regional del dispositivo (evita "mar." o meses en inglés según la plataforma).
/// </summary>
// Lo usan los modelos y los Items para armar los textos que se enlazan en pantalla
// (por ejemplo DiaItem.DiaSemana = Formatos.DiaCorto(Fecha) → "Lun").
public static class Formatos
{
    private static readonly string[] DiasLargos = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    private static readonly string[] DiasCortos = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];
    private static readonly string[] Meses = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];
    private static readonly string[] MesesCortos = ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

    /// <summary>Ej: "Mar, 7 de Octubre de 2025".</summary>
    public static string FechaLarga(DateTime fecha) =>
        $"{DiasCortos[(int)fecha.DayOfWeek]}, {fecha.Day} de {Meses[fecha.Month - 1]} de {fecha.Year}";

    /// <summary>Ej: "Martes 7 de Octubre".</summary>
    public static string FechaMedia(DateTime fecha) =>
        $"{DiasLargos[(int)fecha.DayOfWeek]} {fecha.Day} de {Meses[fecha.Month - 1]}";

    public static string DiaCorto(DateTime fecha) => DiasCortos[(int)fecha.DayOfWeek];

    public static string MesCorto(DateTime fecha) => MesesCortos[fecha.Month - 1];

    /// <summary>Ej: "Lun a Vie", "Lun, Mié y Vie".</summary>
    public static string DiasTexto(IReadOnlyList<DayOfWeek> dias)
    {
        if (dias.Count == 0) return "Sin días asignados";
        var ordenados = dias.OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d).ToList();

        bool consecutivos = true;
        for (int i = 1; i < ordenados.Count; i++)
        {
            if ((int)ordenados[i] != (int)ordenados[i - 1] + 1) { consecutivos = false; break; }
        }

        if (consecutivos && ordenados.Count >= 3)
            return $"{DiasCortos[(int)ordenados[0]]} a {DiasCortos[(int)ordenados[^1]]}";

        var nombres = ordenados.Select(d => DiasCortos[(int)d]).ToList();
        return nombres.Count == 1
            ? nombres[0]
            : string.Join(", ", nombres.Take(nombres.Count - 1)) + " y " + nombres[^1];
    }

    /// <summary>Quita tildes y pasa a minúsculas para comparar búsquedas ("pediatria" = "Pediatría").</summary>
    public static string Normalizar(string texto)
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

    /// <summary>Pone en mayúscula la primera letra de cada palabra ("maría josé" → "María José").</summary>
    public static string Capitalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var palabras = texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => char.ToUpper(p[0], CultureInfo.CurrentCulture) + p[1..].ToLower(CultureInfo.CurrentCulture));
        return string.Join(' ', palabras);
    }
}
