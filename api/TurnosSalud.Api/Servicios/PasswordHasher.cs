using System.Security.Cryptography;

namespace TurnosSalud.Api.Servicios;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 + salt aleatorio (el mismo de la app).
/// La contraseña nunca se guarda en texto plano.
/// </summary>
// Misma clase que en la app: hash PBKDF2-SHA256 con 100.000 iteraciones y salt aleatorio.
// En la base (tabla USUARIO) se guardan PasswordHash y PasswordSalt, nunca la contraseña.
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iteraciones = 100_000;

    public static (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, KeySize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verificar(string password, string hashGuardado, string saltGuardado)
    {
        try
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
