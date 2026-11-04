using System.Globalization;
using System.Net;
using TurnosSalud.Models;

namespace TurnosSalud.Services.Api;

/// <summary>
/// Agenda de turnos contra la API (reemplaza a AgendaService con SQLite).
/// Las reglas (30 minutos, máximo 5, 2 horas, sin doble reserva) ahora las aplica la API,
/// así valen para todos los pacientes y no se pueden saltear desde un celular.
/// </summary>
public sealed class ApiAgendaService(ApiClient api) : IAgendaService
{
    /// <summary>
    /// Próximos días en que atiende el médico. Se calcula en la app con los días que manda la API
    /// (no hace falta pedirlo al servidor).
    /// </summary>
    public IReadOnlyList<DateTime> GetDiasDeAtencion(Medico medico, int cantidad = AgendaService.DiasAMostrar)
    {
        var dias = new List<DateTime>();
        var atiende = medico.Dias;
        var hoy = DateTime.Today;

        for (int i = 0; i < 45 && dias.Count < cantidad; i++)
        {
            var dia = hoy.AddDays(i);
            if (!atiende.Contains(dia.DayOfWeek)) continue;

            if (i == 0)
            {
                var ultimo = dia.Add(TimeSpan.ParseExact(medico.HoraUltimoTurno, @"hh\:mm", CultureInfo.InvariantCulture));
                if (ultimo < DateTime.Now.Add(AgendaService.AnticipacionMinima)) continue;
            }

            dias.Add(dia);
        }

        return dias;
    }

    // GET api/Turnos/Horarios?medicoId=..&dia=..&usuarioId=.. → cada horario con Disponible true/false.
    // ignorarTurnoId: al reprogramar, el turno viejo no cuenta como ocupado.
    public async Task<List<Horario>> GetHorariosAsync(Medico medico, DateTime dia, int usuarioId, int? ignorarTurnoId = null)
    {
        var ruta = $"api/Turnos/Horarios?medicoId={medico.Id}" +
                   $"&dia={dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
                   $"&usuarioId={usuarioId}" +
                   (ignorarTurnoId is int id ? $"&ignorarTurnoId={id}" : "");

        var r = await api.GetAsync<List<Horario>>(ruta);
        return r.Exito ? r.Datos ?? new() : throw new InvalidOperationException(r.Mensaje);
    }

    // POST api/Turnos/Reservar. La API revisa todas las reglas dentro de una transacción.
    // Si otro paciente reservó ese horario un instante antes, responde 409 (conflicto).
    public async Task<ResultadoOperacion> ReservarAsync(int usuarioId, int medicoId, DateTime inicio, int? reprogramaTurnoId = null)
    {
        ResultadoApi<Turno> r;
        try
        {
            r = await api.PostAsync<Turno>("api/Turnos/Reservar", new
            {
                usuarioId,
                medicoId,
                // La hora viaja sin zona horaria: es la hora de Formosa del horario elegido.
                inicio = DateTime.SpecifyKind(inicio, DateTimeKind.Unspecified),
                reprogramaTurnoId
            });
        }
        catch (InvalidOperationException ex)
        {
            return ResultadoOperacion.Falla(ex.Message);   // sin conexión con el servidor
        }

        return r.Exito && r.Datos is not null
            ? ResultadoOperacion.Ok(r.Datos)
            : ResultadoOperacion.Falla(r.Mensaje);
    }

    // PUT api/Turnos/Cancelar/{id}?usuarioId=... → el turno pasa a estado Cancelado.
    public async Task<ResultadoOperacion> CancelarAsync(int turnoId, int usuarioId)
    {
        ResultadoApi<Turno> r;
        try
        {
            r = await api.PutAsync<Turno>($"api/Turnos/Cancelar/{turnoId}?usuarioId={usuarioId}", null);
        }
        catch (InvalidOperationException ex)
        {
            return ResultadoOperacion.Falla(ex.Message);   // sin conexión con el servidor
        }
        return r.Exito ? ResultadoOperacion.Ok(r.Datos) : ResultadoOperacion.Falla(r.Mensaje);
    }

    // GET api/Turnos/Usuario/{id} → todos los turnos del paciente con su médico.
    public async Task<List<TurnoDetalle>> GetTurnosUsuarioAsync(int usuarioId)
    {
        var r = await api.GetAsync<List<TurnoDetalle>>($"api/Turnos/Usuario/{usuarioId}");
        return r.Exito ? r.Datos ?? new() : throw new InvalidOperationException(r.Mensaje);
    }

    // El próximo turno se calcula en la app a partir de la lista anterior.
    public async Task<TurnoDetalle?> GetProximoTurnoAsync(int usuarioId)
    {
        var turnos = await GetTurnosUsuarioAsync(usuarioId);
        return turnos.Where(t => t.EstaActivo && t.EsFuturo).OrderBy(t => t.Inicio).FirstOrDefault();
    }

    // GET api/Turnos/Obtener/{id} → un turno con su médico (para el comprobante).
    public async Task<TurnoDetalle?> GetTurnoDetalleAsync(int turnoId)
    {
        var r = await api.GetAsync<TurnoDetalle>($"api/Turnos/Obtener/{turnoId}");
        if (r.Exito) return r.Datos;
        if (r.Codigo == HttpStatusCode.NotFound) return null;
        throw new InvalidOperationException(r.Mensaje);
    }
}
