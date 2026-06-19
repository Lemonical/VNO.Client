using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using VNO.Client.Services;

namespace VNO.Client.Converters;

/// <summary>
/// Picks a themed panel background from its off and on design.ini entries
/// </summary>
/// <remarks>
/// The legacy tab and feed panels swapped between their ObjectColor entry and
/// the matching _on entry to mark the active choice. Bind the theme service and
/// the active flag, and name the base design.ini key in the parameter
/// </remarks>
public sealed class ThemeActivePanelConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is IThemeService theme &&
            values[1] is bool active && parameter is string key)
        {
            return theme.GetBrush(active ? key + "_on" : key);
        }
        return null;
    }
}
