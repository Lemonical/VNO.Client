using System;
using System.Globalization;
using Avalonia.Data.Converters;
using VNO.Client.Services;

namespace VNO.Client.Converters;

/// <summary>
/// Resolves a design.ini ObjectColor brush from a bound theme service and a key parameter
/// </summary>
/// <remarks>
/// Views bind their view model's theme service and name the design.ini key in
/// the converter parameter, mirroring how the legacy design apply pass painted
/// each control from its ObjectColor entry
/// </remarks>
public sealed class ThemeBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IThemeService theme && parameter is string key)
        {
            return theme.GetBrush(key);
        }
        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
