using System;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VNO.Client.Services;
using VNO.Client.ViewModels;
using VNO.Core.Networking;

namespace VNO.Client;

/// <summary>
/// Entry point and composition root for the client app
/// </summary>
/// <remarks>
/// Builds the dependency injection container then starts Avalonia. Every service
/// and view model is registered here so the rest of the code only asks for
/// interfaces
/// </remarks>
public static class Program
{
    /// <summary>
    /// Process entry point
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        var services = BuildServiceProvider();
        BuildAvaloniaApp(services).StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Builds the Avalonia app and hands it the service provider
    /// </summary>
    public static AppBuilder BuildAvaloniaApp(IServiceProvider services) =>
        AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Designer entry point, builds the app without a configured provider
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static IServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        services.Configure<ClientSettings>(configuration.GetSection("Client"));

        // networking, the client reaches the game server
        services.AddSingleton<IMessageClient, TcpMessageClient>();

        // application services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ICharacterRosterService, CharacterRosterService>();
        services.AddSingleton<ICharacterAssetService, CharacterAssetService>();
        services.AddSingleton<IClientSession, ClientSession>();
        services.AddSingleton<IImageFetcher, ImageFetcher>();
        services.AddSingleton<IAudioService, BassAudioService>();
        services.AddSingleton<IServerConnection, ServerConnection>();
        services.AddSingleton<IAuthServerLink, AuthServerLink>();
        services.AddSingleton<IWindowService, WindowService>();

        // navigation shell, the interface resolves to the same singleton so
        // screens and the shell share one navigator
        services.AddSingleton<ClientNavigator>();
        services.AddSingleton<IClientNavigator>(sp => sp.GetRequiredService<ClientNavigator>());
        services.AddSingleton<MainWindowViewModel>();

        // screen view models, one window swaps between them
        services.AddSingleton<LoginScreenViewModel>();
        services.AddSingleton<ServerListScreenViewModel>();
        services.AddSingleton<CharacterSelectScreenViewModel>();
        services.AddSingleton<GameStageViewModel>();

        // staff windows and dialogs
        services.AddSingleton<ModeratorViewModel>();
        services.AddSingleton<AnimatorViewModel>();
        services.AddTransient<LoginDialogViewModel>();
        services.AddTransient<PasswordDialogViewModel>();

        return services.BuildServiceProvider();
    }
}
