using Microsoft.EntityFrameworkCore;
using TurnosSalud.Api.Dtos;
using TurnosSalud.Api.Models;

namespace TurnosSalud.Api.Servicios;

/// <summary>Resultado de reservar o cancelar: si salió bien, o el código HTTP y el mensaje para el paciente.</summary>
// Resultado de una operación de agenda: si salió bien, el código HTTP a devolver,
// el mensaje de error y el turno.
public sealed record ResultadoAgenda(bool Exito, int Codigo, string? Error, Turno? Turno)
{
    public static ResultadoAgenda Ok(Turno turno) => new(true, StatusCodes.Status200OK, null, turno);
    public static ResultadoAgenda Falla(int codigo, string error) => new(false, codigo, error, null);
}

/// <summary>
/// Reglas de negocio de la agenda, las mismas que tenía la app con SQLite (Services/AgendaService.cs),
/// ahora trabajando contra SQL Server. Así se aplican en un solo lugar para todos los pacientes.
/// </summary>
// =====================================================================
// REGLAS DE NEGOCIO de los turnos (las aplica el servidor, para todos):
//   - reservar con al menos 30 minutos de anticipación,
//   - solo en días y horarios en que atiende el médico,
//   - cancelar o reprogramar hasta 2 horas antes,
//   - sin doble reserva (mismo médico y horario) → 409,
//   - el paciente no puede tener dos turnos a la misma hora,
//   - ni dos turnos con el mismo médico el mismo día,
//   - máximo 5 turnos activos por paciente.
// =====================================================================
public sealed class AgendaService(TurnosSaludContext db)
{
    /// <summary>Anticipación mínima para sacar un turno.</summary>
    public static readonly TimeSpan AnticipacionMinima = TimeSpan.FromMinutes(30);

    /// <summary>Hasta cuándo se puede cancelar o reprogramar.</summary>
    public static readonly TimeSpan LimiteModificacion = TimeSpan.FromHours(2);

    public const int MaxTurnosActivos = 5;

    // Evita que dos reservas se procesen exactamente a la vez dentro de esta API.
    // Igual, el índice único de la base es la última barrera contra la doble reserva.
    private static readonly SemaphoreSlim ReservaLock = new(1, 1);

    // Busca un médico con sus días de atención.
    public Task<Medico?> BuscarMedicoAsync(int medicoId) =>
        db.Medicos
            .Include(m => m.IdEspecialidadNavigation)
            .Include(m => m.IdSedeNavigation)
            .Include(m => m.Dias)
            .FirstOrDefaultAsync(m => m.IdMedico == medicoId);

    /// <summary>Horarios de un día: todos los del médico, marcando cuáles están libres.</summary>
    // Arma los horarios del día (cada DuracionTurnoMinutos entre HoraInicio y HoraUltimoTurno)
    // y marca Disponible = false los ya ocupados o los que no cumplen las reglas.
    public async Task<List<HorarioDto>> GetHorariosAsync(Medico medico, DateTime dia, int usuarioId, int? ignorarTurnoId)
    {
        var desde = dia.Date;
        var hasta = desde.AddDays(1);

        var turnosDelDia = await db.Turnos
            .Where(t => t.Inicio >= desde && t.Inicio < hasta && t.Estado == EstadosTurno.Activo)
            .Where(t => t.IdMedico == medico.IdMedico || t.IdUsuario == usuarioId)
            .ToListAsync();

        if (ignorarTurnoId is int idIgnorado)
            turnosDelDia = turnosDelDia.Where(t => t.IdTurno != idIgnorado).ToList();

        var ocupadosMedico = turnosDelDia.Where(t => t.IdMedico == medico.IdMedico).Select(t => t.Inicio).ToHashSet();
        var ocupadosUsuario = turnosDelDia.Where(t => t.IdUsuario == usuarioId).Select(t => t.Inicio).ToHashSet();
        var limite = Reloj.Ahora.Add(AnticipacionMinima);
        var atiende = medico.Dias.Select(d => (DayOfWeek)d.DiaSemana).ToHashSet();

        var horarios = new List<HorarioDto>();
        if (!atiende.Contains(desde.DayOfWeek))
            return horarios;

        foreach (var inicio in GenerarHorarios(medico, desde))
        {
            bool disponible = inicio > limite
                              && !ocupadosMedico.Contains(inicio)
                              && !ocupadosUsuario.Contains(inicio);
            horarios.Add(new HorarioDto(inicio, disponible));
        }
        return horarios;
    }

    /// <summary>Reserva un turno nuevo o, si viene reprogramaTurnoId, reprograma uno existente.</summary>
    // Reserva (o reprograma) un turno revisando TODAS las reglas de arriba.
    public async Task<ResultadoAgenda> ReservarAsync(int usuarioId, int medicoId, DateTime inicio, int? reprogramaTurnoId)
    {
        await ReservaLock.WaitAsync();
        try
        {
            var ahora = Reloj.Ahora;

            if (!await db.Usuarios.AnyAsync(u => u.IdUsuario == usuarioId))
                return ResultadoAgenda.Falla(StatusCodes.Status404NotFound, "No encontramos tu cuenta. Volvé a iniciar sesión.");

            var medico = await BuscarMedicoAsync(medicoId);
            if (medico is null)
                return ResultadoAgenda.Falla(StatusCodes.Status404NotFound, "El profesional seleccionado no existe.");

            if (inicio <= ahora.Add(AnticipacionMinima))
                return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest, "El horario elegido ya no está disponible. Elegí otro horario.");

            var atiende = medico.Dias.Select(d => (DayOfWeek)d.DiaSemana).ToHashSet();
            if (!atiende.Contains(inicio.DayOfWeek) || !GenerarHorarios(medico, inicio.Date).Contains(inicio))
                return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest, "El profesional no atiende en ese día u horario.");

            Turno? anterior = null;
            if (reprogramaTurnoId is int idAnterior)
            {
                anterior = await db.Turnos.FirstOrDefaultAsync(t => t.IdTurno == idAnterior);
                if (anterior is null || anterior.IdUsuario != usuarioId || anterior.Estado != EstadosTurno.Activo)
                    return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest, "El turno que querés reprogramar ya no está activo.");
                if (anterior.Inicio <= ahora.Add(LimiteModificacion))
                    return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest, "Los turnos solo se pueden reprogramar hasta 2 horas antes.");
            }

            int idExcluido = anterior?.IdTurno ?? 0;

            var activosUsuario = await db.Turnos
                .Where(t => t.IdUsuario == usuarioId && t.Estado == EstadosTurno.Activo && t.Inicio > ahora && t.IdTurno != idExcluido)
                .ToListAsync();

            bool ocupado = await db.Turnos.AnyAsync(t => t.IdMedico == medicoId && t.Inicio == inicio
                                                         && t.Estado == EstadosTurno.Activo && t.IdTurno != idExcluido);
            if (ocupado)
                return ResultadoAgenda.Falla(StatusCodes.Status409Conflict, "Ese horario acaba de ser reservado por otro paciente. Elegí otro.");

            if (activosUsuario.Any(t => t.Inicio == inicio))
                return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest, "Ya tenés otro turno reservado en ese mismo día y horario.");

            if (activosUsuario.Any(t => t.IdMedico == medicoId && t.Inicio.Date == inicio.Date))
                return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest, "Ya tenés un turno con este profesional ese mismo día.");

            if (activosUsuario.Count >= MaxTurnosActivos)
                return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest,
                    $"Podés tener como máximo {MaxTurnosActivos} turnos activos. Cancelá alguno para sacar uno nuevo.");

            var nuevo = new Turno
            {
                IdUsuario = usuarioId,
                IdMedico = medicoId,
                Inicio = inicio,
                Estado = EstadosTurno.Activo,
                FechaCreacion = ahora
            };

            // Reprogramación: se marca el anterior y se crea el nuevo en UNA transacción (todo o nada).
            await using var transaccion = await db.Database.BeginTransactionAsync();
            try
            {
                if (anterior is not null)
                {
                    anterior.Estado = EstadosTurno.Reprogramado;
                    anterior.FechaCancelacion = ahora;
                }
                db.Turnos.Add(nuevo);
                await db.SaveChangesAsync();
                await transaccion.CommitAsync();
            }
            catch (DbUpdateException)
            {
                // El índice único UX_Turno_Medico_Horario rechazó la doble reserva.
                await transaccion.RollbackAsync();
                return ResultadoAgenda.Falla(StatusCodes.Status409Conflict, "Ese horario acaba de ser reservado por otro paciente. Elegí otro.");
            }

            return ResultadoAgenda.Ok(nuevo);
        }
        finally
        {
            ReservaLock.Release();
        }
    }

    // Cancela un turno del usuario si faltan más de 2 horas.
    public async Task<ResultadoAgenda> CancelarAsync(int turnoId, int usuarioId)
    {
        var turno = await db.Turnos.FirstOrDefaultAsync(t => t.IdTurno == turnoId);

        if (turno is null || turno.IdUsuario != usuarioId)
            return ResultadoAgenda.Falla(StatusCodes.Status404NotFound, "No encontramos el turno.");
        if (turno.Estado != EstadosTurno.Activo)
            return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest, "El turno ya no está activo.");
        if (turno.Inicio <= Reloj.Ahora.Add(LimiteModificacion))
            return ResultadoAgenda.Falla(StatusCodes.Status400BadRequest,
                "Los turnos solo se pueden cancelar hasta 2 horas antes. Comunicate con el centro de salud.");

        // El turno no se borra: cambia su estado y queda en el historial.
        turno.Estado = EstadosTurno.Cancelado;
        turno.FechaCancelacion = Reloj.Ahora;
        await db.SaveChangesAsync();
        return ResultadoAgenda.Ok(turno);
    }

    // ---------------- Auxiliares ----------------

    /// <summary>Horarios del día: desde HoraInicio hasta HoraUltimoTurno, cada DuracionTurnoMinutos.</summary>
    private static IEnumerable<DateTime> GenerarHorarios(Medico medico, DateTime dia)
    {
        var desde = dia.Date.Add(medico.HoraInicio);
        var ultimo = dia.Date.Add(medico.HoraUltimoTurno);
        var paso = TimeSpan.FromMinutes(Math.Max(10, medico.DuracionTurnoMinutos));

        for (var h = desde; h <= ultimo; h = h.Add(paso))
            yield return h;
    }
}
