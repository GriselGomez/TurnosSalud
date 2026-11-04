using Microsoft.Maui.Controls.Shapes;

namespace TurnosSalud.Controls;

/// <summary>Avatar circular con iniciales (sin fotos: protege la privacidad y no requiere red).</summary>
public class AvatarView : ContentView
{
    private readonly Border _circulo;
    private readonly Label _texto;

    // Propiedades ENLAZABLES: en el .xaml se usan como Initials="{Binding Iniciales}"
    // y Color="{Binding AvatarColor}". Al cambiar, se actualiza el círculo.
    public static readonly BindableProperty InitialsProperty = BindableProperty.Create(
        nameof(Initials), typeof(string), typeof(AvatarView), string.Empty,
        propertyChanged: (b, _, n) => ((AvatarView)b)._texto.Text = (string)n);

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(AvatarView), Color.FromArgb("#0B5ED7"),
        propertyChanged: (b, _, n) => ((AvatarView)b)._circulo.BackgroundColor = (Color)n);

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(AvatarView), 52d,
        propertyChanged: (b, _, n) => ((AvatarView)b).AplicarTamano((double)n));

    public string Initials { get => (string)GetValue(InitialsProperty); set => SetValue(InitialsProperty, value); }
    public Color Color { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    // Constructor: arma el círculo (Border) con el texto de las iniciales adentro.
    public AvatarView()
    {
        _texto = new Label
        {
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        _circulo = new Border
        {
            StrokeThickness = 3,
            Stroke = Color.FromArgb("#EAF2FF"),
            BackgroundColor = Color.FromArgb("#0B5ED7"),
            Content = _texto,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center
        };

        Content = _circulo;
        AplicarTamano(Size);
    }

    // Ajusta el tamaño del círculo y de la letra según Size.
    private void AplicarTamano(double tamano)
    {
        _circulo.WidthRequest = tamano;
        _circulo.HeightRequest = tamano;
        _circulo.StrokeShape = new RoundRectangle { CornerRadius = tamano / 2 };
        _texto.FontSize = Math.Round(tamano * 0.36);
    }
}
