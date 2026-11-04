using TurnosSalud.Helpers;

namespace TurnosSalud.Models;

// TurnoDetalle junta el Turno y su Medico, y agrega textos ya formateados
// para las pantallas ({Binding Detalle.HoraTexto}, {Binding Turno.Codigo}...).
/// <summary>Turno con los datos del médico, listo para mostrar.</summary>
public class TurnoDetalle
{
    public required Turno Turno { get; init; }
    public required Medico Medico { get; init; }

    public int Id => Turno.Id;
    public DateTime Inicio => Turno.Inicio;

    public string Codigo => $"#{Turno.Id:D6}";
    public string FechaTexto => Formatos.FechaLarga(Turno.Inicio);
    public string HoraTexto => $"{Turno.Inicio:HH:mm} hs";
    public string DiaNumero => Turno.Inicio.Day.ToString();
    public string MesCorto => Formatos.MesCorto(Turno.Inicio);
    public string DiaSemanaCorto => Formatos.DiaCorto(Turno.Inicio);

    // Reglas de negocio usadas por la vista (IsVisible de los botones en Mis turnos).
    public bool EsFuturo => Turno.Inicio > DateTime.Now;
    public bool EstaActivo => Turno.Estado == EstadosTurno.Activo;

    /// <summary>Se puede cancelar o reprogramar hasta 2 horas antes.</summary>
    public bool PuedeModificar => EstaActivo && Turno.Inicio > DateTime.Now.AddHours(2);

    // Texto, color de letra y color de fondo de la etiqueta del estado.
    // Ejemplo de enlace que devuelve un Color: BackgroundColor="{Binding Detalle.EstadoFondo}".
    public string EstadoTexto => Turno.Estado switch
    {
        EstadosTurno.Activo when !EsFuturo => "Finalizado",
        EstadosTurno.Activo => "Confirmado",
        EstadosTurno.Reprogramado => "Reprogramado",
        _ => "Cancelado"
    };

    public Color EstadoColor => EstadoTexto switch
    {
        "Confirmado" => Color.FromArgb("#16A34A"),
        "Finalizado" => Color.FromArgb("#64748B"),
        "Reprogramado" => Color.FromArgb("#B45309"),
        _ => Color.FromArgb("#DC2626")
    };

    public Color EstadoFondo => EstadoTexto switch
    {
        "Confirmado" => Color.FromArgb("#E8F7EE"),
        "Finalizado" => Color.FromArgb("#F1F5F9"),
        "Reprogramado" => Color.FromArgb("#FFF7E6"),
        _ => Color.FromArgb("#FEF2F2")
    };
}
