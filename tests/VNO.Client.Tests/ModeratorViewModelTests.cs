using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VNO.Client.Services;
using VNO.Client.ViewModels;
using VNO.Core.Models;
using VNO.Core.Protocol;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers the moderator window's action history logging
/// </summary>
public sealed class ModeratorViewModelTests
{
    private sealed class FakeServer : IServerConnection
    {
        public List<NetworkMessage> Sent { get; } = new();
        public ConnectionState State => ConnectionState.Connected;
        public event EventHandler<NetworkMessage>? MessageReceived;
#pragma warning disable CS0067 // required by the interface, not exercised here
        public event EventHandler<ConnectionState>? StateChanged;
#pragma warning restore CS0067
        public Task ConnectAsync(string displayName, string? host = null, int? port = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
        public Task SendInCharacterAsync(string characterName, string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendChatAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Raise(NetworkMessage m) => MessageReceived?.Invoke(this, m);
    }

    [Fact]
    public async Task Kick_sends_the_message_and_records_history()
    {
        var server = new FakeServer();
        var vm = new ModeratorViewModel(server) { TargetUserId = "7", Reason = "spam" };

        await vm.KickCommand.ExecuteAsync(null);

        var sent = Assert.Single(server.Sent);
        Assert.Equal(MessageType.Kick, sent.Type);
        Assert.Equal("7", sent.GetArgument(0));

        var entry = Assert.Single(vm.History);
        Assert.Contains("player 7", entry);
    }

    [Fact]
    public async Task Missing_target_does_not_send_or_log()
    {
        var server = new FakeServer();
        var vm = new ModeratorViewModel(server) { TargetUserId = string.Empty };

        await vm.MuteCommand.ExecuteAsync(null);

        Assert.Empty(server.Sent);
        Assert.Empty(vm.History);
        Assert.Equal("Enter a player id first", vm.Status);
    }
}
