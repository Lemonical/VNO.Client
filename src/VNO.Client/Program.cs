using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        // settings come from the legacy data folder ini files, not a json config,
        // so the same external files the player edits drive the port
        services.AddSingleton<IOptions<ClientSettings>>(Options.Create(ClientSettingsLoader.Load()));

        // networking, the client reaches the game server. The transport comes from settings so
        // the client can dial a WebSocket server while still reaching a legacy TCP one
        services.AddSingleton<IMessageClient>(BuildGameServerLink);

        // application services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ICharacterRosterService, CharacterRosterService>();
        services.AddSingleton<ICharacterAssetService, CharacterAssetService>();
        services.AddSingleton<IClientSession, ClientSession>();
        services.AddSingleton<IImageFetcher, ImageFetcher>();
        services.AddSingleton<IAudioService, BassAudioService>();
        services.AddSingleton<IDiscordPresenceService>(sp =>
            new DiscordRpcPresenceService(
                sp.GetRequiredService<IOptions<ClientSettings>>().Value.DiscordApplicationId));
        services.AddSingleton<DiscordPresenceCoordinator>();
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

    // the game server link, game payloads carry evidence and animation so it takes the larger
    // inbound cap. The AS link is built separately inside AuthServerLink from its own settings
    private static IMessageClient BuildGameServerLink(IServiceProvider services)
    {
        var settings = services.GetRequiredService<IOptions<ClientSettings>>().Value;
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        var options = new WebSocketTransportOptions
        {
            UseTls = settings.GameServerUseTls,
            MaxInboundBytes = VNO.Core.Protocol.ProtocolConstants.MaxGameMessageBytes,
        };
        return MessageTransportFactory.CreateClient(settings.GameServerTransport, loggerFactory, options);
    }
}
