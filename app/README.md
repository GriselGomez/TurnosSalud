# Turnos Salud · Formosa Capital

App móvil en **.NET MAUI (.NET 10)** para sacar, ver, reprogramar y cancelar turnos médicos.
**Versión final:** los datos se guardan en **SQL Server** a través de la API **TurnosSalud.Api** (ver `LEEME.md` en la carpeta principal).
La versión anterior con **SQLite local** queda como plan B (ver `MauiProgram.cs`).

## Cómo abrirla en Visual Studio 2026

1. Descomprimí el `.zip` y abrí **`TurnosSalud.sln`**.
2. Verificá que tengas la carga de trabajo **".NET Multi-platform App UI development"** (Visual Studio Installer › Modificar).
3. Esperá a que se restauren los paquetes NuGet (automático).
4. Elegí dónde ejecutar en la barra superior:
   - **Windows Machine** → se abre en una ventana con tamaño de teléfono (lo más rápido para probar).
   - **Android Emulator** (por ej. Pixel 7 · API 35) o tu celular por USB con *Depuración USB* activada.
5. Presioná **F5**.

> Si aparece un error de restauración de iOS en una PC sin Mac, no afecta a Android/Windows.
> Si querés quitarlo, borrá `;net10.0-ios` en la segunda línea `TargetFrameworks` del `.csproj`.

## Cuenta de demostración

| DNI (usuario) | Contraseña |
|---|---|
| `23002826` | `Turnos2026` |

La cuenta precargada es la de **Griselda Elizabeth Gómez** (griselgo73@hotmail.com, obra social IASEP).

En la pantalla de login hay un botón **"Completar automáticamente"**. También podés registrar cuentas nuevas.

## Flujo

Bienvenida → **Iniciar sesión con DNI + contraseña** (si el DNI no está registrado, ofrece **registrarse** con el DNI ya cargado; ahí se piden todos los datos, incluido el correo para avisos) → Inicio → Especialidad → Médicos → Agenda (días + horarios) → Confirmar turno → ¡Turno confirmado! → Mis turnos / Perfil.

## Validaciones y reglas implementadas

**Registro / login**
- Nombre y apellido: solo letras, 2–60 caracteres (se guardan capitalizados).
- DNI: 7 u 8 dígitos, sin puntos, **único**. Es el usuario para ingresar (teclado numérico, fácil para adultos mayores).
- Email con formato válido, **único**, normalizado en minúsculas. Se usa para avisos y se puede cambiar en Datos personales.
- Celular: 10 dígitos (código de área sin 0 + número sin 15, ej. 3704123456).
- Obra social obligatoria (lista con IASEP, PAMI, OSDE, etc.) y N° de afiliado opcional.
- Contraseña: mínimo 8 caracteres, letras y números, sin espacios; confirmación obligatoria.
- Aceptación de términos y privacidad (Ley 25.326).
- Contraseñas guardadas con **PBKDF2-SHA256 + salt** (nunca en texto plano), comparación en tiempo constante.
- **Bloqueo de 1 minuto** tras 5 intentos fallidos.
- Sesión recordada: al reabrir la app entra directo.
- Errores mostrados **debajo de cada campo**, y se re-validan mientras se escribe.

**Turnos**
- No se pueden sacar turnos en el pasado ni con menos de 30 minutos de anticipación.
- Un horario no puede ser tomado por dos pacientes.
- Un paciente no puede tener dos turnos a la misma hora, ni dos turnos con el mismo médico el mismo día.
- Máximo **5 turnos activos** por paciente.
- Cancelar o reprogramar solo hasta **2 horas antes**; la reprogramación es atómica (transacción).
- Para que la demo se vea real, ~28 % de los horarios aparecen ocupados por "otros pacientes" (simulación determinística en `AgendaService`).

## Estructura (MVVM)

```
TurnosSalud/
├── Models/        Usuario, Especialidad, Medico, Turno, TurnoDetalle
├── Data/          DatabaseService (SQLite), SeedData (datos iniciales)
├── Services/      Contratos (interfaces) + implementación SQLite: Auth, Agenda, Catálogo; Sesión, Diálogos, Navegación
├── ViewModels/    Un ViewModel por pantalla (CommunityToolkit.Mvvm)
├── Views/         Páginas XAML con bindings compilados (x:DataType)
├── Controls/      FormEntry (campo con ícono + error), AvatarView (iniciales)
├── Helpers/       Validador, PasswordHasher, Formatos, Rutas, Obras sociales
└── Resources/     Colores, estilos, íconos SVG, ícono de app y splash
```

Paquetes: `CommunityToolkit.Mvvm`, `sqlite-net-pcl` 1.11 (incluye SQLite actualizado).

## Personalizar

- **Médicos, sedes y horarios**: `Data/SeedData.cs` (los nombres son ficticios).
  Para recargar los datos, desinstalá la app del emulador/celular (borra la base local).
- **Colores**: `Resources/Styles/Colors.xaml`.
- **Reglas** (anticipación, límite de turnos, plazo de cancelación): constantes al inicio de `Services/AgendaService.cs`.
- Para pasar a producción, reemplazá `DatabaseService`/`AgendaService` por llamadas a una API del sistema de salud; las pantallas no cambian.

## Cambiar de base de datos más adelante

Las pantallas no conocen SQLite: solo usan las interfaces de **`Services/Contratos.cs`**
(`IAuthService`, `ICatalogoService`, `IAgendaService`).

| Pieza | Hoy (SQLite local) | Para migrar |
|---|---|---|
| Cuentas e ingreso con DNI | `Services/AuthService.cs` | Nueva clase que implemente `IAuthService` |
| Especialidades y médicos | `Services/CatalogoService.cs` | Nueva clase que implemente `ICatalogoService` |
| Turnos y agenda | `Services/AgendaService.cs` | Nueva clase que implemente `IAgendaService` |
| Conexión / datos iniciales | `Data/DatabaseService.cs`, `Data/SeedData.cs` | Ya no se usan (o quedan como caché offline) |

Después solo se cambian 3 líneas en **`MauiProgram.cs`**, por ejemplo:

```csharp
builder.Services.AddSingleton<IAuthService, ApiAuthService>();
builder.Services.AddSingleton<ICatalogoService, ApiCatalogoService>();
builder.Services.AddSingleton<IAgendaService, ApiAgendaService>();
```

> Recomendado para producción: que la app no se conecte directo a la base de datos,
> sino a una **API web** (por ejemplo ASP.NET Core) que sea la única que habla con SQL Server/MySQL/PostgreSQL.
> Así las credenciales de la base nunca viajan dentro de la app.

## Ingreso con Google ("Continuar con Google")

La pantalla de login tiene el botón **Continuar con Google** (OAuth 2.0 + PKCE).
La app nunca ve la contraseña de Gmail: Google solo le devuelve nombre, correo e identificador.

Dónde aparece:
- **Bienvenida:** "Continuar con Google" · "o con tu DNI" · "Ingresar con DNI" · "¿No tenés cuenta? Creala acá".
- **Iniciar sesión:** debajo del botón Ingresar.
- **Registro:** "Registrarse con Google" arriba del formulario.

Cómo se comporta:
- **Primera vez con esa cuenta:** aviso "Datos de Google vinculados" y el registro queda con nombre y correo cargados; solo se completa DNI, celular y obra social (sin contraseña).
- **Si el correo ya tenía cuenta:** desde Bienvenida o Login entra directo (la cuenta queda vinculada); desde Registro pregunta "Ya tenés una cuenta… ¿Querés ingresar directamente?".
- **Cuenta creada con Google:** en *Datos personales* puede crear una contraseña para entrar también con su DNI.
- **El ingreso con DNI sigue igual** para quien no tiene cuenta de Google.
- **Funciona en:** Android (emulador o celular) y Windows. Requiere internet.

### Configuración (una sola vez)

1. Entrar a https://console.cloud.google.com con tu Gmail y crear el proyecto **Turnos Salud**.
2. **APIs y servicios → Pantalla de consentimiento de OAuth:** tipo *Externo*, nombre de la app, tu correo de soporte. En *Usuarios de prueba* agregar los Gmail del grupo.
3. **Credenciales → Crear credenciales → ID de cliente de OAuth**, dos veces:
   - **Android:** nombre de paquete `com.formosa.turnossalud` y la **huella SHA-1** de tu PC (ver abajo).
     Después de crearlo, en **Configuración avanzada** activar **"Habilitar esquema de URI personalizado"**.
   - **App de escritorio:** solo un nombre (es para probar en Windows). Copiar el *ID de cliente* y el *Secreto*.
4. Pegar los valores en **`Services/GoogleAuthConfig.cs`** (reemplazar los textos `PEGAR_...`).
5. **Logo de Google:** descargar el logo oficial "G" desde las pautas de marca de Google
   (https://developers.google.com/identity/branding-guidelines), guardarlo como
   **`Resources/Images/ic_google.png`** (nombre en minúsculas) y volver a compilar.
   Sin ese archivo los botones funcionan igual, solo que sin el logo.

> **Trabajo en grupo:** todos pueden usar el **mismo proyecto de Google Cloud** (los mismos ID de cliente).
> El dueño del proyecto agrega los Gmail del grupo como *usuarios de prueba*.

**Obtener la huella SHA-1 (Windows, en "Símbolo del sistema"):**

```
keytool -list -v -keystore "%LOCALAPPDATA%\Xamarin\Mono for Android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
```

Copiar la línea que empieza con `SHA1:`. (Si `keytool` no se reconoce, usar la *Developer PowerShell* de Visual Studio.)

> Mientras la pantalla de consentimiento esté en modo *Prueba*, solo pueden ingresar los Gmail agregados como usuarios de prueba.
