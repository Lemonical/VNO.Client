using Avalonia.Controls;

namespace VNO.Client.Views;

/// <summary>
/// Code behind for the server list screen
/// </summary>
/// <remarks>
/// Lives in the Views namespace so the view locator can resolve it from
/// ServerListScreenViewModel by convention
/// </remarks>
public sealed partial class ServerListScreenView : UserControl
{
    /// <summary>
    /// Builds the screen and loads its XAML
    /// </summary>
    public ServerListScreenView() => InitializeComponent();
}
