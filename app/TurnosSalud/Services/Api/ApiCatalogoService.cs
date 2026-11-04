using System.Net;
using TurnosSalud.Models;

namespace TurnosSalud.Services.Api;

/// <summary>Especialidades y médicos traídos de la API (reemplaza a CatalogoService con SQLite).</summary>
public sealed class ApiCatalogoService(ApiClient api) : ICatalogoService
{
    // GET api/Catalogo/Especialidades → las 10 especialidades de la tabla ESPECIALIDAD.
    public async Task<List<Especialidad>> GetEspecialidadesAsync() =>
        Lista(await api.GetAsync<List<Especialidad>>("api/Catalogo/Especialidades"));

    // GET api/Catalogo/Especialidad/{id}
    public async Task<Especialidad?> GetEspecialidadAsync(int id) =>
        DatosONull(await api.GetAsync<Especialidad>($"api/Catalogo/Especialidad/{id}"));

    // GET api/Catalogo/Medicos?especialidadId=... → médicos de esa especialidad.
    public async Task<List<Medico>> GetMedicosPorEspecialidadAsync(int especialidadId) =>
        Lista(await api.GetAsync<List<Medico>>($"api/Catalogo/Medicos?especialidadId={especialidadId}"));

    // GET api/Catalogo/Buscar?texto=... → busca por nombre de médico o especialidad.
    public async Task<List<Medico>> BuscarMedicosAsync(string texto) =>
        Lista(await api.GetAsync<List<Medico>>($"api/Catalogo/Buscar?texto={Uri.EscapeDataString(texto)}"));

    // GET api/Catalogo/Medico/{id} → un médico con sus días de atención.
    public async Task<Medico?> GetMedicoAsync(int id) =>
        DatosONull(await api.GetAsync<Medico>($"api/Catalogo/Medico/{id}"));

    // Ayudantes: si la API respondió bien devuelven los datos; si no, lanzan el mensaje de error.
    private static List<T> Lista<T>(ResultadoApi<List<T>> r) =>
        r.Exito ? r.Datos ?? new List<T>() : throw new InvalidOperationException(r.Mensaje);

    private static T? DatosONull<T>(ResultadoApi<T> r) where T : class
    {
        if (r.Exito) return r.Datos;
        if (r.Codigo == HttpStatusCode.NotFound) return null;
        throw new InvalidOperationException(r.Mensaje);
    }
}
