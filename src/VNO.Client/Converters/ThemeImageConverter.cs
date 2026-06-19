using System;
using System.Globalization;
using Avalonia.Data.Converters;
using VNO.Client.Services;

namespace VNO.Client.Converters;

/// <summary>
/// Resolves a theme skin image from a bound theme service and a file name parameter
/// </summary>
/// <remarks>
/// Views bind their view model's theme service and name the skin file in the
/// converter parameter, so screens stay declarative while every image still
/// comes from the player's external theme folder. A missing image yields null
/// and the Image control simply draws nothing, like the legacy client
/// </remarks>
public sealed class ThemeImageConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IThemeService theme && parameter is string relativePath)
        {
            return theme.GetImage(relativePath);
        }
        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
