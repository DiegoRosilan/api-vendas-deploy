using System.Globalization;
using System.Windows.Data;

namespace GestorPDV.Wpf.Helpers;

// DatePicker.SelectedDate é DateTime?; os campos de domínio usam DateOnly?
// (mais adequado para datas sem hora). Este conversor faz a ponte entre os
// dois, só para uso em binding de UI.
public class DateOnlyParaDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateOnly data ? data.ToDateTime(TimeOnly.MinValue) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime data ? DateOnly.FromDateTime(data) : null;
}
