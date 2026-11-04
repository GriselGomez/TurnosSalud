using System.Security.Cryptography;

namespace TurnosSalud.Helpers;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 + salt aleatorio.
/// La contraseña nunca se almacena en texto plano.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    // HASH = "huella" de la contraseña que no se puede revertir. SALT = valor al azar
    // que se mezcla para que dos contraseñas iguales den hashes distintos.
    // Con la API, el hash lo calcula el servidor (TurnosSalud.Api/Servicios/PasswordHasher.cs).
    private const int KeySize = 32;
    private const int Iteraciones = 100_000;

    public static (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        // Genera hash y salt para una contraseña nueva.
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, KeySize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verificar(string password, string hashGuardado, string saltGuardado)
    {
        try
        // Verifica: vuelve a calcular el hash con el salt guardado y compara.
        {
            var salt = Convert.FromBase64String(saltGuardado);
            var esperado = Convert.FromBase64String(hashGuardado);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, KeySize);
            // Comparación en tiempo constante para evitar ataques de temporización.
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
