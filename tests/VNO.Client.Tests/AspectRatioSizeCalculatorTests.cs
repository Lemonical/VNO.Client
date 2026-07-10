using Avalonia;
using VNO.Client.Views;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers client-size correction while the main window is resized
/// </summary>
public sealed class AspectRatioSizeCalculatorTests
{
    [Fact]
    public void Width_led_resize_derives_height()
    {
        var result = AspectRatioSizeCalculator.Constrain(
            new Size(900, 500),
            new Size(630, 400),
            630d / 400d);

        Assert.Equal(new Size(900, 900d / (630d / 400d)), result);
    }

    [Fact]
    public void Height_led_resize_derives_width()
    {
        var result = AspectRatioSizeCalculator.Constrain(
            new Size(650, 600),
            new Size(630, 400),
            630d / 400d);

        Assert.Equal(new Size(600d * (630d / 400d), 600), result);
    }

    [Theory]
    [InlineData(456, 387)]
    [InlineData(630, 400)]
    [InlineData(726, 679)]
    [InlineData(1279, 796)]
    public void Preserves_each_screen_ratio(double width, double height)
    {
        var aspectRatio = width / height;
        var result = AspectRatioSizeCalculator.Constrain(
            new Size(width * 1.5, height * 1.2),
            new Size(width, height),
            aspectRatio);

        Assert.Equal(aspectRatio, result.AspectRatio, 10);
    }
}
