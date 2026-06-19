namespace VNO.Client.ViewModels;

/// <summary>
/// Switches the single client window between its full screen states
/// </summary>
/// <remarks>
/// The legacy Form15 was one window that showed or hid large group boxes to move
/// between login, the server list, character select, and the game stage. This
/// interface models that same flow without the screens needing to know about each
/// other
/// </remarks>
public interface IClientNavigator
{
    /// <summary>
    /// Shows the login screen
    /// </summary>
    void ShowLogin();

    /// <summary>
    /// Shows the server list screen
    /// </summary>
    void ShowServerList();

    /// <summary>
    /// Shows the character select screen
    /// </summary>
    void ShowCharacterSelect();

    /// <summary>
    /// Shows the in game stage
    /// </summary>
    void ShowGameStage();
}
