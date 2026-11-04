using TurnosSalud.Models;

namespace TurnosSalud.Services;

/// <summary>
/// Mantiene el usuario logueado en memoria y recuerda su Id entre aperturas de la app.
/// </summary>
// "Sesión" = quién está usando la app ahora. Se guarda el usuario en memoria
// (UsuarioActual) y su Id en el celular (Preferences), para que al volver a
// abrir la app no tenga que ingresar de nuevo. Se registra como Singleton
// en MauiProgram.cs: todas las pantallas ven el MISMO usuario.
public sealed class SessionService
{
    private const string ClaveUsuario = "sesion_usuario_id";

    public Usuario? UsuarioActual { get; private set; }

    public bool HaySesion => UsuarioActual is not null;

    public int UsuarioGuardadoId => Preferences.Default.Get(ClaveUsuario, 0);

    // Se llama al iniciar sesión o registrarse.
    public void Iniciar(Usuario usuario)
    {
        UsuarioActual = usuario;
        Preferences.Default.Set(ClaveUsuario, usuario.Id);
    }

    // Se llama al editar los datos personales.
    public void Actualizar(Usuario usuario) => UsuarioActual = usuario;

    // Se llama al cerrar sesión: borra el usuario de memoria y del celular.
    public void Cerrar()
    {
        UsuarioActual = null;
        Preferences.Default.Remove(ClaveUsuario);
    }
}
