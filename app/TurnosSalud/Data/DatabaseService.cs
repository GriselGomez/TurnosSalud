using SQLite;
using TurnosSalud.Models;

namespace TurnosSalud.Data;

/// <summary>
/// Acceso a la base de datos SQLite local. La conexión se crea una sola vez (lazy),
/// se crean las tablas y se cargan los datos iniciales la primera vez.
/// </summary>
// VERSIÓN ANTERIOR: conexión a la base SQLite local (plan B sin internet).
// Con la API, los datos están en SQL Server.
public sealed class DatabaseService
{
    private const string NombreArchivo = "turnossalud.db3";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _conexion;

    public static string RutaBaseDeDatos => Path.Combine(FileSystem.AppDataDirectory, NombreArchivo);

    public async Task<SQLiteAsyncConnection> GetConexionAsync()
    {
        if (_conexion is not null)
            return _conexion;

        await _initLock.WaitAsync();
        try
        {
            if (_conexion is not null)
                return _conexion;

            var conexion = new SQLiteAsyncConnection(RutaBaseDeDatos, Flags);
            await conexion.CreateTablesAsync<Usuario, Especialidad, Medico, Turno>();
            await SeedData.CargarSiEstaVaciaAsync(conexion);

            _conexion = conexion;
            return _conexion;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
