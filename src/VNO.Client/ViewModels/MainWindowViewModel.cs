namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the single client window shell
/// </summary>
/// <remarks>
/// Ports the outer Form15 window. Form15 was one window that swapped large group
/// boxes between login, the server list, character select, and the game stage.
/// Here the shell simply exposes the navigator whose current screen the window
/// hosts in a content control, and starts on the login screen
/// </remarks>
public sealed class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Creates the shell and shows the login screen first
    /// </summary>
    public MainWindowViewModel(ClientNavigator navigator)
    {
        Navigator = navigator;
        Navigator.ShowLogin();
    }

    /// <summary>
    /// The navigator whose current screen the window displays
    /// </summary>
    public ClientNavigator Navigator { get; }
}
