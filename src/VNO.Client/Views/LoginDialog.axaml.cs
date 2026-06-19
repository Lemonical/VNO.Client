using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VNO.Client.Views;

/// <summary>
/// Code behind for the login dialog
/// </summary>
/// <remarks>
/// The two button handlers only close the window with a result, they hold no
/// logic. The caller reads the bound view model after a true result
/// </remarks>
public sealed partial class LoginDialog : Window
{
    /// <summary>
    /// Builds the dialog and loads its XAML
    /// </summary>
    public LoginDialog() => InitializeComponent();

    private void OnAccept(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
