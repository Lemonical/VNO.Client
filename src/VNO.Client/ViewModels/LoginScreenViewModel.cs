using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VNO.Client.Services;
using VNO.Core.Models;
using VNO.Core.Protocol;

namespace VNO.Client.ViewModels;

/// <summary>
/// View model for the login screen
/// </summary>
/// <remarks>
/// Ports groupbox_login from Form15. The skin image, colors, and fonts come from
/// the player's theme through the theme service, matching the legacy design.ini
/// driven look. The version label text is the runtime string FormCreate assigned,
/// not the 1.0 placeholder in the DFM, and the AS label flips from AS:ERROR to
/// 'AS: OK' when the master connection succeeds, exactly like the original.
/// When settings.ini has User enabled=1 the credentials are prefilled and the
/// remember me box starts checked
/// </remarks>
public sealed partial class LoginScreenViewModel : ViewModelBase
{
    private readonly IClientNavigator _navigator;
    private readonly IAuthServerLink _authLink;
    private readonly IThemeService _theme;
    private readonly IWindowService _windows;

    [ObservableProperty]
    private string _userName = "Username";

    [ObservableProperty]
    private string _password = "Password";

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private string _authStatus = "AS:ERROR";

    // the FormCreate default shown until the AS answers the version check, the
    // decompiled string is truncated so the tail is a best effort completion
    [ObservableProperty]
    private string _news =
        "Could not connect to the master server.\nYou either have the wrong version or the server is down.";

    [ObservableProperty]
    private string _takenNotice = string.Empty;

    /// <summary>
    /// Runtime caption FormCreate put on label_version in the original client
    /// </summary>
    public string Version { get; } = "Remix 3rd Strike";

    /// <summary>
    /// Login skin from the theme folder, null when the theme has no login.png
    /// </summary>
    public Bitmap? BackgroundImage { get; }

    /// <summary>Foreground of the AS status label</summary>
    public IBrush AsForeground { get; }

    /// <summary>Font of the AS status label</summary>
    public FontFamily AsFontFamily { get; }

    /// <summary>Font size of the AS status label</summary>
    public double AsFontSize { get; }

    /// <summary>Foreground of the version label</summary>
    public IBrush VersionForeground { get; }

    /// <summary>Font of the version label</summary>
    public FontFamily VersionFontFamily { get; }

    /// <summary>Font size of the version label</summary>
    public double VersionFontSize { get; }

    /// <summary>Background of the username box</summary>
    public IBrush UserNameBackground { get; }

    /// <summary>Foreground of the username box</summary>
    public IBrush UserNameForeground { get; }

    /// <summary>Font of the username box</summary>
    public FontFamily UserNameFontFamily { get; }

    /// <summary>Font size of the username box</summary>
    public double UserNameFontSize { get; }

    /// <summary>Background of the password box</summary>
    public IBrush PasswordBackground { get; }

    /// <summary>Foreground of the password box</summary>
    public IBrush PasswordForeground { get; }

    /// <summary>Font of the password box</summary>
    public FontFamily PasswordFontFamily { get; }

    /// <summary>Font size of the password box</summary>
    public double PasswordFontSize { get; }

    /// <summary>Foreground of the Remember Me label</summary>
    public IBrush RememberMeForeground { get; }

    /// <summary>Font of the Remember Me label</summary>
    public FontFamily RememberMeFontFamily { get; }

    /// <summary>Font size of the Remember Me label</summary>
    public double RememberMeFontSize { get; }

    /// <summary>Background of the Identify button</summary>
    public IBrush IdentifyBackground { get; }

    /// <summary>Foreground of the Identify button</summary>
    public IBrush IdentifyForeground { get; }

    /// <summary>Font of the Identify button</summary>
    public FontFamily IdentifyFontFamily { get; }

    /// <summary>Font size of the Identify button</summary>
    public double IdentifyFontSize { get; }

    /// <summary>Background of the Create Account button</summary>
    public IBrush CreateAccountBackground { get; }

    /// <summary>Foreground of the Create Account button</summary>
    public IBrush CreateAccountForeground { get; }

    /// <summary>Font of the Create Account button</summary>
    public FontFamily CreateAccountFontFamily { get; }

    /// <summary>Font size of the Create Account button</summary>
    public double CreateAccountFontSize { get; }

    /// <summary>
    /// Creates the login screen over the navigator, the auth server link, and the theme
    /// </summary>
    public LoginScreenViewModel(
        IClientNavigator navigator, IAuthServerLink authLink, IThemeService theme, IWindowService windows)
    {
        _navigator = navigator;
        _authLink = authLink;
        _theme = theme;
        _windows = windows;

        BackgroundImage = theme.GetImage("login.png");

        AsForeground = theme.GetBrush("label_as_font_color");
        AsFontFamily = theme.GetFontFamily("label_as");
        AsFontSize = theme.GetFontSize("label_as");

        VersionForeground = theme.GetBrush("label_version_font_color");
        VersionFontFamily = theme.GetFontFamily("label_version");
        VersionFontSize = theme.GetFontSize("label_version");

        UserNameBackground = theme.GetBrush("edit_username");
        UserNameForeground = theme.GetBrush("edit_username_font_color");
        UserNameFontFamily = theme.GetFontFamily("edit_username");
        UserNameFontSize = theme.GetFontSize("edit_username");

        PasswordBackground = theme.GetBrush("edit_password");
        PasswordForeground = theme.GetBrush("edit_password_font_color");
        PasswordFontFamily = theme.GetFontFamily("edit_password");
        PasswordFontSize = theme.GetFontSize("edit_password");

        RememberMeForeground = theme.GetBrush("label7_font_color");
        RememberMeFontFamily = theme.GetFontFamily("label7");
        RememberMeFontSize = theme.GetFontSize("label7");

        IdentifyBackground = theme.GetBrush("panel_btn_identify");
        IdentifyForeground = theme.GetBrush("panel_btn_identify_font_color");
        IdentifyFontFamily = theme.GetFontFamily("panel_btn_identify");
        IdentifyFontSize = theme.GetFontSize("panel_btn_identify");

        CreateAccountBackground = theme.GetBrush("panel_btn_createaccount");
        CreateAccountForeground = theme.GetBrush("panel_btn_createaccount_font_color");
        CreateAccountFontFamily = theme.GetFontFamily("panel_btn_createaccount");
        CreateAccountFontSize = theme.GetFontSize("panel_btn_createaccount");

        // legacy remember me, settings.ini [User] enabled=1 prefills both boxes
        if (theme.ReadSettingInteger("User", "enabled", 0) == 1)
        {
            RememberMe = true;
            UserName = theme.ReadSetting("User", "user", "Placeholder");
            Password = theme.ReadSetting("User", "pass", "Placeholder");
        }

        _authLink.StateChanged += OnAuthStateChanged;
        _authLink.NewsReceived += OnNewsReceived;
        _authLink.VersionRejected += OnVersionRejected;
        _authLink.ConnectFailed += OnConnectFailed;
        ApplyAuthState(_authLink.State);

        // kick off the AS connection in the background, the label reflects the result
        _ = _authLink.ConnectAsync();
    }

    private void OnAuthStateChanged(object? sender, ConnectionState state)
    {
        // the link raises this off the UI thread, marshal before touching bound state
        Dispatcher.UIThread.Post(() => ApplyAuthState(state));
    }

    private void OnNewsReceived(object? sender, string news) =>
        Dispatcher.UIThread.Post(() => News = news);

    private void OnVersionRejected(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() =>
            _ = _windows.ShowMessageAsync("Wrong VNO version, visit the tumblr to get the latest release."));

    private void OnConnectFailed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await _windows.ShowMessageAsync(
                "Connection to the authentication server failed. Sign-in is required before joining a server.");
        });
    }

    private void ApplyAuthState(ConnectionState state) => AuthStatus = state switch
    {
        // the original only ever swapped the DFM caption for 'AS: OK' on connect
        ConnectionState.Connected => "AS: OK",
        _ => "AS:ERROR",
    };

    [RelayCommand]
    private async Task IdentifyAsync()
    {
        // the legacy CO command sent the MD5 of the password box
        var result = await _authLink.LoginAsync(UserName, LegacyHash.ToWireCredential(Password));
        switch (result)
        {
            case MasterLoginResult.Granted:
                PersistRememberMe();
                _navigator.ShowServerList();
                break;
            case MasterLoginResult.Denied:
                await _windows.ShowMessageAsync("This account doesn't exist or the password is incorrect.");
                break;
            case MasterLoginResult.Banned:
                await _windows.ShowMessageAsync("This account was banned from VNO.");
                break;
            case MasterLoginResult.TimedOut:
                await _windows.ShowMessageAsync("The auth server did not answer.");
                break;
        }
    }

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        // the legacy flow prompted with two InputBox dialogs, sent CA with the
        // MD5 of the password, and reported the result through label_taken
        var username = await _windows.InputBoxAsync("New Account", "Enter your desired username");
        string? password = null;
        if (!string.IsNullOrEmpty(username))
        {
            password = await _windows.InputBoxAsync("New Account", "Enter your desired password");
        }
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await _windows.ShowMessageAsync("Account creation cancelled.");
            return;
        }

        var result = await _authLink.CreateAccountAsync(username, LegacyHash.ToWireCredential(password));
        TakenNotice = result switch
        {
            AccountCreateResult.Created => "Account created.",
            AccountCreateResult.Taken => "Account name already taken.",
            AccountCreateResult.Invalid => "Account couldn't be created.",
            AccountCreateResult.TimedOut => "The auth server did not answer.",
            _ => TakenNotice,
        };
    }

    private void PersistRememberMe()
    {
        // the legacy VNAL handler wrote the User block on a successful login
        if (RememberMe)
        {
            _theme.WriteSetting("User", "enabled", "1");
            _theme.WriteSetting("User", "user", UserName);
            _theme.WriteSetting("User", "pass", LegacyHash.ToWireCredential(Password));
        }
        else
        {
            _theme.WriteSetting("User", "enabled", "0");
        }
    }
}
