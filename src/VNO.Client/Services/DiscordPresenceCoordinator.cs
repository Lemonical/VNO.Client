using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VNO.Client.Services;

/// <summary>
/// Coordinates privacy projection and isolates gameplay from Discord failures.
/// </summary>
public sealed class DiscordPresenceCoordinator : IAsyncDisposable
{
    private readonly IDiscordPresenceService _service;
    private readonly ClientSettings _settings;
    private readonly ILogger<DiscordPresenceCoordinator> _logger;

    /// <summary>
    /// Creates the coordinator over the configured privacy choice.
    /// </summary>
    public DiscordPresenceCoordinator(
        IDiscordPresenceService service,
        IOptions<ClientSettings> settings,
        ILogger<DiscordPresenceCoordinator> logger)
    {
        _service = service;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Publishes the minimum running-only presence permitted by the player.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default) =>
        PublishAsync(new DiscordPresenceContext(), cancellationToken);

    /// <summary>
    /// Publishes a server only when it came from the public Master directory.
    /// </summary>
    public Task ShowServerAsync(
        string serverName,
        bool isPublicDirectoryEntry,
        string? playerCount,
        CancellationToken cancellationToken = default)
    {
        var (online, capacity) = ParsePlayerCount(playerCount);
        var context = isPublicDirectoryEntry
            ? new DiscordPresenceContext(serverName, online, capacity)
            : new DiscordPresenceContext();
        return PublishAsync(context, cancellationToken);
    }

    /// <summary>
    /// Clears activity without allowing a Discord outage to affect VNO.
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _service.ClearAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Discord Rich Presence could not be cleared");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ClearAsync().ConfigureAwait(false);
        if (_service is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task PublishAsync(DiscordPresenceContext context, CancellationToken cancellationToken)
    {
        var presence = DiscordPresenceProjector.Project(_settings.DiscordPresence, context);
        if (presence is null)
        {
            await ClearAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            await _service.UpdateAsync(presence, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Discord Rich Presence could not be updated");
        }
    }

    private static (int? Online, int? Capacity) ParsePlayerCount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        var separator = value.IndexOf('/');
        if (separator < 1)
        {
            return (null, null);
        }

        var left = value[..separator].Trim();
        var right = value[(separator + 1)..].Trim();
        var firstSpace = right.IndexOf(' ');
        if (firstSpace >= 0)
        {
            right = right[..firstSpace];
        }

        return int.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out var online) &&
            int.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out var capacity) &&
            online >= 0 && capacity > 0 && online <= capacity
                ? (online, capacity)
                : (null, null);
    }
}
