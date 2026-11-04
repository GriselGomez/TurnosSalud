namespace TurnosSalud.Api.Servicios;

/// <summary>
/// Cuenta los intentos fallidos de ingreso por DNI y bloquea 1 minuto después de 5 fallos.
/// Se registra como Singleton: una sola instancia para toda la API.
/// </summary>
// Cuenta los intentos fallidos de login por DNI: al quinto, bloquea un minuto.
// Vive en memoria (Singleton): si la API se reinicia, los contadores vuelven a cero.
public sealed class IntentosLogin
{
    private const int MaxIntentos = 5;
    private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(1);

    private readonly Dictionary<string, (int Fallos, DateTime? BloqueadoHasta)> _intentos = new();
    private readonly object _lock = new();

    /// <summary>Segundos que faltan de bloqueo, o 0 si no está bloqueado.</summary>
    public int SegundosBloqueo(string dni)
    {
        lock (_lock)
        {
            if (_intentos.TryGetValue(dni, out var info) && info.BloqueadoHasta is { } hasta && hasta > DateTime.UtcNow)
                return (int)Math.Ceiling((hasta - DateTime.UtcNow).TotalSeconds);
            return 0;
        }
    }

    /// <summary>Registra un fallo. Devuelve los intentos que quedan (0 = quedó bloqueado).</summary>
    public int RegistrarFallo(string dni)
    {
        lock (_lock)
        {
            var fallos = (_intentos.TryGetValue(dni, out var previo) ? previo.Fallos : 0) + 1;
            if (fallos >= MaxIntentos)
            {
                _intentos[dni] = (0, DateTime.UtcNow.Add(DuracionBloqueo));
                return 0;
            }
            _intentos[dni] = (fallos, null);
            return MaxIntentos - fallos;
        }
    }

    public void Limpiar(string dni)
    {
        lock (_lock) _intentos.Remove(dni);
    }

    public static int SegundosDeBloqueoTotal => (int)DuracionBloqueo.TotalSeconds;
}
