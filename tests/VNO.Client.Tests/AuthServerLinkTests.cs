using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VNO.Client.Services;
using VNO.Core.Models;
using VNO.Core.Networking;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers the Client-specific Master endpoint and version handshake.
/// </summary>
public sealed class AuthServerLinkTests
{
    [Fact]
    public async Task Connect_uses_the_shared_master_endpoint_and_application_version()
    {
        var client = new FakeMessageClient();
        await using var link = new AuthServerLink(
            client,
            Options.Create(new ClientSettings()),
            new ClientSession(),
            NullLogger<AuthServerLink>.Instance);

        await link.ConnectAsync();

        Assert.Equal(MasterServerEndpoint.Host, client.ConnectedHost);
        Assert.Equal(MasterServerEndpoint.Port, client.ConnectedPort);
        var version = Assert.Single(client.Sent);
        Assert.Equal(MessageType.VersionCheck, version.Type);
        Assert.Equal("client", version.GetArgument(0));
        Assert.Equal(ProtocolConstants.ApplicationVersion, version.GetArgument(1));
        await link.DisconnectAsync();
    }

    [Fact]
    public async Task Connect_exposes_ready_state_only_after_version_acceptance_and_publishes_news()
    {
        var client = new FakeMessageClient { RespondToVersion = false };
        await using var link = Build(client);
        var states = new List<ConnectionState>();
        string? news = null;
        link.StateChanged += (_, state) => states.Add(state);
        link.NewsReceived += (_, value) => news = value;

        var connecting = link.ConnectAsync();

        Assert.Equal(ConnectionState.Connecting, link.State);
        Assert.Equal(MasterLoginResult.NotConnected, await link.LoginAsync("user", "password"));
        client.Raise(new NetworkMessage(MessageType.VersionAccepted, "Welcome|Back"));
        await connecting;

        Assert.Equal(ConnectionState.Connected, link.State);
        Assert.Equal("Welcome\nBack", news);
        Assert.Contains(ConnectionState.Connected, states);
    }

    [Fact]
    public async Task Account_banned_during_version_check_is_treated_as_version_rejection()
    {
        var client = new FakeMessageClient
        {
            VersionResponse = NetworkMessage.Create(MessageType.AccountBanned),
        };
        await using var link = Build(client);
        var rejected = false;
        link.VersionRejected += (_, _) => rejected = true;

        await link.ConnectAsync();

        Assert.True(rejected);
        Assert.Equal(ConnectionState.Disconnected, link.State);
    }

    [Fact]
    public async Task Login_snapshot_retains_server_list_for_late_consumers()
    {
        var client = new FakeMessageClient();
        client.Response = message => message.Type switch
        {
            MessageType.VersionCheck => NetworkMessage.Create(MessageType.VersionAccepted),
            MessageType.MasterLogin => new NetworkMessage(
                MessageType.LoginGranted,
                "user", "news", "1",
                "0", "Test Server", "game.example", "6541", "Description", "", "no",
                "0"),
            _ => null,
        };
        await using var link = Build(client);
        await link.ConnectAsync();

        var result = await link.LoginAsync("user", "password");

        Assert.Equal(MasterLoginResult.Granted, result);
        var server = Assert.Single(link.ServerListings);
        Assert.Equal("Test Server", server.Name);
        Assert.Equal("game.example", server.Host);
        Assert.Equal(6541, server.Port);
    }

    private static AuthServerLink Build(FakeMessageClient client) => new(
        client,
        Options.Create(new ClientSettings()),
        new ClientSession(),
        NullLogger<AuthServerLink>.Instance);

    private sealed class FakeMessageClient : IMessageClient
    {
        public string ConnectedHost { get; private set; } = string.Empty;
        public int ConnectedPort { get; private set; }
        public List<NetworkMessage> Sent { get; } = [];
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public bool RespondToVersion { get; init; } = true;
        public NetworkMessage VersionResponse { get; init; } = NetworkMessage.Create(MessageType.VersionAccepted);
        public Func<NetworkMessage, NetworkMessage?>? Response { get; set; }

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

        public Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            ConnectedHost = host;
            ConnectedPort = port;
            State = ConnectionState.Connected;
            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(State));
            return Task.CompletedTask;
        }

        public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            var response = Response?.Invoke(message);
            if (response is not null)
            {
                Raise(response);
            }
            else if (RespondToVersion && message.Type == MessageType.VersionCheck)
            {
                Raise(VersionResponse);
            }
            return Task.CompletedTask;
        }

        public void Raise(NetworkMessage message) => MessageReceived?.Invoke(
            this,
            new MessageReceivedEventArgs(string.Empty, message));

        public Task DisconnectAsync()
        {
            State = ConnectionState.Disconnected;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
