# Cómo leer el código de Turnos Salud (MVVM y enlace de datos)

Todos los archivos tienen comentarios en español. Esta guía explica cómo recorrerlos y cómo defender el proyecto.

## 1. La idea en una frase

**La vista dibuja, el ViewModel decide y los dos se conectan solos por enlace de datos (*data binding*).**

| Antes (código detrás, *code-behind*) | Ahora (MVVM) |
| --- | --- |
| `txtDni.Text` para leer lo escrito | `Text="{Binding Dni}"`: lo escrito llega solo a la propiedad `Dni` |
| `btnIngresar.Clicked += ...` | `Command="{Binding IngresarCommand}"` |
| `lblError.Text = "..."; lblError.IsVisible = true;` | El ViewModel hace `DniError = "..."` y la vista se actualiza sola |
| La lógica vive en el `.xaml.cs` | El `.xaml.cs` queda casi vacío: solo `BindingContext = viewModel;` |

## 2. Las tres carpetas de MVVM

| Letra | Carpeta | Qué hay | Ejemplo |
| --- | --- | --- | --- |
| **M**odel (modelo) | `Models/` | Clases de datos | `Medico`, `Turno`, `Usuario` |
| **V**iew (vista) | `Views/` | Pantallas `.xaml` | `LoginPage.xaml` |
| **V**iew**M**odel (modelo de la vista) | `ViewModels/` | Datos y comandos de cada pantalla | `LoginViewModel.cs` |

Cada pantalla tiene **tres archivos**: `LoginPage.xaml` (dibujo), `LoginPage.xaml.cs` (conexión) y `ViewModels/LoginViewModel.cs` (lógica).

## 3. Los tipos de enlace que van a encontrar

| En el `.xaml` | Qué significa | Dónde está en C# |
| --- | --- | --- |
| `Text="{Binding Saludo}"` | Muestra un dato (ida: ViewModel → pantalla) | `[ObservableProperty] private string saludo;` |
| `Text="{Binding Dni}"` en una caja de texto | Doble vía: lo que escribe el usuario vuelve al ViewModel | `[ObservableProperty] private string dni;` |
| `Command="{Binding IngresarCommand}"` | Al tocar, ejecuta un método | `[RelayCommand] private async Task IngresarAsync()` |
| `ItemsSource="{Binding Medicos}"` | Lista: una fila por elemento | `ObservableCollection<Medico> Medicos` |
| `x:DataType="models:Medico"` en un `DataTemplate` | Dentro de la plantilla, los `{Binding}` leen **cada fila** | `Models/Medico.cs` |
| `IsVisible="{Binding ErrorGeneral, Converter={StaticResource HasText}}"` | Convierte un texto en verdadero o falso | `Converters/Converters.cs` |
| `<DataTrigger Binding="{Binding IsSelected}" Value="True">` | Si el dato vale eso, cambia un color | `ViewModels/Items.cs` |
| `{StaticResource Primary}` | **No es enlace**: color o estilo fijo | `Resources/Styles/` |

**Regla de los nombres:**
- `[ObservableProperty] private string dni;` genera la propiedad `Dni` (con mayúscula).
- `[RelayCommand] IngresarAsync()` genera `IngresarCommand` (sin "Async" y con "Command" al final).

## 4. Recorrido completo para practicar: "Ingresar"

1. `Views/LoginPage.xaml.cs`: `BindingContext = viewModel;` conecta la pantalla con `LoginViewModel`.
2. El usuario escribe el DNI. Como `Text="{Binding Dni}"` es de doble vía, el valor se copia a `Dni` y se ejecuta `OnDniChanged`.
3. Toca **Ingresar**. `Command="{Binding IngresarCommand}"` ejecuta `IngresarAsync()`.
4. `IngresarAsync()` valida con `Helpers/Validador.cs` y llama a `auth.IniciarSesionAsync(...)`.
5. `auth` es `ApiAuthService` (lo decide `MauiProgram.cs`). Envía `POST api/Cuentas/Login` usando `ApiClient` (HttpClient).
6. En la API, `CuentasController.Login` busca el usuario en SQL Server con Entity Framework y verifica el hash de la contraseña.
7. La respuesta vuelve en JSON `{ mensaje, response }`. Según el estado:
   - **Exitoso:** navega a Inicio.
   - **Contraseña incorrecta:** hace `PasswordError = "..."` y el texto rojo aparece solo en pantalla.

Hagan el mismo ejercicio con **Sacar un turno**, que es el corazón de la app:

> `HomePage` → `MedicosPage` → `MedicoDetallePage` (días y horarios) → `ConfirmarTurnoPage` (`POST api/Turnos/Reservar`) → `TurnoConfirmadoPage`

## 5. Por qué está bien hecho (para defenderlo)

- **Separación de responsabilidades:** la vista no sabe de bases de datos. El ViewModel no sabe de botones. La API no sabe de pantallas.
- **Programar contra interfaces:** los ViewModels piden `IAuthService`, `ICatalogoService` e `IAgendaService`. Pasamos de SQLite a la API con SQL Server agregando las clases de `Services/Api/` y cambiando el registro en `MauiProgram.cs`, sin tocar ninguna pantalla ni ningún ViewModel.
- **Inyección de dependencias:** nadie hace `new` de los servicios. Se registran una vez en `MauiProgram.cs` (app) y en `Program.cs` (API).
- **Reglas en el servidor:** la doble reserva, el máximo de 5 turnos y el límite de 2 horas los controla la API. Además, la base tiene un índice único que hace imposible la doble reserva aunque falle todo lo demás.
- **Base normalizada (3FN):** 7 tablas. Los DTO y `Mapeo.cs` devuelven a la app los nombres ya resueltos.

## 6. Pregunta típica del profesor: "¿qué pasa si cambia `IsBusy`?"

`IsBusy` tiene `[ObservableProperty]`. Al asignarle un valor, el código generado dispara `PropertyChanged("IsBusy")`. MAUI ve que la ruedita tiene `IsRunning="{Binding IsBusy}"` y la actualiza. Además, `[NotifyPropertyChangedFor(nameof(IsNotBusy))]` avisa también que cambió `IsNotBusy`, y los botones con `IsEnabled="{Binding IsNotBusy}"` se desactivan. Nadie tocó un control por su nombre.
