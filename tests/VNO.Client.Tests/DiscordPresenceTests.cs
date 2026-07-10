using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VNO.Client.Services;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers Rich Presence privacy projection and failure isolation.
/// </summary>
public sealed class DiscordPresenceTests
{
    [Fact]
    public void Default_settings_disable_presence()
    {
        var settings = new ClientSettings();

        Assert.Equal(DiscordPresenceMode.Off, settings.DiscordPresence);
        Assert.Null(DiscordPresenceProjector.Project(
            settings.DiscordPresence,
            new DiscordPresenceContext("Private room", 1, 4)));
    }

    [Fact]
    public void Running_mode_does_not_publish_server_or_player_data()
    {
        var presence = DiscordPresenceProjector.Project(
            DiscordPresenceMode.Running,
            new DiscordPresenceContext("Attorney District", 12, 40));

        Assert.NotNull(presence);
        Assert.Equal("Playing Visual Novel Online", presence.Details);
        Assert.Null(presence.State);
    }

    [Fact]
    public void Public_server_mode_sanitises_the_display_name()
    {
        var presence = DiscordPresenceProjector.Project(
            DiscordPresenceMode.PublicServer,
            new DiscordPresenceContext("  Attorney\r\n District  ", 12, 40));

        Assert.NotNull(presence);
        Assert.Equal("Server: Attorney District", presence.State);
        Assert.DoesNotContain("12", presence.State);
    }

    [Theory]
    [InlineData(12, 40, "Server: Attorney District · Players: 12/40")]
    [InlineData(41, 40, "Server: Attorney District")]
    [InlineData(-1, 40, "Server: Attorney District")]
    [InlineData(1, 0, "Server: Attorney District")]
    public void Player_count_is_published_only_when_valid(int online, int capacity, string expected)
    {
        var presence = DiscordPresenceProjector.Project(
            DiscordPresenceMode.PublicServerAndPlayerCount,
            new DiscordPresenceContext("Attorney District", online, capacity));

        Assert.NotNull(presence);
        Assert.Equal(expected, presence.State);
    }

    [Fact]
    public async Task Coordinator_never_publishes_a_non_directory_server()
    {
        var service = new RecordingPresenceService();
        await using var coordinator = CreateCoordinator(
            service,
            DiscordPresenceMode.PublicServerAndPlayerCount);

        await coordinator.ShowServerAsync("Private room", false, "4 / 10 players");

        var presence = Assert.Single(service.Updates);
        Assert.Null(presence.State);
    }

    [Fact]
    public async Task Discord_failures_do_not_escape_the_coordinator()
    {
        var service = new ThrowingPresenceService();
        await using var coordinator = CreateCoordinator(service, DiscordPresenceMode.Running);

        await coordinator.StartAsync();
        await coordinator.ClearAsync();
    }

    private static DiscordPresenceCoordinator CreateCoordinator(
        IDiscordPresenceService service,
        DiscordPresenceMode mode) =>
        new(
            service,
            Options.Create(new ClientSettings { DiscordPresence = mode }),
            NullLogger<DiscordPresenceCoordinator>.Instance);

    private sealed class RecordingPresenceService : IDiscordPresenceService
    {
        public System.Collections.Generic.List<DiscordPresence> Updates { get; } = [];

        public Task UpdateAsync(DiscordPresence presence, CancellationToken cancellationToken = default)
        {
            Updates.Add(presence);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingPresenceService : IDiscordPresenceService
    {
        public Task UpdateAsync(DiscordPresence presence, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Discord is unavailable"));

        public Task ClearAsync(CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("Discord is unavailable"));
    }
}
