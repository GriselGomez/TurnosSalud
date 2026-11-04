using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TurnosSalud.Helpers;
using TurnosSalud.Models;

namespace TurnosSalud.ViewModels;

// =====================================================================
// "Items": pequeños ViewModels para CADA ELEMENTO de una lista.
// Cuando una lista tiene botones o selección propia, cada fila necesita
// su propio comando y su propio estado. En la vista, el DataTemplate usa
// x:DataType="vm:DiaItem" (por ejemplo) y sus {Binding} leen ESTE objeto.
// =====================================================================
/// <summary>Tarjeta de acceso rápido de la pantalla de inicio.</summary>
public sealed class AccesoItem(string nombre, string icono, Color fondo, Color borde, Func<Task> accion)
{
    public string Nombre { get; } = nombre;
    public string Icono { get; } = icono;
    public Color Fondo { get; } = fondo;
    public Color Borde { get; } = borde;
    // TocarCommand → TapGestureRecognizer de cada tarjeta de Inicio.
    // AsyncRelayCommand envuelve la acción recibida en un comando enlazable.
    public IAsyncRelayCommand TocarCommand { get; } = new AsyncRelayCommand(accion);
}

// Hereda de ObservableObject porque IsSelected cambia y la vista debe enterarse
// (DataTrigger que pinta el día de azul).
/// <summary>Día seleccionable en la agenda del médico.</summary>
public sealed partial class DiaItem : ObservableObject
{
    public DiaItem(DateTime fecha, Action<DiaItem> alSeleccionar)
    {
        Fecha = fecha;
        SeleccionarCommand = new RelayCommand(() => alSeleccionar(this));
    }

    public DateTime Fecha { get; }
    // Textos que muestra la tarjeta del día: "Lun", "17", "Nov".
    public string DiaSemana => Formatos.DiaCorto(Fecha);
    public string Numero => Fecha.Day.ToString();
    public string Mes => Formatos.MesCorto(Fecha);
    public string Descripcion => Formatos.FechaMedia(Fecha);
    // SeleccionarCommand → TapGestureRecognizer de cada día. Llama de vuelta al
    // MedicoDetalleViewModel (alSeleccionar) para que cargue los horarios.
    public IRelayCommand SeleccionarCommand { get; }

    // IsSelected → DataTrigger Binding="{Binding IsSelected}" Value="True" → fondo azul.
    [ObservableProperty]
    private bool isSelected;
}

/// <summary>Horario (slot) seleccionable.</summary>
public sealed partial class HorarioItem : ObservableObject
{
    public HorarioItem(DateTime inicio, bool disponible, Action<HorarioItem> alSeleccionar)
    {
        Inicio = inicio;
        Disponible = disponible;
        SeleccionarCommand = new RelayCommand(() => alSeleccionar(this));
    }

    // Disponible → DataTrigger Value="False" → gris y tachado (horario ocupado).
    public DateTime Inicio { get; }
    public bool Disponible { get; }
    public string Hora => Inicio.ToString("HH:mm");
    public string Descripcion => Disponible ? $"{Hora}, disponible" : $"{Hora}, ocupado";
    public IRelayCommand SeleccionarCommand { get; }

    // IsSelected → DataTrigger Value="True" → azul (horario elegido).
    [ObservableProperty]
    private bool isSelected;
}

// Cada turno de "Mis turnos" con sus dos botones (Cancelar y Reprogramar).
/// <summary>Turno en la lista "Mis turnos", con sus acciones.</summary>
public sealed class TurnoItem
{
    public TurnoItem(TurnoDetalle detalle, Func<TurnoItem, Task> cancelar, Func<TurnoItem, Task> reprogramar)
    {
        Detalle = detalle;
        CancelarCommand = new AsyncRelayCommand(() => cancelar(this));
        ReprogramarCommand = new AsyncRelayCommand(() => reprogramar(this));
    }

    // Detalle → en la vista, {Binding Detalle.HoraTexto}, {Binding Detalle.Medico.NombreCompleto}...
    public TurnoDetalle Detalle { get; }
    public IAsyncRelayCommand CancelarCommand { get; }
    public IAsyncRelayCommand ReprogramarCommand { get; }
}
