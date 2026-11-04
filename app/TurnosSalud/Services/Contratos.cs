using TurnosSalud.Models;

namespace TurnosSalud.Services;

// =====================================================================================
//  CONTRATOS DE DATOS
//  Las pantallas (ViewModels) solo conocen estas interfaces, nunca la base de datos.
//
//  Implementación actual: API + SQL Server (Services/Api: ApiAuthService, ApiCatalogoService, ApiAgendaService).
//  Implementación anterior: SQLite local (AuthService, CatalogoService, AgendaService), queda como plan B.
//
//  Para cambiar de base de datos (SQL Server, MySQL, PostgreSQL, Firebase o una API web):
//    1. Creá clases nuevas que implementen estas interfaces
//       (por ejemplo ApiAuthService : IAuthService).
//    2. En MauiProgram.cs cambiá el registro, por ejemplo:
//         builder.Services.AddSingleton<IAuthService, ApiAuthService>();
//    Ninguna pantalla necesita cambios.
// =====================================================================================

// INTERFAZ = contrato: dice QUÉ se puede hacer, no CÓMO. Los ViewModels
// piden IAuthService; hoy lo cumple ApiAuthService (API + SQL Server) y antes
// lo cumplía AuthService (SQLite). Es el principio de "programar contra interfaces".
// Task = método asincrónico (async/await): la app no se congela mientras espera la respuesta.
/// <summary>Cuentas de pacientes: registro, ingreso con DNI y datos personales.</summary>
public interface IAuthService
{
    Task<Usuario?> BuscarPorDniAsync(string dni);
    Task<Usuario?> BuscarPorEmailAsync(string email);
    Task<bool> DniRegistradoAsync(string dni);
    Task<bool> EmailRegistradoAsync(string email);

    /// <summary>Ingreso con DNI + contraseña.</summary>
    Task<LoginResultado> IniciarSesionAsync(string dni, string password);
    Task<RegistroResultado> RegistrarAsync(NuevoUsuario datos);

    /// <summary>Ingreso con una cuenta de Google (ver <see cref="IGoogleAuthService"/>).</summary>
    Task<LoginResultado> IniciarSesionConGoogleAsync(GoogleUsuario google);

    /// <summary>Cuenta ya registrada con esa cuenta de Google o con su mismo correo.</summary>
    Task<Usuario?> BuscarCuentaGoogleAsync(GoogleUsuario google);

    /// <summary>Crea una contraseña sin pedir la anterior (para cuentas creadas con Google).</summary>
    Task EstablecerPasswordAsync(int usuarioId, string nueva);

    /// <summary>Recupera la sesión guardada al abrir la app.</summary>
    Task<bool> RestaurarSesionAsync();
    Task ActualizarDatosAsync(Usuario usuario);
    Task<bool> CambiarPasswordAsync(int usuarioId, string actual, string nueva);
    void CerrarSesion();
}

// Catálogo: datos de consulta (especialidades y médicos). Hoy: ApiCatalogoService.
/// <summary>Consulta de especialidades y profesionales.</summary>
public interface ICatalogoService
{
    Task<List<Especialidad>> GetEspecialidadesAsync();
    Task<Especialidad?> GetEspecialidadAsync(int id);
    Task<List<Medico>> GetMedicosPorEspecialidadAsync(int especialidadId);
    Task<List<Medico>> BuscarMedicosAsync(string texto);
    Task<Medico?> GetMedicoAsync(int id);
}

// Agenda: todo lo relacionado con turnos. Hoy: ApiAgendaService.
/// <summary>Agenda: horarios disponibles, reservas, reprogramaciones y cancelaciones.</summary>
public interface IAgendaService
{
    IReadOnlyList<DateTime> GetDiasDeAtencion(Medico medico, int cantidad = AgendaService.DiasAMostrar);
    Task<List<Horario>> GetHorariosAsync(Medico medico, DateTime dia, int usuarioId, int? ignorarTurnoId = null);
    Task<ResultadoOperacion> ReservarAsync(int usuarioId, int medicoId, DateTime inicio, int? reprogramaTurnoId = null);
    Task<ResultadoOperacion> CancelarAsync(int turnoId, int usuarioId);
    Task<List<TurnoDetalle>> GetTurnosUsuarioAsync(int usuarioId);
    Task<TurnoDetalle?> GetProximoTurnoAsync(int usuarioId);
    Task<TurnoDetalle?> GetTurnoDetalleAsync(int turnoId);
}

// Ingreso con Google: lo implementa GoogleAuthService.
/// <summary>Inicio de sesión con Google (OAuth 2.0 + PKCE).</summary>
public interface IGoogleAuthService
{
    /// <summary>True si la plataforma lo soporta y los Client ID están configurados.</summary>
    bool Disponible { get; }

    /// <summary>Abre la pantalla de Google. Devuelve null si el usuario cancela.</summary>
    Task<GoogleUsuario?> IniciarAsync();
}
