using System.Globalization;

namespace TurnosSalud.Converters;

// CONVERTIDOR: transforma el valor de un enlace antes de mostrarlo.
// En el .xaml: IsVisible="{Binding ErrorGeneral, Converter={StaticResource HasText}}".
// ErrorGeneral es un texto pero IsVisible necesita true/false: HasText hace la conversión.
// Se registra con la clave "HasText" en Resources/Styles/Styles.xaml.
/// <summary>Devuelve true si el valor es un texto no vacío (útil para mostrar errores).</summary>
public sealed class HasTextConverter : IValueConverter
{
    // Convert: del ViewModel a la pantalla.
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s);

    // ConvertBack: de la pantalla al ViewModel (acá no se usa).
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Invierte un valor booleano.</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
