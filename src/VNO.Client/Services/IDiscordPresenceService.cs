using System.Threading;
using System.Threading.Tasks;

namespace VNO.Client.Services;

/// <summary>
/// Publishes optional activity to the locally running Discord desktop client.
/// </summary>
public interface IDiscordPresenceService
{
    /// <summary>
    /// Replaces the current display-only activity.
    /// </summary>
    Task UpdateAsync(DiscordPresence presence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes any activity previously published by VNO.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
