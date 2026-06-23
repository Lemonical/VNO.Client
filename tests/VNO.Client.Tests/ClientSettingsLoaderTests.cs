using System;
using System.IO;
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
        Assert.Equal("127.0.0.1", settings.AuthServerHost);
        Assert.Equal(6543, settings.AuthServerPort);
        Assert.Equal(16789, settings.GameServerPort);
    }

    [Fact]
    public void Reads_the_saved_identity_from_settings_ini()
    {
        WriteData("settings.ini", "[User]\nuser=Phoenix\n");

        var settings = ClientSettingsLoader.Load(_baseDirectory);

        Assert.Equal("Phoenix", settings.DisplayName);
    }

    [Fact]
    public void Reads_the_primary_auth_server_from_AS_ini_with_optional_port()
    {
        WriteData("AS.ini", "[AS]\n1=auth.example:7000\n2=backup.example\n");

        var settings = ClientSettingsLoader.Load(_baseDirectory);

        Assert.Equal("auth.example", settings.AuthServerHost);
        Assert.Equal(7000, settings.AuthServerPort);
    }

    [Fact]
    public void A_bare_auth_address_keeps_the_default_port()
    {
        WriteData("AS.ini", "[AS]\n1=auth.example\n");

        var settings = ClientSettingsLoader.Load(_baseDirectory);

        Assert.Equal("auth.example", settings.AuthServerHost);
        Assert.Equal(6543, settings.AuthServerPort);
    }
}
