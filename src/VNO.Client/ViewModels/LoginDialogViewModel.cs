using CommunityToolkit.Mvvm.ComponentModel;

namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the login dialog
/// </summary>
/// <remarks>
/// Ports the legacy LoginDialog which collected a user name and password before
/// connecting. The view binds the two fields and the host reads them back when
/// the user accepts the dialog
/// </remarks>
public sealed partial class LoginDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    /// <summary>
    /// True when both fields hold a value, used to enable the accept button
    /// </summary>
    public bool CanSubmit => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);

    partial void OnUserNameChanged(string value) => OnPropertyChanged(nameof(CanSubmit));

    partial void OnPasswordChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
}
