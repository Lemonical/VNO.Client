using Avalonia.Media;
using VNO.Client.Services;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers the Delphi TColor string parser used for design.ini values
/// </summary>
public sealed class DelphiColorTests
{
    [Theory]
    [InlineData("$00ffffff", 0xFF, 0xFF, 0xFF)]
    [InlineData("$000000ff", 0xFF, 0x00, 0x00)]
    [InlineData("$0028d004", 0x04, 0xD0, 0x28)]
    [InlineData("$003e3e3e", 0x3E, 0x3E, 0x3E)]
    public void Parses_delphi_bgr_hex(string text, byte r, byte g, byte b)
    {
        var color = DelphiColor.Parse(text, Colors.Black);

        Assert.Equal(Color.FromRgb(r, g, b), color);
    }

    [Theory]
    [InlineData("clLime", 0x00, 0xFF, 0x00)]
    [InlineData("clYellow", 0xFF, 0xFF, 0x00)]
    [InlineData("clSilver", 0xC0, 0xC0, 0xC0)]
    public void Parses_vcl_color_names(string text, byte r, byte g, byte b)
    {
        var color = DelphiColor.Parse(text, Colors.Black);

        Assert.Equal(Color.FromRgb(r, g, b), color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("$zznothex")]
    [InlineData("clNotAColor")]
    [InlineData("garbage")]
    public void Falls_back_on_unparseable_input(string? text)
    {
        var color = DelphiColor.Parse(text, Colors.Magenta);

        Assert.Equal(Colors.Magenta, color);
    }
}
