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

    [Fact]
    public void IdIgnore_without_a_target_does_not_ignore()
    {
        var server = new FakeServer();
        var vm = new ModeratorViewModel(server) { TargetUserId = string.Empty };

        vm.IdIgnoreCommand.Execute(null);

        Assert.False(vm.IsIgnoring("anyone"));
        Assert.Equal("Select a player first", vm.Status);
    }

    [Fact]
    public void IdIgnore_toggles_the_selected_name_on_and_off()
    {
        var server = new FakeServer();
        var vm = new ModeratorViewModel(server) { TargetUserId = "Phoenix" };

        vm.IdIgnoreCommand.Execute(null);
        Assert.True(vm.IsIgnoring("phoenix")); // case insensitive
        Assert.Contains("Ignoring Phoenix", vm.Status);

        vm.IdIgnoreCommand.Execute(null);
        Assert.False(vm.IsIgnoring("Phoenix"));
        Assert.Contains("No longer ignoring", vm.Status);
    }

    [Fact]
    public void IdIgnore_purges_already_shown_lines_from_that_name()
    {
        var server = new FakeServer();
        var vm = new ModeratorViewModel(server) { TargetUserId = "Edgeworth" };
        vm.ChatFeed.Add("Edgeworth: Objection");
        vm.ChatFeed.Add("Phoenix: Hold it");
        vm.ChatFeed.Add("(OOC) hi");

        vm.IdIgnoreCommand.Execute(null);

        Assert.DoesNotContain(vm.ChatFeed, line => line.StartsWith("Edgeworth: ", System.StringComparison.Ordinal));
        Assert.Contains("Phoenix: Hold it", vm.ChatFeed);
        Assert.Contains("(OOC) hi", vm.ChatFeed);
    }
}
