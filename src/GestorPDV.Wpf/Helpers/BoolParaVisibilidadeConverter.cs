using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestorPDV.Wpf.Helpers;

// Converte bool (true = visível) ou string (não vazia = visível) em
// Visibility, para reaproveitar o mesmo conversor tanto em flags quanto em
// mensagens de erro condicionais.
public class BoolParaVisibilidadeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        bool ligado => ligado ? Visibility.Visible : Visibility.Collapsed,
        string texto => string.IsNullOrWhiteSpace(texto) ? Visibility.Collapsed : Visibility.Visible,
        null => Visibility.Collapsed,
        _ => Visibility.Visible
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
