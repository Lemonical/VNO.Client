using Avalonia;

namespace VNO.Client.Views;

internal static class AspectRatioSizeCalculator
{
    public static Size Constrain(Size requestedSize, Size previousSize, double aspectRatio)
    {
        var relativeWidthChange = Math.Abs(requestedSize.Width - previousSize.Width) / previousSize.Width;
        var relativeHeightChange = Math.Abs(requestedSize.Height - previousSize.Height) / previousSize.Height;

        return relativeWidthChange >= relativeHeightChange
            ? new Size(requestedSize.Width, requestedSize.Width / aspectRatio)
            : new Size(requestedSize.Height * aspectRatio, requestedSize.Height);
    }
}
