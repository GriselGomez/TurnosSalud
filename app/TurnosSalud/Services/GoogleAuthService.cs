using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TurnosSalud.Services;

/// <summary>Datos básicos de la cuenta de Google que devuelve el inicio de sesión.</summary>
// Datos que devuelve Google: identificador, correo, nombre y apellido.
public sealed record GoogleUsuario(string Id, string Email, string Nombre, string Apellido);

/// <summary>
/// "Continuar con Google" con OAuth 2.0 + PKCE (el flujo recomendado por Google para apps instaladas).
/// - Android: navegador seguro del sistema (WebAuthenticator) y vuelta a la app por esquema propio.
/// - Windows: navegador predeterminado y respuesta recibida en localhost (loopback).
/// La app nunca ve la contraseña de Google: solo recibe nombre, correo e identificador.
/// </summary>
// Ingreso con Google usando OAuth 2.0 (el protocolo estándar de "Iniciar sesión con...")
// y PKCE (una protección extra para apps instaladas en el celular).
public sealed class GoogleAuthService : IGoogleAuthService
{
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
    private const string Scopes = "openid email profile";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public bool Disponible =>
#if ANDROID
        GoogleAuthConfig.AndroidConfigurado;
#elif WINDOWS
        GoogleAuthConfig.DesktopConfigurado;
#else
        false;
#endif

    // Abre el navegador con la pantalla de Google y espera la respuesta.
    public async Task<GoogleUsuario?> IniciarAsync()
    {
        if (!Disponible)
            throw new InvalidOperationException("El ingreso con Google no está configurado en esta plataforma.");

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            throw new InvalidOperationException("Necesitás conexión a internet para ingresar con Google.");

        // PKCE: un secreto de un solo uso que solo conoce esta app.
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));

#if ANDROID
        var clientId = GoogleAuthConfig.AndroidClientId;
        string? clientSecret = null;
        var redirect = GoogleAuthConfig.AndroidRedirectUri;

        WebAuthenticatorResult resultado;
        try
        {
            resultado = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(ArmarUrl(clientId, redirect, challenge, state)),
                new Uri(redirect));
        }
        catch (TaskCanceledException)
        {
            return null; // el usuario cerró la ventana de Google
        }

        var parametros = resultado.Properties;
        parametros.TryGetValue("error", out var error);
        parametros.TryGetValue("state", out var estadoRecibido);
        parametros.TryGetValue("code", out var code);
#elif WINDOWS
        var clientId = GoogleAuthConfig.DesktopClientId;
        string? clientSecret = GoogleAuthConfig.DesktopClientSecret;
        var redirect = $"http://localhost:{PuertoLibre()}/";

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirect);
        listener.Start();

        await Launcher.Default.OpenAsync(new Uri(ArmarUrl(clientId, redirect, challenge, state)));

        HttpListenerContext contexto;
        try
        {
            contexto = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(3));
        }
        catch (TimeoutException)
        {
            return null; // el usuario no terminó el ingreso a tiempo
        }

        var error = contexto.Request.QueryString["error"];
        var estadoRecibido = contexto.Request.QueryString["state"];
        var code = contexto.Request.QueryString["code"];

        var html = Encoding.UTF8.GetBytes(
            "<html><body style='font-family:sans-serif;text-align:center;margin-top:80px'>" +
            "<h2>Listo</h2><p>Ya podés cerrar esta pestaña y volver a Turnos Salud.</p></body></html>");
        contexto.Response.ContentType = "text/html; charset=utf-8";
        contexto.Response.ContentLength64 = html.Length;
        await contexto.Response.OutputStream.WriteAsync(html);
        contexto.Response.Close();
#else
        throw new PlatformNotSupportedException();
#endif

#if ANDROID || WINDOWS
        if (error == "access_denied")
            return null; // el usuario rechazó el permiso

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException($"Google respondió: {error}.");

        // Protección contra respuestas falsificadas (CSRF).
        if (estadoRecibido != state || string.IsNullOrEmpty(code))
            throw new InvalidOperationException("La respuesta de Google no es válida. Intentá nuevamente.");

        var accessToken = await CanjearCodigoAsync(code, clientId, clientSecret, redirect, verifier);
        return await ObtenerUsuarioAsync(accessToken);
#endif
    }

    private static string ArmarUrl(string clientId, string redirect, string challenge, string state) =>
        $"{AuthEndpoint}?client_id={Uri.EscapeDataString(clientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirect)}" +
        "&response_type=code" +
        $"&scope={Uri.EscapeDataString(Scopes)}" +
        $"&code_challenge={challenge}&code_challenge_method=S256" +
        $"&state={state}" +
        "&prompt=select_account";

    private static async Task<string> CanjearCodigoAsync(string code, string clientId, string? clientSecret,
        string redirect, string verifier)
    {
        var datos = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["redirect_uri"] = redirect,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = verifier
        };
        if (!string.IsNullOrEmpty(clientSecret))
            datos["client_secret"] = clientSecret;

        using var respuesta = await Http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(datos));
        var json = await respuesta.Content.ReadAsStringAsync();
        if (!respuesta.IsSuccessStatusCode)
            throw new InvalidOperationException($"No se pudo completar el ingreso con Google ({(int)respuesta.StatusCode}).");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
               ?? throw new InvalidOperationException("Google no devolvió un token de acceso.");
    }

    private static async Task<GoogleUsuario> ObtenerUsuarioAsync(string accessToken)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var respuesta = await Http.SendAsync(pedido);
        respuesta.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        var raiz = doc.RootElement;

        string Texto(string nombre) =>
            raiz.TryGetProperty(nombre, out var valor) ? valor.GetString() ?? string.Empty : string.Empty;

        var verificado = raiz.TryGetProperty("email_verified", out var v) && v.ValueKind == JsonValueKind.True;
        if (!verificado)
            throw new InvalidOperationException("El correo de esa cuenta de Google no está verificado.");

        return new GoogleUsuario(Texto("sub"), Texto("email").ToLowerInvariant(), Texto("given_name"), Texto("family_name"));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static int PuertoLibre()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var puerto = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return puerto;
    }
}
