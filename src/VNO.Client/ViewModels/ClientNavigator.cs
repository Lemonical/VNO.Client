using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace VNO.Client.ViewModels;

/// <summary>
/// Default navigator that holds the active screen for the shell to display
/// </summary>
/// <remarks>
/// Screens are resolved lazily from the container so this service can depend on
/// every screen view model without creating a construction cycle. The shell binds
/// to <see cref="CurrentScreen"/> through a content control
/// </remarks>
public sealed partial class ClientNavigator : ObservableObject, IClientNavigator
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ViewModelBase? _currentScreen;

    /// <summary>
    /// Creates the navigator over the application service provider
    /// </summary>
    public ClientNavigator(IServiceProvider services) => _services = services;

    /// <inheritdoc />
    public void ShowLogin() => CurrentScreen = _services.GetRequiredService<LoginScreenViewModel>();

    /// <inheritdoc />
    public void ShowServerList() => CurrentScreen = _services.GetRequiredService<ServerListScreenViewModel>();

    /// <inheritdoc />
    public void ShowCharacterSelect() => CurrentScreen = _services.GetRequiredService<CharacterSelectScreenViewModel>();

    /// <inheritdoc />
    public void ShowGameStage() => CurrentScreen = _services.GetRequiredService<GameStageViewModel>();
}
