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
        public ConnectionState State => ConnectionState.Connected;
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

        public Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync() => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        // keep the compiler from warning about the unused events
        public void Raise() { MessageReceived?.Invoke(this, null!); StateChanged?.Invoke(this, null!); }
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
}
