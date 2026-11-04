namespace TurnosSalud.Helpers;

/// <summary>Rutas de navegación de Shell (centralizadas para evitar errores de tipeo).</summary>
// Uso: nav.IrAAsync(Routes.Login).
// "//" al principio = ruta ABSOLUTA: reemplaza todo lo que había (por ejemplo, al iniciar sesión).
// Sin "//" = página APILADA encima de la actual (se puede volver con "atrás").
public static class Routes
{
    // Raíces absolutas
    public const string Welcome = "//welcome";
    public const string Home = "//main/home";
    public const string MisTurnos = "//main/turnos";
    public const string Perfil = "//main/perfil";

    // Páginas apiladas
    public const string Login = "login";
    public const string Registro = "registro";
    public const string Especialidades = "especialidades";
    public const string Medicos = "medicos";
    public const string MedicoDetalle = "medicoDetalle";
    public const string ConfirmarTurno = "confirmarTurno";
    public const string TurnoConfirmado = "turnoConfirmado";
    public const string DatosPersonales = "datosPersonales";
}
