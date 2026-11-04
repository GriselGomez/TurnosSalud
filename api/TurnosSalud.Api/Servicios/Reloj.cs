namespace TurnosSalud.Api.Servicios;

/// <summary>
/// Hora de Argentina. El servidor del hosting puede estar en otro país (o en hora UTC),
/// y las reglas de turnos (30 minutos, 2 horas) se cuentan con la hora de Formosa.
/// </summary>
// La hora de Argentina, aunque el servidor del hosting esté en otro país.
// Todas las reglas de horario usan Reloj.Ahora y no DateTime.Now.
public static class Reloj
{
    private static readonly TimeZoneInfo ZonaArgentina = BuscarZona();

    public static DateTime Ahora => TimeZoneInfo.ConvertTime(DateTime.UtcNow, ZonaArgentina);

    private static TimeZoneInfo BuscarZona()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Argentina no usa horario de verano: UTC-3 fijo como respaldo.
        return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "Argentina", "Argentina");
    }
}
