using TurnosSalud.Data;
using TurnosSalud.Helpers;
using TurnosSalud.Models;

namespace TurnosSalud.Services;

/// <summary>Consulta de especialidades y médicos. Implementación con SQLite local de <see cref="ICatalogoService"/>.</summary>
// VERSIÓN ANTERIOR (SQLite). Hoy se usa ApiCatalogoService. Queda como plan B sin internet.
public sealed class CatalogoService(DatabaseService database) : ICatalogoService
{
    public async Task<List<Especialidad>> GetEspecialidadesAsync()
    {
        var db = await database.GetConexionAsync();
        return await db.Table<Especialidad>().OrderBy(e => e.Orden).ToListAsync();
    }

    public async Task<Especialidad?> GetEspecialidadAsync(int id)
    {
        var db = await database.GetConexionAsync();
        return await db.FindAsync<Especialidad>(id);
    }

    public async Task<List<Medico>> GetMedicosPorEspecialidadAsync(int especialidadId)
    {
        var db = await database.GetConexionAsync();
        var especialidad = await db.FindAsync<Especialidad>(especialidadId);
        var medicos = await db.Table<Medico>().Where(m => m.EspecialidadId == especialidadId).ToListAsync();

        foreach (var m in medicos)
            m.EspecialidadNombre = especialidad?.Nombre ?? string.Empty;

        return medicos.OrderByDescending(m => m.Calificacion).ThenBy(m => m.Apellido).ToList();
    }

    /// <summary>Busca por nombre de médico o por especialidad, sin importar tildes ni mayúsculas.</summary>
    public async Task<List<Medico>> BuscarMedicosAsync(string texto)
    {
        var termino = Formatos.Normalizar(texto);
        var especialidades = (await GetEspecialidadesAsync()).ToDictionary(e => e.Id, e => e.Nombre);

        var db = await database.GetConexionAsync();
        var todos = await db.Table<Medico>().ToListAsync();

        foreach (var m in todos)
            m.EspecialidadNombre = especialidades.GetValueOrDefault(m.EspecialidadId, string.Empty);

        if (termino.Length == 0)
            return todos.OrderBy(m => m.Apellido).ToList();

        return todos
            .Where(m => Formatos.Normalizar($"{m.Nombre} {m.Apellido}").Contains(termino)
                     || Formatos.Normalizar(m.EspecialidadNombre).Contains(termino)
                     || Formatos.Normalizar(m.Sede).Contains(termino))
            .OrderByDescending(m => m.Calificacion)
            .ToList();
    }

    public async Task<Medico?> GetMedicoAsync(int id)
    {
        var db = await database.GetConexionAsync();
        var medico = await db.FindAsync<Medico>(id);
        if (medico is null) return null;

        var especialidad = await db.FindAsync<Especialidad>(medico.EspecialidadId);
        medico.EspecialidadNombre = especialidad?.Nombre ?? string.Empty;
        return medico;
    }
}
