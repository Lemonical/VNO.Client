using System;
using System.IO;
using VNO.Core.Networking;
using VNO.Client.Services;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers loading the client settings from the legacy data folder ini files
/// </summary>
public sealed class ClientSettingsLoaderTests : IDisposable
{
    private readonly string _baseDirectory =
        Path.Combine(Path.GetTempPath(), "vno-client-loader-" + Path.GetRandomFileName());

    private string DataDirectory => Path.Combine(_baseDirectory, "data");

    public ClientSettingsLoaderTests() => Directory.CreateDirectory(DataDirectory);

    public void Dispose()
    {
        if (Directory.Exists(_baseDirectory))
        {
            Directory.Delete(_baseDirectory, recursive: true);
        }
    }

    private void WriteData(string fileName, string content) =>
        File.WriteAllText(Path.Combine(DataDirectory, fileName), content);

    [Fact]
    public void Missing_files_fall_back_to_the_built_in_defaults()
    {
        var settings = ClientSettingsLoader.Load(_baseDirectory);

        Assert.Equal("Player", settings.DisplayName);
        Assert.Equal("vno-master-rjrun.ondigitalocean.app", MasterServerEndpoint.Host);
        Assert.Equal(443, MasterServerEndpoint.Port);
        Assert.Equal(Transport.WebSocket, MasterServerEndpoint.Transport);
        Assert.True(MasterServerEndpoint.UseTls);
        Assert.Equal(16789, settings.GameServerPort);
        Assert.Equal(DiscordPresenceMode.Off, settings.DiscordPresence);
        Assert.Empty(settings.DiscordApplicationId);
    }

    [Fact]
    public void Reads_discord_privacy_and_public_application_id()
    {
        WriteData(
            "settings.ini",
            "[Discord]\npresence=PublicServerAndPlayerCount\napplication_id=123456789\n");

        var settings = ClientSettingsLoader.Load(_baseDirectory);

        Assert.Equal(DiscordPresenceMode.PublicServerAndPlayerCount, settings.DiscordPresence);
        Assert.Equal("123456789", settings.DiscordApplicationId);
    }

    [Fact]
    public void Reads_the_saved_identity_from_settings_ini()
    {
        WriteData("settings.ini", "[User]\nuser=Phoenix\n");

        var settings = ClientSettingsLoader.Load(_baseDirectory);

        Assert.Equal("Phoenix", settings.DisplayName);
    }

}
