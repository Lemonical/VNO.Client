using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using VNO.Client.Services;

namespace VNO.Client.Converters;

/// <summary>
/// Picks a music or area list entry color from whether the client has the file
/// </summary>
/// <remarks>
/// The legacy lists drew entries you have the file for in the listbox_item color
/// and entries you are missing in the listbox_item_missing color. Bind the theme
/// service and the has file flag
/// </remarks>
public sealed class ThemeItemAvailabilityConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is IThemeService theme && values[1] is bool hasFile)
        {
            return theme.GetBrush(hasFile ? "listbox_item" : "listbox_item_missing");
        }
        return null;
    }
}
