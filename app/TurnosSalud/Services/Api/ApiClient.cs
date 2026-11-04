using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TurnosSalud.Helpers;

namespace TurnosSalud.Services.Api;

/// <summary>Resultado de un pedido a la API: si salió bien, el código HTTP, el mensaje y los datos.</summary>
public sealed record ResultadoApi<T>(bool Exito, HttpStatusCode Codigo, string Mensaje, T? Datos);

/// <summary>
/// Hace los pedidos HTTP a la API con HttpClient y lee la respuesta { mensaje, response }
/// que devuelven todos los endpoints (el mismo formato del modelo de la cátedra).
/// </summary>
// CÓMO VIAJA UN PEDIDO: ViewModel → ApiAuthService (por ejemplo) → ApiClient
// → HTTP → API (Controllers) → Entity Framework → SQL Server, y la respuesta
// vuelve por el mismo camino convertida de JSON a objetos C#.
public sealed class ApiClient
{
    // JsonSerializerDefaults.Web: acepta nombres en minúscula del JSON ("nombre") para las propiedades C# ("Nombre").
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // HttpClient: la clase de .NET que hace pedidos HTTP. BaseAddress = dirección de ApiConfig.
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(ApiConfig.Url),
        Timeout = TimeSpan.FromSeconds(30)
    };

    // Atajos para los tres verbos HTTP que usa la app:
    // GET = consultar, POST = crear o enviar datos, PUT = modificar.
    public Task<ResultadoApi<T>> GetAsync<T>(string ruta) => EnviarAsync<T>(HttpMethod.Get, ruta, null);

    public Task<ResultadoApi<T>> PostAsync<T>(string ruta, object cuerpo) => EnviarAsync<T>(HttpMethod.Post, ruta, cuerpo);

    public Task<ResultadoApi<T>> PutAsync<T>(string ruta, object? cuerpo) => EnviarAsync<T>(HttpMethod.Put, ruta, cuerpo);

    // Método común: arma el pedido, lo envía, y convierte la respuesta JSON.
    private async Task<ResultadoApi<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo)
    {
        // Si hay datos para enviar, se convierten a JSON en el cuerpo del pedido.
        using var pedido = new HttpRequestMessage(metodo, ruta);
        if (cuerpo is not null)
            pedido.Content = JsonContent.Create(cuerpo, options: Json);

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await _http.SendAsync(pedido);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Sin internet, API apagada o dirección equivocada en ApiConfig.
            throw new InvalidOperationException(
                "No se pudo conectar con el servidor de Turnos Salud. Revisá tu conexión a internet e intentá de nuevo.", ex);
        }

        // Lee el JSON { mensaje, response } y lo devuelve como ResultadoApi.
        using (respuesta)
        {
            Envoltura<T>? cuerpoRespuesta = null;
            try
            {
                cuerpoRespuesta = await respuesta.Content.ReadFromJsonAsync<Envoltura<T>>(Json);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                // La respuesta no tenía el formato { mensaje, response } (por ejemplo, un error 500 del servidor).
            }

            var mensaje = cuerpoRespuesta?.Mensaje
                          ?? (respuesta.IsSuccessStatusCode ? "ok" : "El servidor no pudo procesar el pedido. Intentá de nuevo.");
            return new ResultadoApi<T>(respuesta.IsSuccessStatusCode, respuesta.StatusCode, mensaje,
                cuerpoRespuesta is null ? default : cuerpoRespuesta.Response);
        }
    }

    // Forma del JSON que devuelven todos los endpoints de la API.
    private sealed class Envoltura<T>
    {
        public string? Mensaje { get; set; }
        public T? Response { get; set; }
    }
}
