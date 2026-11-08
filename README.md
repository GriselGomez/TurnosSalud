# Turnos Salud – app + API + base de datos SQL Server

Versión final: la app .NET MAUI ya no guarda los datos en el celular (SQLite), sino que los pide a la
API **TurnosSalud.Api**, y la API los guarda en **SQL Server** (base **TurnosSaludDB**, 7 tablas normalizadas).

```
turnos-salud/
├── app/   App .NET MAUI (TurnosSalud.sln)        → abrir con Visual Studio
├── api/   Web API ASP.NET Core (TurnosSalud.Api.sln)
└── db/    TurnosSaludDB.sql          → base local (SQL Server Express)
           TurnosSaludDB_hosting.sql  → base del hosting (MonsterASP.NET)
```

Cuenta de prueba: **DNI 11222333 · contraseña Turnos2026**

Cada pantalla, ViewModel, servicio y controlador tiene comentarios en español que explican su funcionamiento.

---

## Paso 1 · Crear la base de datos local (SSMS)

1. Abrir **SQL Server Management Studio** y conectarse a `localhost\SQLEXPRESS` (autenticación de Windows).
2. Archivo → Abrir → `db/TurnosSaludDB.sql` → **Ejecutar (F5)**.
3. Al final aparece una tabla de comprobación: OBRA_SOCIAL 12, ESPECIALIDAD 10, SEDE 4, MEDICO 26, MEDICO_DIA 83, USUARIO 1.

> Si tu servidor no se llama `localhost\SQLEXPRESS`, cambiá `Server=` en `api/TurnosSalud.Api/appsettings.json`
> (es el mismo nombre que usaste en el ejercicio de Productos).

## Paso 2 · Ejecutar la API y probarla en Swagger

1. Abrir `api/TurnosSalud.Api.sln` en Visual Studio y esperar que se restauren los paquetes.
2. Elegir el perfil **http** (flecha al lado del botón verde) y presionar **F5**. Se abre Swagger en `http://localhost:5161/swagger`.
3. Probar, en este orden:
   - `GET /api/Catalogo/Especialidades` → **Try it out** → **Execute**: devuelve las 10 especialidades.
   - `POST /api/Cuentas/Login` con `{ "dni": "11222333", "password": "Turnos2026" }`: devuelve `"estado": "Exitoso"` y los datos de la paciente.
   - `POST /api/Cuentas/Login` con una contraseña mal: devuelve **401** y `"PasswordIncorrecta"`.
4. **Dejar la API corriendo** mientras se usa la app.

## Paso 3 · Ejecutar la app contra la API local

1. Abrir `app/TurnosSalud.sln` en **otra ventana** de Visual Studio.
2. En `Helpers/ApiConfig.cs` dejar `UsarHosting = false`.
3. Ejecutar en **Windows Machine** o en el **emulador de Android** (el emulador usa `http://10.0.2.2:5161`, ya está configurado).
4. Ingresar con la cuenta de prueba, sacar un turno y comprobar en SSMS:
   `SELECT * FROM TURNO;` → el turno está guardado en SQL Server.
5. Prueba de la doble reserva: con otra cuenta (registrala desde la app) intentar el mismo médico, día y hora → la app avisa
   "Ese horario acaba de ser reservado por otro paciente".

> Un celular físico no ve `localhost` de la notebook: para el celular se usa la API publicada (paso 4).

## Paso 4 · Publicar la API y la base en MonsterASP.NET

1. Crear la cuenta en monsterasp.net y crear un **sitio web** (queda una dirección del tipo `https://NOMBRE.runasp.net`).
2. En el panel, crear una **base de datos MSSQL**. El panel muestra dos cadenas de conexión:
   - **local** (la usa la API publicada) y
   - **remota** (para conectarse desde SSMS; hay que habilitar el acceso remoto en el panel).
3. **Tablas y datos:** en SSMS, conectarse con los datos de la conexión remota (autenticación de SQL Server),
   elegir esa base y ejecutar `db/TurnosSaludDB_hosting.sql`.
4. **Cadena de conexión de la API:** copiar `appsettings.Production.ejemplo.json` como `appsettings.Production.json`
   (misma carpeta) y pegar ahí la cadena **local** del panel. Este archivo no se sube a GitHub (.gitignore).
5. **Publicar:** en el panel del sitio → **Deploy** → activar **WebDeploy** → **Download publish profile**.
   En Visual Studio: clic derecho en `TurnosSalud.Api` → **Publicar** → **Importar perfil** → elegir el archivo → **Publicar**.
6. Abrir `https://NOMBRE.runasp.net/swagger` y repetir las pruebas del paso 2.

## Paso 5 · App apuntando al hosting y APK

1. En `app/TurnosSalud/Helpers/ApiConfig.cs`: pegar la dirección en `Hosting` (con `/` al final) y poner `UsarHosting = true`.
2. Probar en Windows o en el emulador.
3. APK: elegir **Release** y **Android**, clic derecho en el proyecto → **Publicar** → **Ad hoc** → crear la firma → guardar el `.apk`.
   Pasarlo al celular e instalarlo (permitir "orígenes desconocidos").

## Problemas comunes

| Mensaje / síntoma | Causa | Solución |
| --- | --- | --- |
| "No se pudo conectar con el servidor de Turnos Salud" | La API no está corriendo o `ApiConfig` apunta a otra dirección | Ejecutar la API (paso 2) o revisar `UsarHosting` |
| La API falla al abrir Swagger con error de SQL | La base no existe o el nombre del servidor es otro | Paso 1 y revisar `appsettings.json` |
| En el hosting, error 500 | Falta `appsettings.Production.json` o la cadena es la remota | Usar la cadena **local** del panel y volver a publicar |
| La primera vez el hosting tarda | El plan gratuito "duerme" la API si no se usa | Abrir Swagger un minuto antes de la demo |
| Google no ingresa | Faltan credenciales en `Services/GoogleAuthConfig.cs` | Igual que antes; el ingreso con DNI siempre funciona |

## Qué cambió en la app 

- `Services/Api/` (nuevo): `ApiClient` (HttpClient), `ApiAuthService`, `ApiCatalogoService`, `ApiAgendaService`.
  Implementan las **mismas interfaces** de `Services/Contratos.cs`, por eso **ninguna pantalla cambió**.
- `MauiProgram.cs`: se registran los servicios `Api*` en lugar de los de SQLite (las líneas viejas quedan comentadas como plan B).
- `Helpers/ApiConfig.cs` (nuevo): la dirección de la API.
- `Platforms/Android/AndroidManifest.xml`: permite `http` para probar con la API local.
- Las reglas de turnos (30 minutos, máximo 5, 2 horas, sin doble reserva) ahora las aplica la API.
