namespace TurnosSalud.Helpers;

/// <summary>Obras sociales más usadas en Formosa (lista editable).</summary>
// Lista que se muestra en el Picker (ItemsSource="{Binding ObrasSocialesDisponibles}").
// En SQL Server están en la tabla OBRA_SOCIAL.
public static class ObrasSociales
{
    public const string Particular = "Particular (sin obra social)";

    public static readonly IReadOnlyList<string> Lista =
    [
        Particular,
        "IASEP",
        "PAMI",
        "OSDE",
        "Swiss Medical",
        "Galeno",
        "Sancor Salud",
        "OSECAC",
        "OSPRERA",
        "IOSFA",
        "Medifé",
        "Otra"
    ];
}
