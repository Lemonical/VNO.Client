using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VNO.Client.Services;
using VNO.Core.Models;
using VNO.Core.Networking;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers the message types the server connection puts on the wire
/// </summary>
/// <remarks>
/// Guards the regression where in character lines were sent as OutOfCharacter,
/// so both feeds carried the same type
/// </remarks>
public sealed class ServerConnectionTests
{
    private sealed class FakeMessageClient : IMessageClient
    {
        public List<NetworkMessage> Sent { get; } = new();
        public Action<NetworkMessage>? OnSend { get; set; }
        public ConnectionState State => ConnectionState.Connected;
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

        public Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            OnSend?.Invoke(message);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync() => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void RaiseMessage(NetworkMessage message) =>
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(string.Empty, message));

        // keep the compiler from warning about the unused event
        public void RaiseState() => StateChanged?.Invoke(this, null!);
    }

    private static ServerConnection Build(FakeMessageClient client) =>
        new(client, Options.Create(new ClientSettings()));

    [Fact]
    public async Task InCharacter_send_uses_the_in_character_type_with_name_and_text()
    {
        var client = new FakeMessageClient();
        var connection = Build(client);

        await connection.SendInCharacterAsync("Phoenix", "Objection!");

        var message = Assert.Single(client.Sent);
        Assert.Equal(MessageType.InCharacter, message.Type);
        Assert.Equal("Phoenix", message.GetArgument(0));
        Assert.Equal("Objection!", message.GetArgument(1));
    }

    [Fact]
    public async Task Chat_send_uses_the_out_of_character_type()
    {
        var client = new FakeMessageClient();
        var connection = Build(client);

        await connection.SendChatAsync("hello room");

        var message = Assert.Single(client.Sent);
        Assert.Equal(MessageType.OutOfCharacter, message.Type);
        Assert.Equal("hello room", message.GetArgument(0));
    }

    [Fact]
    public async Task Connect_performs_version_check_then_single_use_login_before_completing()
    {
        var client = new FakeMessageClient();
        client.OnSend = message =>
        {
            if (message.Type == MessageType.VersionCheck)
            {
                client.RaiseMessage(NetworkMessage.Create(MessageType.VersionAccepted));
            }
            else if (message.Type == MessageType.Login)
            {
                client.RaiseMessage(new NetworkMessage(
                    MessageType.JoinSnapshot,
                    "1", "Court", "0", "1", "Phoenix"));
            }
        };
        await using var connection = Build(client);

        await connection.ConnectAsync("one-use-token", "game.example", 6541);

        Assert.Collection(
            client.Sent,
            message =>
            {
                Assert.Equal(MessageType.VersionCheck, message.Type);
                Assert.Equal("client", message.GetArgument(0));
                Assert.Equal(ProtocolConstants.ClientVersion, message.GetArgument(1));
            },
            message =>
            {
                Assert.Equal(MessageType.Login, message.Type);
                Assert.Equal("one-use-token", message.GetArgument(0));
            });
    }

    [Fact]
    public void Join_snapshot_expands_to_existing_definition_events()
    {
        var client = new FakeMessageClient();
        var connection = Build(client);
        var received = new List<NetworkMessage>();
        connection.MessageReceived += (_, message) => received.Add(message);

        client.RaiseMessage(new NetworkMessage(
            MessageType.JoinSnapshot,
            "2", "Court", "Lobby",
            "1", "Cornered.mp3",
            "2", "Phoenix", "Maya"));

        Assert.Collection(
            received,
            message => Assert.Equal(new[] { "Court", "Lobby" }, message.Arguments),
            message => Assert.Equal(new[] { "Cornered.mp3" }, message.Arguments),
            message => Assert.Equal(new[] { "Phoenix", "Maya" }, message.Arguments),
            message => Assert.Empty(message.Arguments));
    }
}
