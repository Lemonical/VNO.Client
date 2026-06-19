using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using VNO.Client.ViewModels;
using VNO.Client.Views.Dialogs;
using VNO.Client.Views.Windows;

namespace VNO.Client.Services;

/// <summary>
/// Default window service that opens the staff windows and modal dialogs
/// </summary>
/// <remarks>
/// View models are resolved from the container so the windows stay bound to the
/// same singletons used elsewhere. Open staff windows are reused rather than
/// duplicated
/// </remarks>
public sealed class WindowService : IWindowService
{
    private readonly IServiceProvider _services;

    private ModeratorWindow? _moderator;
    private AnimatorWindow? _animator;

    /// <summary>
    /// Creates the service over the application service provider
    /// </summary>
    public WindowService(IServiceProvider services) => _services = services;

    /// <inheritdoc />
    public void ShowModerator()
    {
        if (_moderator is null)
        {
            _moderator = new ModeratorWindow { DataContext = _services.GetRequiredService<ModeratorViewModel>() };
            _moderator.Closed += (_, _) => _moderator = null;
            _moderator.Show();
        }
        else
        {
            _moderator.Activate();
        }
    }

    /// <inheritdoc />
    public void ShowAnimator()
    {
        if (_animator is null)
        {
            _animator = new AnimatorWindow { DataContext = _services.GetRequiredService<AnimatorViewModel>() };
            _animator.Closed += (_, _) => _animator = null;
            _animator.Show();
        }
        else
        {
            _animator.Activate();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ShowPasswordDialogAsync()
    {
        var dialog = new PasswordDialog { DataContext = _services.GetRequiredService<PasswordDialogViewModel>() };

        var owner = MainWindow;
        if (owner is null)
        {
            return false;
        }

        return await dialog.ShowDialog<bool>(owner).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public async Task ShowMessageAsync(string message)
    {
        var owner = MainWindow;
        if (owner is null)
        {
            return;
        }

        var dialog = new MessageDialog { Message = message };
        await dialog.ShowDialog(owner).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public async Task<string?> InputBoxAsync(string title, string prompt)
    {
        var owner = MainWindow;
        if (owner is null)
        {
            return null;
        }

        var dialog = new InputDialog { Title = title, Prompt = prompt };
        return await dialog.ShowDialog<string?>(owner).ConfigureAwait(true);
    }

    private static Window? MainWindow =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
