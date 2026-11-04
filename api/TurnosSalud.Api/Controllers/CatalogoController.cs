using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosSalud.Api.Dtos;
using TurnosSalud.Api.Models;
using TurnosSalud.Api.Servicios;

namespace TurnosSalud.Api.Controllers;

/// <summary>Módulo Catálogo: especialidades, médicos y buscador.</summary>
// =====================================================================
// CONTROLADOR = la "puerta de entrada" de un grupo de endpoints.
//   [Route("api/[controller]")] → la URL empieza con api/Catalogo
//      ([controller] se reemplaza por el nombre de la clase sin "Controller").
//   [ApiController] → convierte solo el JSON recibido en objetos C#.
//   ControllerBase  → da los métodos para responder: Ok, NotFound, StatusCode...
//   (TurnosSaludContext db) → la base de datos llega por inyección de dependencias.
// Todas las respuestas tienen la forma { mensaje, response } (clase Respuesta en Dtos.cs),
// el mismo formato del ejemplo de la cátedra (WebApiProductos).
// =====================================================================
[Route("api/[controller]")]
[ApiController]
public class CatalogoController(TurnosSaludContext db) : ControllerBase
{
    // Médicos con su especialidad, su sede y sus días de atención (los tres Include).
    // Include = traer también los datos de las tablas relacionadas (como un JOIN en SQL).
    // AsNoTracking = solo lectura: EF Core no vigila cambios, así es más rápido.
    private IQueryable<Medico> MedicosCompletos() => db.Medicos
        .Include(m => m.IdEspecialidadNavigation)
        .Include(m => m.IdSedeNavigation)
        .Include(m => m.Dias)
        .AsNoTracking();

    // GET api/Catalogo/Especialidades   (HU03)
    // [HttpGet] = responde a pedidos GET. [Route("Especialidades")] completa la URL.
    // IActionResult = la respuesta HTTP (código + cuerpo JSON).
    [HttpGet]
    [Route("Especialidades")]
    public async Task<IActionResult> ListarEspecialidades()
    {
        var lista = await db.Especialidades.AsNoTracking().OrderBy(e => e.Orden).ToListAsync();
        // Mapeo.ADto convierte la entidad de la base en el DTO (la forma que espera la app).
        return StatusCode(StatusCodes.Status200OK, new Respuesta<List<EspecialidadDto>>("ok", lista.Select(x => Mapeo.ADto(x)).ToList()));
    }

    // GET api/Catalogo/Especialidad/1
    [HttpGet]
    // {idEspecialidad:int} = la parte de la URL que se recibe como parámetro (debe ser número).
    [Route("Especialidad/{idEspecialidad:int}")]
    public async Task<IActionResult> ObtenerEspecialidad(int idEspecialidad)
    {
        var e = await db.Especialidades.AsNoTracking().FirstOrDefaultAsync(x => x.IdEspecialidad == idEspecialidad);
        return e is null
            // 404 (NotFound) si no existe; 200 (OK) con los datos si existe.
            ? NotFound(new Respuesta<object>("Especialidad no encontrada", null))
            : StatusCode(StatusCodes.Status200OK, new Respuesta<EspecialidadDto>("ok", Mapeo.ADto(e)));
    }

    // GET api/Catalogo/Medicos?especialidadId=1   (HU03)
    [HttpGet]
    [Route("Medicos")]
    // [FromQuery] = el dato viene después del "?" en la URL (?especialidadId=1).
    public async Task<IActionResult> ListarMedicos([FromQuery] int especialidadId)
    {
        var lista = await MedicosCompletos().Where(m => m.IdEspecialidad == especialidadId).ToListAsync();
        var resultado = lista.OrderByDescending(m => m.Calificacion).ThenBy(m => m.Apellido).Select(x => Mapeo.ADto(x)).ToList();
        return StatusCode(StatusCodes.Status200OK, new Respuesta<List<MedicoDto>>("ok", resultado));
    }

    // GET api/Catalogo/Medico/5   (HU04)
    [HttpGet]
    [Route("Medico/{idMedico:int}")]
    public async Task<IActionResult> ObtenerMedico(int idMedico)
    {
        var m = await MedicosCompletos().FirstOrDefaultAsync(x => x.IdMedico == idMedico);
        return m is null
            ? NotFound(new Respuesta<object>("Profesional no encontrado", null))
            : StatusCode(StatusCodes.Status200OK, new Respuesta<MedicoDto>("ok", Mapeo.ADto(m)));
    }

    // GET api/Catalogo/Buscar?texto=pediatria   → sin importar tildes ni mayúsculas (HU09)
    [HttpGet]
    [Route("Buscar")]
    public async Task<IActionResult> BuscarMedicos([FromQuery] string? texto)
    {
        var termino = Texto.Normalizar(texto);
        // Son pocos médicos: se traen todos y se filtra en memoria para poder ignorar las tildes.
        var todos = (await MedicosCompletos().ToListAsync()).Select(x => Mapeo.ADto(x)).ToList();

        var resultado = termino.Length == 0
            ? todos.OrderBy(m => m.Apellido).ToList()
            : todos.Where(m => Texto.Normalizar($"{m.Nombre} {m.Apellido}").Contains(termino)
                            || Texto.Normalizar(m.EspecialidadNombre).Contains(termino)
                            || Texto.Normalizar(m.Sede).Contains(termino))
                   .OrderByDescending(m => m.Calificacion)
                   .ToList();

        return StatusCode(StatusCodes.Status200OK, new Respuesta<List<MedicoDto>>("ok", resultado));
    }
}
