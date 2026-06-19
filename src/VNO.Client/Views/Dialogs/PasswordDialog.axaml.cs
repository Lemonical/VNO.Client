using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VNO.Client.Views.Dialogs;

/// <summary>
/// Code behind for the password list dialog
/// </summary>
/// <remarks>
/// The two button handlers only close the window with a result, the list itself
/// is managed by the bound view model
/// </remarks>
public sealed partial class PasswordDialog : Window
{
    /// <summary>
    /// Builds the dialog and loads its XAML
    /// </summary>
    public PasswordDialog() => InitializeComponent();

    private void OnAccept(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
