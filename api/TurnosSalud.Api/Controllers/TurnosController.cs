using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosSalud.Api.Dtos;
using TurnosSalud.Api.Models;
using TurnosSalud.Api.Servicios;

namespace TurnosSalud.Api.Controllers;

/// <summary>Módulo Turnos: horarios, reserva, reprogramación, cancelación y consulta.</summary>
// =====================================================================
// Controlador de Turnos: horarios disponibles, reservar, cancelar y listar.
// URL base: api/Turnos.
// Las reglas de negocio NO están acá sino en Servicios/AgendaService.cs:
// el controlador solo recibe el pedido, llama al servicio y arma la respuesta.
// =====================================================================
[Route("api/[controller]")]
[ApiController]
public class TurnosController(TurnosSaludContext db, AgendaService agenda) : ControllerBase
{
    // GET api/Turnos/Horarios?medicoId=1&dia=2026-11-17&usuarioId=1&ignorarTurnoId=   (HU04)
    [HttpGet]
    [Route("Horarios")]
    public async Task<IActionResult> Horarios([FromQuery] int medicoId, [FromQuery] DateTime dia,
                                              [FromQuery] int usuarioId, [FromQuery] int? ignorarTurnoId)
    {
        var medico = await agenda.BuscarMedicoAsync(medicoId);
        if (medico is null)
            return NotFound(new Respuesta<object>("Profesional no encontrado", null));

        var horarios = await agenda.GetHorariosAsync(medico, dia.Date, usuarioId, ignorarTurnoId);
        return StatusCode(StatusCodes.Status200OK, new Respuesta<List<HorarioDto>>("ok", horarios));
    }

    // POST api/Turnos/Reservar   → reserva, o reprograma si trae ReprogramaTurnoId (HU05, HU08)
    [HttpPost]
    [Route("Reservar")]
    public async Task<IActionResult> Reservar([FromBody] ReservaPedido pedido)
    {
        // La hora llega sin zona horaria (hora de Formosa); se guarda tal cual.
        var inicio = DateTime.SpecifyKind(pedido.Inicio, DateTimeKind.Unspecified);
        var r = await agenda.ReservarAsync(pedido.UsuarioId, pedido.MedicoId, inicio, pedido.ReprogramaTurnoId);

        return r.Exito
            ? StatusCode(StatusCodes.Status200OK, new Respuesta<TurnoDto>("ok", Mapeo.ADto(r.Turno!)))
            : StatusCode(r.Codigo, new Respuesta<object>(r.Error!, null));
    }

    // PUT api/Turnos/Cancelar/15?usuarioId=1   → el turno no se borra, cambia a Cancelado (HU07)
    [HttpPut]
    [Route("Cancelar/{idTurno:int}")]
    public async Task<IActionResult> Cancelar(int idTurno, [FromQuery] int usuarioId)
    {
        var r = await agenda.CancelarAsync(idTurno, usuarioId);
        return r.Exito
            ? StatusCode(StatusCodes.Status200OK, new Respuesta<TurnoDto>("ok", Mapeo.ADto(r.Turno!)))
            : StatusCode(r.Codigo, new Respuesta<object>(r.Error!, null));
    }

    // GET api/Turnos/Usuario/1   → todos los turnos del paciente con su médico (HU06, HU11)
    [HttpGet]
    [Route("Usuario/{idUsuario:int}")]
    public async Task<IActionResult> TurnosDeUsuario(int idUsuario)
    {
        var turnos = await TurnosCompletos().Where(t => t.IdUsuario == idUsuario).ToListAsync();
        var resultado = turnos.Select(t => new TurnoDetalleDto
        {
            Turno = Mapeo.ADto(t),
            Medico = Mapeo.ADto(t.IdMedicoNavigation)
        }).ToList();
        return StatusCode(StatusCodes.Status200OK, new Respuesta<List<TurnoDetalleDto>>("ok", resultado));
    }

    // GET api/Turnos/Obtener/15   → detalle para el comprobante (HU05)
    [HttpGet]
    [Route("Obtener/{idTurno:int}")]
    public async Task<IActionResult> ObtenerTurno(int idTurno)
    {
        var t = await TurnosCompletos().FirstOrDefaultAsync(x => x.IdTurno == idTurno);
        if (t is null)
            return NotFound(new Respuesta<object>("Turno no encontrado", null));

        return StatusCode(StatusCodes.Status200OK, new Respuesta<TurnoDetalleDto>("ok",
            new TurnoDetalleDto { Turno = Mapeo.ADto(t), Medico = Mapeo.ADto(t.IdMedicoNavigation) }));
    }

    private IQueryable<Turno> TurnosCompletos() => db.Turnos
        .Include(t => t.IdMedicoNavigation).ThenInclude(m => m.IdEspecialidadNavigation)
        .Include(t => t.IdMedicoNavigation).ThenInclude(m => m.IdSedeNavigation)
        .Include(t => t.IdMedicoNavigation).ThenInclude(m => m.Dias)
        .AsNoTracking();
}
