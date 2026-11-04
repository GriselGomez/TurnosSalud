using TurnosSalud.Api.Dtos;
using TurnosSalud.Api.Models;

namespace TurnosSalud.Api.Servicios;

/// <summary>Convierte las entidades de la base en los DTO que espera la app.</summary>
// Convierte ENTIDADES (tablas de la base) en DTOs (lo que viaja en el JSON).
// Así la base puede estar normalizada (Id de sede, Id de obra social...) y la app
// sigue recibiendo los nombres ya resueltos (el nombre de la sede, de la especialidad o de la obra social).
public static class Mapeo
{
    public static UsuarioDto ADto(Usuario u) => new()
    {
        Id = u.IdUsuario,
        Nombre = u.Nombre,
        Apellido = u.Apellido,
        Dni = u.Dni,
        Email = u.Email,
        Telefono = u.Telefono,
        ObraSocial = u.IdObraSocialNavigation?.Nombre ?? "",
        NumeroAfiliado = u.NumeroAfiliado,
        GoogleId = u.GoogleId,
        FechaRegistro = u.FechaRegistro
    };

    public static EspecialidadDto ADto(Especialidad e) => new()
    {
        Id = e.IdEspecialidad,
        Nombre = e.Nombre,
        Icono = e.Icono ?? "",
        ColorFondo = e.ColorFondo ?? "#EAF2FF",
        ColorBorde = e.ColorBorde ?? "#C7DCFF",
        Orden = e.Orden,
        Destacada = e.Destacada
    };

    /// <summary>Requiere el médico cargado con su especialidad, su sede y sus días (Include).</summary>
    public static MedicoDto ADto(Medico m) => new()
    {
        Id = m.IdMedico,
        Titulo = m.Titulo,
        Nombre = m.Nombre,
        Apellido = m.Apellido,
        EspecialidadId = m.IdEspecialidad,
        EspecialidadNombre = m.IdEspecialidadNavigation?.Nombre ?? "",
        Matricula = m.Matricula,
        Sede = m.IdSedeNavigation?.Nombre ?? "",
        Calificacion = (double)m.Calificacion,
        CantidadResenas = m.CantidadResenas,
        // La tabla MEDICO_DIA (una fila por día) se arma como "1,2,3" para la app.
        DiasAtencion = string.Join(",", m.Dias.Select(d => d.DiaSemana).OrderBy(d => d)),
        HoraInicio = m.HoraInicio.ToString(@"hh\:mm"),
        HoraUltimoTurno = m.HoraUltimoTurno.ToString(@"hh\:mm"),
        DuracionTurnoMinutos = m.DuracionTurnoMinutos,
        ColorAvatar = m.ColorAvatar ?? "#0B5ED7"
    };

    public static TurnoDto ADto(Turno t) => new()
    {
        Id = t.IdTurno,
        UsuarioId = t.IdUsuario,
        MedicoId = t.IdMedico,
        Inicio = t.Inicio,
        Estado = t.Estado,
        FechaCreacion = t.FechaCreacion,
        FechaCancelacion = t.FechaCancelacion
    };
}
