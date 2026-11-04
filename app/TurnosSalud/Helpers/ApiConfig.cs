namespace TurnosSalud.Helpers;

/// <summary>
/// Dirección de la API TurnosSalud.Api. Es lo ÚNICO que cambia entre probar en la notebook
/// y usar la API publicada en el hosting.
/// </summary>
// =====================================================================
// ApiConfig: la dirección (URL) donde está la API.
//   - Probando en la notebook: UsarHosting = false → usa Local (la API con F5).
//   - Celular real o profesor desde otra PC: UsarHosting = true → usa Hosting.
// "const" = valor fijo que se decide al compilar. Para cambiarlo hay que
// volver a compilar la app.
// =====================================================================
public static class ApiConfig
{
    // 1) Local: la API corriendo con F5 en Visual Studio, en esta misma notebook.
#if ANDROID
    // El emulador de Android ve a la notebook con la dirección especial 10.0.2.2 (no "localhost").
    public const string Local = "http://10.0.2.2:5161/";
#else
    public const string Local = "http://localhost:5161/";
#endif

    // 2) Hosting (MonsterASP.NET): pegar acá la dirección pública del sitio, con la barra final.
    public const string Hosting = "https://PEGAR-SITIO.runasp.net/";

    // Cambiar a true para usar la API publicada (por ejemplo, para instalar el APK en un celular).
    public const bool UsarHosting = false;

    // La propiedad que usa ApiClient: elige una de las dos direcciones.
    public static string Url => UsarHosting ? Hosting : Local;
}
