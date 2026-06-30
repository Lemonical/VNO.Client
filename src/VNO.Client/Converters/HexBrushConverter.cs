using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace VNO.Client.Converters;

/// <summary>
/// Turns an argb hex color string into a brush for the HP and MP readouts
/// </summary>
/// <remarks>
/// The animator sends a font color as a hex string like <c>#FFFF0000</c>, stored on
/// <see cref="VNO.Core.Models.PlayerStats"/>. A missing or bad value falls back to
/// white so a label is always legible
/// </remarks>
public sealed class HexBrushConverter : IValueConverter
{
    private static readonly IBrush Fallback = Brushes.White;

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && Color.TryParse(text, out var color))
        {
            return new SolidColorBrush(color);
        }

        return Fallback;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
