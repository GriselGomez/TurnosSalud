using Microsoft.EntityFrameworkCore;
using TurnosSalud.Api.Models;
using TurnosSalud.Api.Servicios;

// =====================================================================
// Program.cs: el ARRANQUE de la API (ASP.NET Core Web API).
// Igual que MauiProgram.cs en la app: primero se REGISTRAN los servicios
// (builder.Services...) y después se arma el "camino" que recorre cada
// pedido HTTP (app.Use..., app.Map...).
// La API es un programa que queda escuchando pedidos: la app le pide
// datos por HTTP y la API responde en JSON leyendo y escribiendo SQL Server.
// =====================================================================
var builder = WebApplication.CreateBuilder(args);

// Conexión a la base TurnosSaludDB (la cadena está en appsettings.json)
// Entity Framework Core (EF Core): traduce código C# (LINQ) a consultas SQL.
// TurnosSaludContext representa la base; "CadenaSQL" dice a qué servidor conectarse.
builder.Services.AddDbContext<TurnosSaludContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSQL")));

// Reglas de negocio (las mismas que tenía la app con SQLite)
// AddScoped = una instancia por cada pedido HTTP. AddSingleton = una para toda la API
// (IntentosLogin necesita recordar los intentos de todos los pedidos).
builder.Services.AddScoped<AgendaService>();
builder.Services.AddSingleton<IntentosLogin>();   // bloqueo tras 5 intentos fallidos

// CORS: permite que la app (u otra página) use la API
var misReglasCors = "ReglasCors";
builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(name: misReglasCors, politica =>
    {
        politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// AddControllers: busca las clases de la carpeta Controllers.
// AddOpenApi: genera la descripción de la API que muestra Swagger.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Desde acá se arma el camino de cada pedido (el orden importa).
var app = builder.Build();

// Swagger queda disponible también en el hosting, para mostrar y probar la API en la presentación.
// Swagger UI: la página web para ver y probar cada endpoint (/swagger).
app.MapOpenApi();
app.UseSwaggerUI(opciones =>
    opciones.SwaggerEndpoint("/openapi/v1.json", "TurnosSalud.Api v1"));

app.UseCors(misReglasCors);

// No se redirige a HTTPS: así el emulador de Android puede usar http://10.0.2.2:5161 en desarrollo.
// En el hosting la dirección pública ya es https.

app.UseAuthorization();

// Conecta cada URL (por ejemplo api/Catalogo/Especialidades) con el método del controlador.
app.MapControllers();

// Página de inicio simple para comprobar que la API está viva.
app.MapGet("/", () => Results.Redirect("/swagger"));

// Arranca la API y queda escuchando pedidos.
app.Run();
