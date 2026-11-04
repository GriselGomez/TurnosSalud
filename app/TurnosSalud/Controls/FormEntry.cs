using Microsoft.Maui.Controls.Shapes;

namespace TurnosSalud.Controls;

/// <summary>
/// Campo de formulario reutilizable: etiqueta + caja con ícono + texto de error.
/// Si IsPassword es true, agrega el botón para mostrar/ocultar la contraseña.
/// </summary>
// CONTROL PROPIO: un ContentView armado con código C#. Se usa en el .xaml como
// <controls:FormEntry Text="{Binding Dni}" Error="{Binding DniError}" />.
public class FormEntry : ContentView
{
    private static readonly Color ColorBorde = Color.FromArgb("#E2E8F0");
    private static readonly Color ColorFoco = Color.FromArgb("#0B5ED7");
    private static readonly Color ColorError = Color.FromArgb("#DC2626");

    private readonly Label _titulo;
    private readonly Image _icono;
    private readonly Entry _entry;
    private readonly ImageButton _ojo;
    private readonly Border _caja;
    private readonly Label _error;
    private bool _tieneFoco;

    // BINDABLEPROPERTY = propiedad ENLAZABLE. Para que un control propio acepte
    // {Binding ...} sus propiedades deben declararse así (una propiedad común no
    // se puede enlazar). propertyChanged: qué hacer cuando el valor cambia.
    // ----------------- Propiedades enlazables -----------------

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(FormEntry), string.Empty,
        propertyChanged: (b, _, n) => ((FormEntry)b)._titulo.Text = (string)n);

    // Text es de DOBLE VÍA por defecto (BindingMode.TwoWay):
    // ViewModel → caja: cuando cambia la propiedad, se escribe en el Entry interno.
    // caja → ViewModel: ver _entry.TextChanged en el constructor.
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(FormEntry), string.Empty, BindingMode.TwoWay,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormEntry)b;
            var nuevo = (string?)n ?? string.Empty;
            if (f._entry.Text != nuevo) f._entry.Text = nuevo;
        });

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(FormEntry), string.Empty,
        propertyChanged: (b, _, n) => ((FormEntry)b)._entry.Placeholder = (string)n);

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(FormEntry), null,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormEntry)b;
            if (n is string archivo && archivo.Length > 0)
            {
                f._icono.Source = ImageSource.FromFile(archivo);
                f._icono.IsVisible = true;
            }
            else
            {
                f._icono.IsVisible = false;
            }
        });

    // Cuando el ViewModel pone o borra el mensaje de error, se repinta el borde (rojo o gris).
    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(
        nameof(Error), typeof(string), typeof(FormEntry), null,
        propertyChanged: (b, _, _) => ((FormEntry)b).ActualizarEstado());

    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
        nameof(Keyboard), typeof(Keyboard), typeof(FormEntry), Keyboard.Default,
        propertyChanged: (b, _, n) => ((FormEntry)b)._entry.Keyboard = (Keyboard)n);

    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
        nameof(IsPassword), typeof(bool), typeof(FormEntry), false,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormEntry)b;
            f._entry.IsPassword = (bool)n;
            f._ojo.IsVisible = (bool)n;
        });

    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(
        nameof(MaxLength), typeof(int), typeof(FormEntry), int.MaxValue,
        propertyChanged: (b, _, n) => ((FormEntry)b)._entry.MaxLength = (int)n);

    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly), typeof(bool), typeof(FormEntry), false,
        propertyChanged: (b, _, n) =>
        {
            var f = (FormEntry)b;
            f._entry.IsReadOnly = (bool)n;
            f._entry.TextColor = (bool)n ? Color.FromArgb("#64748B") : Color.FromArgb("#1B2433");
            f._caja.BackgroundColor = (bool)n ? Color.FromArgb("#F1F5F9") : Colors.White;
        });

    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(
        nameof(ReturnType), typeof(ReturnType), typeof(FormEntry), ReturnType.Next,
        propertyChanged: (b, _, n) => ((FormEntry)b)._entry.ReturnType = (ReturnType)n);

    public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(
        nameof(ReturnCommand), typeof(System.Windows.Input.ICommand), typeof(FormEntry), null,
        propertyChanged: (b, _, n) => ((FormEntry)b)._entry.ReturnCommand = (System.Windows.Input.ICommand?)n);

    // Propiedades "envoltorio" que leen y escriben las BindableProperty de arriba.
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
    public ReturnType ReturnType { get => (ReturnType)GetValue(ReturnTypeProperty); set => SetValue(ReturnTypeProperty, value); }
    public System.Windows.Input.ICommand? ReturnCommand { get => (System.Windows.Input.ICommand?)GetValue(ReturnCommandProperty); set => SetValue(ReturnCommandProperty, value); }

    // Constructor: arma la etiqueta, la caja con ícono, el Entry, el ojo para la contraseña
    // y el texto de error.
    public FormEntry()
    {
        _titulo = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1B2433"),
            Margin = new Thickness(2, 0, 0, 0)
        };

        _icono = new Image { WidthRequest = 20, HeightRequest = 20, VerticalOptions = LayoutOptions.Center, IsVisible = false };

        _entry = new Entry
        {
            VerticalOptions = LayoutOptions.Center,
            ReturnType = ReturnType.Next,
            IsSpellCheckEnabled = false,
            IsTextPredictionEnabled = false
        };
        // Cada vez que el usuario escribe, se copia a Text, y como Text está enlazada
        // en doble vía, el valor llega a la propiedad del ViewModel.
        _entry.TextChanged += (_, e) => { if (Text != e.NewTextValue) Text = e.NewTextValue ?? string.Empty; };
        _entry.Focused += (_, _) => { _tieneFoco = true; ActualizarEstado(); };
        _entry.Unfocused += (_, _) => { _tieneFoco = false; ActualizarEstado(); };

        _ojo = new ImageButton
        {
            Source = "ic_eye.png",
            WidthRequest = 40,
            HeightRequest = 40,
            Padding = 10,
            BackgroundColor = Colors.Transparent,
            IsVisible = false
        };
        SemanticProperties.SetDescription(_ojo, "Mostrar contraseña");
        _ojo.Clicked += (_, _) =>
        {
            _entry.IsPassword = !_entry.IsPassword;
            _ojo.Source = _entry.IsPassword ? "ic_eye.png" : "ic_eye_off.png";
            SemanticProperties.SetDescription(_ojo, _entry.IsPassword ? "Mostrar contraseña" : "Ocultar contraseña");
        };

        var fila = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 10
        };
        fila.Add(_icono, 0);
        fila.Add(_entry, 1);
        fila.Add(_ojo, 2);

        _caja = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = ColorBorde,
            StrokeThickness = 1.2,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(12, 2, 4, 2),
            MinimumHeightRequest = 50,
            Content = fila
        };

        // Tocar la caja enfoca el campo (área táctil más grande).
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => _entry.Focus();
        _caja.GestureRecognizers.Add(tap);

        _error = new Label
        {
            FontSize = 12,
            TextColor = ColorError,
            Margin = new Thickness(4, 0, 0, 0),
            IsVisible = false
        };

        Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children = { _titulo, _caja, _error }
        };
    }

    // Pinta el borde: rojo si hay error, azul si tiene el foco, gris si no.
    private void ActualizarEstado()
    {
        var hayError = !string.IsNullOrWhiteSpace(Error);
        _error.Text = Error;
        _error.IsVisible = hayError;
        _caja.Stroke = hayError ? ColorError : _tieneFoco ? ColorFoco : ColorBorde;
        _caja.StrokeThickness = hayError || _tieneFoco ? 1.6 : 1.2;
        SemanticProperties.SetHint(_entry, hayError ? Error : Title);
    }
}
