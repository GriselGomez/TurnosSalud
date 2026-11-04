using Android.App;
using Android.Content;
using Android.Content.PM;

namespace TurnosSalud;

/// <summary>
/// Recibe la vuelta desde la pantalla de Google (com.formosa.turnossalud:/oauth2redirect)
/// y se la entrega al WebAuthenticator. El esquema debe coincidir con GoogleAuthConfig.AndroidScheme.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.formosa.turnossalud")]
public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
