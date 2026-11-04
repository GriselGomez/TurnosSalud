using Microsoft.Extensions.Logging;
using TurnosSalud.Data;
using TurnosSalud.Services;
using TurnosSalud.Services.Api;
using TurnosSalud.ViewModels;
using TurnosSalud.Views;

namespace TurnosSalud;

// =====================================================================
// MauiProgram: el PUNTO DE ARRANQUE de la app. Acá se configura todo antes
// de mostrar la primera pantalla.
//
// INYECCIÓN DE DEPENDENCIAS (DI, dependency injection): en vez de que cada
// clase haga "new" de lo que necesita, se REGISTRA acá qué clase entregar
// para cada pedido. Cuando MAUI crea, por ejemplo, LoginPage, ve que su
// constructor pide un LoginViewModel; para crearlo ve que pide IAuthService,
// y entrega ApiAuthService porque así está registrado abajo.
//
//   AddSingleton  = UNA sola instancia para toda la app (servicios, sesión).
//   AddTransient  = una instancia NUEVA cada vez (páginas y ViewModels:
//                   cada vez que se abre una pantalla arranca limpia).
// =====================================================================
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Crea el "constructor" de la app y le dice cuál es la clase App (App.xaml.cs).
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Ajuste visual de las cajas de texto (ver el método más abajo).
        ConfigurarControles();

        // ---------- Servicios (una instancia para toda la app) ----------
        builder.Services.AddSingleton<SessionService>();

        // ---------- Acceso a datos ----------
        // Implementación actual: API TurnosSalud.Api + SQL Server (la dirección está en Helpers/ApiConfig.cs).
        // Las pantallas no cambiaron: solo usan las interfaces de Services/Contratos.cs.
        // ApiClient: el único que habla HTTP con la API (Services/Api/ApiClient.cs).
        // Las 3 líneas siguientes: "cuando alguien pida la INTERFAZ, entregale la clase Api...".
        // Este es el cambio que conectó la app con SQL Server sin tocar pantallas.
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<IAuthService, ApiAuthService>();
        builder.Services.AddSingleton<ICatalogoService, ApiCatalogoService>();
        builder.Services.AddSingleton<IAgendaService, ApiAgendaService>();

        // Versión anterior con SQLite local (plan B sin internet): comentar las 4 líneas de arriba
        // y descomentar estas 4.
        // builder.Services.AddSingleton<DatabaseService>();
        // builder.Services.AddSingleton<IAuthService, AuthService>();
        // builder.Services.AddSingleton<ICatalogoService, CatalogoService>();
        // builder.Services.AddSingleton<IAgendaService, AgendaService>();
        // Ingreso con Google, ventanas de aviso y navegación (también por interfaz).
        builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
        builder.Services.AddSingleton<IngresoConGoogle>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // ---------- Páginas y ViewModels ----------
        // Cada pantalla con su ViewModel. Si se agrega una pantalla nueva, se registra acá.
        builder.Services.AddTransient<WelcomePage>().AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<LoginPage>().AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegistroPage>().AddTransient<RegistroViewModel>();
        builder.Services.AddTransient<HomePage>().AddTransient<HomeViewModel>();
        builder.Services.AddTransient<EspecialidadesPage>().AddTransient<EspecialidadesViewModel>();
        builder.Services.AddTransient<MedicosPage>().AddTransient<MedicosViewModel>();
        builder.Services.AddTransient<MedicoDetallePage>().AddTransient<MedicoDetalleViewModel>();
        builder.Services.AddTransient<ConfirmarTurnoPage>().AddTransient<ConfirmarTurnoViewModel>();
        builder.Services.AddTransient<TurnoConfirmadoPage>().AddTransient<TurnoConfirmadoViewModel>();
        builder.Services.AddTransient<MisTurnosPage>().AddTransient<MisTurnosViewModel>();
        builder.Services.AddTransient<PerfilPage>().AddTransient<PerfilViewModel>();
        builder.Services.AddTransient<DatosPersonalesPage>().AddTransient<DatosPersonalesViewModel>();

// En modo depuración (Debug) muestra los mensajes de log en la ventana Salida de Visual Studio.
#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Construye la app con todo lo registrado.
        return builder.Build();
    }

    /// <summary>
    /// Quita el subrayado/borde nativo de Entry y Picker: el diseño ya los envuelve en una caja redondeada.
    /// </summary>
    // #if ANDROID / IOS / WINDOWS: código que se compila solo para esa plataforma.
    // "Mapper" permite retocar el control nativo de cada sistema (quitar la línea de abajo del Entry).
    private static void ConfigurarControles()
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("SinBordeNativo", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Background = null;
#endif
        });

        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("SinBordeNativo", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
            handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            handler.PlatformView.Background = null;
#endif
        });
    }
}
