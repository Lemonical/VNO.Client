using Avalonia.Controls;

namespace VNO.Client.Views;

/// <summary>
/// Code behind for the login screen
/// </summary>
/// <remarks>
/// Lives in the Views namespace so the view locator can resolve it from
/// LoginScreenViewModel by convention
/// </remarks>
public sealed partial class LoginScreenView : UserControl
{
    /// <summary>
    /// Builds the screen and loads its XAML
    /// </summary>
    public LoginScreenView() => InitializeComponent();
}
