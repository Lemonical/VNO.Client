using System.Threading.Tasks;

namespace VNO.Client.Services;

/// <summary>
/// Opens the auxiliary staff windows and modal dialogs
/// </summary>
/// <remarks>
/// The legacy client used separate top level forms for the moderator (Form1) and
/// animator (Form7) interfaces, and modal dialogs for password entry. View models
/// ask for these through this interface so they never touch window types directly
/// </remarks>
public interface IWindowService
{
    /// <summary>
    /// Shows the moderator interface window, bringing it forward if already open
    /// </summary>
    void ShowModerator();

    /// <summary>
    /// Shows the animator interface window, bringing it forward if already open
    /// </summary>
    void ShowAnimator();

    /// <summary>
    /// Shows the password list dialog and returns true if the user accepted it
    /// </summary>
    Task<bool> ShowPasswordDialogAsync();

    /// <summary>
    /// Shows a modal message popup, the legacy ShowMessage
    /// </summary>
    Task ShowMessageAsync(string message);

    /// <summary>
    /// Prompts for one value, the legacy InputBox. Null when cancelled
    /// </summary>
    Task<string?> InputBoxAsync(string title, string prompt);
}
