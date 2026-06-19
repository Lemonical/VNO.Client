using System;
using System.Globalization;
using Avalonia.Data.Converters;
using VNO.Client.Services;

namespace VNO.Client.Converters;

/// <summary>
/// Resolves a design.ini Font family from a bound theme service and a key parameter
/// </summary>
public sealed class ThemeFontConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IThemeService theme && parameter is string key)
        {
            return theme.GetFontFamily(key);
        }
        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
