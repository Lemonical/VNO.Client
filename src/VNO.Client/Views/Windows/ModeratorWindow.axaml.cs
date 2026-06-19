using Avalonia.Controls;

namespace VNO.Client.Views.Windows;

/// <summary>
/// Code behind for the moderator interface window
/// </summary>
/// <remarks>
/// Ports Form1 as a standalone window, opened by the window service. Behavior
/// lives in the bound moderator view model
/// </remarks>
public sealed partial class ModeratorWindow : Window
{
    /// <summary>
    /// Builds the window and loads its XAML
    /// </summary>
    public ModeratorWindow() => InitializeComponent();
}
