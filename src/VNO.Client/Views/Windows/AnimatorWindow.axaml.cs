using Avalonia.Controls;

namespace VNO.Client.Views.Windows;

/// <summary>
/// Code behind for the animator interface window
/// </summary>
/// <remarks>
/// Ports Form7 as a standalone window, opened by the window service. Behavior
/// lives in the bound animator view model
/// </remarks>
public sealed partial class AnimatorWindow : Window
{
    /// <summary>
    /// Builds the window and loads its XAML
    /// </summary>
    public AnimatorWindow() => InitializeComponent();
}
