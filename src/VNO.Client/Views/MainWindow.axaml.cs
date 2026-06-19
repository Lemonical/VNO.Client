using Avalonia.Controls;

namespace VNO.Client.Views;

/// <summary>
/// Code behind for the main client window
/// </summary>
/// <remarks>
/// Stays empty on purpose, all behavior lives in the bound view model
/// </remarks>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Builds the window and loads its XAML
    /// </summary>
    public MainWindow() => InitializeComponent();
}
