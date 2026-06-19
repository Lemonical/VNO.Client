using System;
using System.Globalization;
using Avalonia.Media;

namespace VNO.Client.Services;

/// <summary>
/// Parses the Delphi TColor strings found in the legacy data files
/// </summary>
/// <remarks>
/// Theme design.ini values are hex TColor literals in $00BBGGRR byte order and
/// settings.ini also uses VCL color names like clLime. Unknown input returns
/// the caller's fallback so a broken theme never crashes the client
/// </remarks>
public static class DelphiColor
{
    /// <summary>
    /// Parses a Delphi color string, returning the fallback when it cannot be read
    /// </summary>
    public static Color Parse(string? text, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        var value = text.Trim();

        if (value.StartsWith('$'))
        {
            if (uint.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bgr))
            {
                var r = (byte)(bgr & 0xFF);
                var g = (byte)((bgr >> 8) & 0xFF);
                var b = (byte)((bgr >> 16) & 0xFF);
                return Color.FromRgb(r, g, b);
            }
            return fallback;
        }

        if (value.StartsWith("cl", StringComparison.OrdinalIgnoreCase))
        {
            return value[2..].ToLowerInvariant() switch
            {
                "black" => Colors.Black,
                "maroon" => Color.FromRgb(0x80, 0x00, 0x00),
                "green" => Color.FromRgb(0x00, 0x80, 0x00),
                "olive" => Color.FromRgb(0x80, 0x80, 0x00),
                "navy" => Color.FromRgb(0x00, 0x00, 0x80),
                "purple" => Color.FromRgb(0x80, 0x00, 0x80),
                "teal" => Color.FromRgb(0x00, 0x80, 0x80),
                "gray" or "grey" => Color.FromRgb(0x80, 0x80, 0x80),
                "silver" => Color.FromRgb(0xC0, 0xC0, 0xC0),
                "red" => Colors.Red,
                "lime" => Color.FromRgb(0x00, 0xFF, 0x00),
                "yellow" => Colors.Yellow,
                "blue" => Colors.Blue,
                "fuchsia" => Colors.Fuchsia,
                "aqua" => Colors.Aqua,
                "white" => Colors.White,
                _ => fallback,
            };
        }

        return Color.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
