using System.Globalization;
using TurnosSalud.Data;
using TurnosSalud.Models;

namespace TurnosSalud.Services;

// Tipos compartidos: Horario (un horario con su disponibilidad) y
// ResultadoOperacion (si una reserva o cancelación salió bien y por qué no).
public sealed record Horario(DateTime Inicio, bool Disponible);

public sealed record ResultadoOperacion(bool Exito, string? Error = null, Turno? Turno = null)
{
    public static ResultadoOperacion Ok(Turno? turno = null) => new(true, null, turno);
    public static ResultadoOperacion Falla(string error) => new(false, error);
}

/// <summary>
/// Agenda de turnos: días y horarios disponibles, reservas, reprogramaciones y cancelaciones.
/// Aplica las reglas de negocio para evitar superposiciones y turnos inválidos.
/// Implementación con SQLite local de <see cref="IAgendaService"/>.
/// Si en el futuro el servidor valida los turnos, estas reglas pueden quedar allí.
/// </summary>
// =====================================================================
// VERSIÓN ANTERIOR (SQLite). Hoy se usa ApiAgendaService y estas reglas
// las aplica la API (TurnosSalud.Api/Servicios/AgendaService.cs).
// Las constantes de esta clase (DiasAMostrar, AnticipacionMinima...) se
// siguen usando desde ApiAgendaService.
// =====================================================================
public sealed class AgendaService(DatabaseService database, ICatalogoService catalogo) : IAgendaService
{
    /// <summary>Anticipación mínima para sacar un turno.</summary>
    public static readonly TimeSpan AnticipacionMinima = TimeSpan.FromMinutes(30);

    /// <summary>Hasta cuándo se puede cancelar o reprogramar.</summary>
    public static readonly TimeSpan LimiteModificacion = TimeSpan.FromHours(2);

    public const int MaxTurnosActivos = 5;
    public const int DiasAMostrar = 10;

    // Evita reservas simultáneas del mismo horario.
    private static readonly SemaphoreSlim ReservaLock = new(1, 1);

    /// <summary>Próximos días en los que atiende el médico (con al menos un horario a futuro).</summary>
    public IReadOnlyList<DateTime> GetDiasDeAtencion(Medico medico, int cantidad = DiasAMostrar)
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
                var ultimo = dia.Add(ParseHora(medico.HoraUltimoTurno));
                if (ultimo < DateTime.Now.Add(AnticipacionMinima)) continue;
            }

            dias.Add(dia);
        }

        return dias;
    }

    public async Task<List<Horario>> GetHorariosAsync(Medico medico, DateTime dia, int usuarioId, int? ignorarTurnoId = null)
    {
        var db = await database.GetConexionAsync();
        var desde = dia.Date;
        var hasta = desde.AddDays(1);

        var turnosDelDia = await db.Table<Turno>()
            .Where(t => t.Inicio >= desde && t.Inicio < hasta && t.Estado == EstadosTurno.Activo)
            .ToListAsync();

        if (ignorarTurnoId is int idIgnorado)
            turnosDelDia = turnosDelDia.Where(t => t.Id != idIgnorado).ToList();

        var ocupadosMedico = turnosDelDia.Where(t => t.MedicoId == medico.Id).Select(t => t.Inicio).ToHashSet();
        var ocupadosUsuario = turnosDelDia.Where(t => t.UsuarioId == usuarioId).Select(t => t.Inicio).ToHashSet();
        var limite = DateTime.Now.Add(AnticipacionMinima);

        var horarios = new List<Horario>();
        foreach (var inicio in GenerarHorarios(medico, desde))
        {
            bool disponible = inicio > limite
                              && !ocupadosMedico.Contains(inicio)
                              && !ocupadosUsuario.Contains(inicio)
                              && !OcupadoPorOtrosPacientes(medico.Id, inicio);
            horarios.Add(new Horario(inicio, disponible));
        }

        return horarios;
    }

    public async Task<ResultadoOperacion> ReservarAsync(int usuarioId, int medicoId, DateTime inicio, int? reprogramaTurnoId = null)
    {
        await ReservaLock.WaitAsync();
        try
        {
            var db = await database.GetConexionAsync();

            var medico = await catalogo.GetMedicoAsync(medicoId);
            if (medico is null)
                return ResultadoOperacion.Falla("El profesional seleccionado no existe.");

            if (inicio <= DateTime.Now.Add(AnticipacionMinima))
                return ResultadoOperacion.Falla("El horario elegido ya no está disponible. Elegí otro horario.");

            if (!medico.Dias.Contains(inicio.DayOfWeek) || !GenerarHorarios(medico, inicio.Date).Contains(inicio))
                return ResultadoOperacion.Falla("El profesional no atiende en ese día u horario.");

            Turno? anterior = null;
            if (reprogramaTurnoId is int idAnterior)
            {
                anterior = await db.FindAsync<Turno>(idAnterior);
                if (anterior is null || anterior.UsuarioId != usuarioId || anterior.Estado != EstadosTurno.Activo)
                    return ResultadoOperacion.Falla("El turno que querés reprogramar ya no está activo.");
                if (anterior.Inicio <= DateTime.Now.Add(LimiteModificacion))
                    return ResultadoOperacion.Falla("Los turnos solo se pueden reprogramar hasta 2 horas antes.");
            }

            var activosUsuario = await db.Table<Turno>()
                .Where(t => t.UsuarioId == usuarioId && t.Estado == EstadosTurno.Activo)
                .ToListAsync();
            activosUsuario = activosUsuario.Where(t => t.Id != anterior?.Id && t.Inicio > DateTime.Now).ToList();

            int idExcluido = anterior?.Id ?? 0;
            bool ocupado = OcupadoPorOtrosPacientes(medicoId, inicio)
                           || await db.Table<Turno>().CountAsync(t => t.MedicoId == medicoId && t.Inicio == inicio
                                                                     && t.Estado == EstadosTurno.Activo && t.Id != idExcluido) > 0;
            if (ocupado)
                return ResultadoOperacion.Falla("Ese horario acaba de ser reservado por otro paciente. Elegí otro.");

            if (activosUsuario.Any(t => t.Inicio == inicio))
                return ResultadoOperacion.Falla("Ya tenés otro turno reservado en ese mismo día y horario.");

            if (activosUsuario.Any(t => t.MedicoId == medicoId && t.Inicio.Date == inicio.Date))
                return ResultadoOperacion.Falla("Ya tenés un turno con este profesional ese mismo día.");

            if (activosUsuario.Count >= MaxTurnosActivos)
                return ResultadoOperacion.Falla($"Podés tener como máximo {MaxTurnosActivos} turnos activos. Cancelá alguno para sacar uno nuevo.");

            var nuevo = new Turno
            {
                UsuarioId = usuarioId,
                MedicoId = medicoId,
                Inicio = inicio,
                Estado = EstadosTurno.Activo,
                FechaCreacion = DateTime.Now
            };

            // Reprogramación: se marca el anterior y se crea el nuevo en una única transacción.
            await db.RunInTransactionAsync(conn =>
            {
                if (anterior is not null)
                {
                    anterior.Estado = EstadosTurno.Reprogramado;
                    anterior.FechaCancelacion = DateTime.Now;
                    conn.Update(anterior);
                }
                conn.Insert(nuevo);
            });

            return ResultadoOperacion.Ok(nuevo);
        }
        finally
        {
            ReservaLock.Release();
        }
    }

    public async Task<ResultadoOperacion> CancelarAsync(int turnoId, int usuarioId)
    {
        var db = await database.GetConexionAsync();
        var turno = await db.FindAsync<Turno>(turnoId);

        if (turno is null || turno.UsuarioId != usuarioId)
            return ResultadoOperacion.Falla("No encontramos el turno.");
        if (turno.Estado != EstadosTurno.Activo)
            return ResultadoOperacion.Falla("El turno ya no está activo.");
        if (turno.Inicio <= DateTime.Now.Add(LimiteModificacion))
            return ResultadoOperacion.Falla("Los turnos solo se pueden cancelar hasta 2 horas antes. Comunicate con el centro de salud.");

        turno.Estado = EstadosTurno.Cancelado;
        turno.FechaCancelacion = DateTime.Now;
        await db.UpdateAsync(turno);
        return ResultadoOperacion.Ok(turno);
    }

    public async Task<List<TurnoDetalle>> GetTurnosUsuarioAsync(int usuarioId)
    {
        var db = await database.GetConexionAsync();
        var turnos = await db.Table<Turno>().Where(t => t.UsuarioId == usuarioId).ToListAsync();

        var resultado = new List<TurnoDetalle>(turnos.Count);
        var cacheMedicos = new Dictionary<int, Medico?>();

        foreach (var t in turnos)
        {
            if (!cacheMedicos.TryGetValue(t.MedicoId, out var medico))
            {
                medico = await catalogo.GetMedicoAsync(t.MedicoId);
                cacheMedicos[t.MedicoId] = medico;
            }

            if (medico is not null)
                resultado.Add(new TurnoDetalle { Turno = t, Medico = medico });
        }

        return resultado;
    }

    public async Task<TurnoDetalle?> GetProximoTurnoAsync(int usuarioId)
    {
        var turnos = await GetTurnosUsuarioAsync(usuarioId);
        return turnos.Where(t => t.EstaActivo && t.EsFuturo).OrderBy(t => t.Inicio).FirstOrDefault();
    }

    public async Task<TurnoDetalle?> GetTurnoDetalleAsync(int turnoId)
    {
        var db = await database.GetConexionAsync();
        var turno = await db.FindAsync<Turno>(turnoId);
        if (turno is null) return null;

        var medico = await catalogo.GetMedicoAsync(turno.MedicoId);
        return medico is null ? null : new TurnoDetalle { Turno = turno, Medico = medico };
    }

    // ---------------- Auxiliares ----------------

    private static IEnumerable<DateTime> GenerarHorarios(Medico medico, DateTime dia)
    {
        var desde = dia.Date.Add(ParseHora(medico.HoraInicio));
        var ultimo = dia.Date.Add(ParseHora(medico.HoraUltimoTurno));
        var paso = TimeSpan.FromMinutes(Math.Max(10, medico.DuracionTurnoMinutos));

        for (var h = desde; h <= ultimo; h = h.Add(paso))
            yield return h;
    }

    private static TimeSpan ParseHora(string hora) =>
        TimeSpan.ParseExact(hora, @"hh\:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Simula la demanda de otros pacientes (≈28% de horarios ocupados) para que la demo
    /// se vea realista. Es determinística: el mismo horario siempre da el mismo resultado.
    /// En producción se reemplaza por los turnos reales del servidor.
    /// </summary>
    private static bool OcupadoPorOtrosPacientes(int medicoId, DateTime inicio)
    {
        unchecked
        {
            uint x = (uint)(medicoId * 73856093);
            x ^= (uint)(inicio.Year * 10000 + inicio.Month * 100 + inicio.Day) * 19349663u;
            x ^= (uint)(inicio.Hour * 60 + inicio.Minute) * 83492791u;
            x ^= x >> 13;
            x *= 0x5bd1e995;
            x ^= x >> 15;
            return x % 100 < 28;
        }
    }
}
