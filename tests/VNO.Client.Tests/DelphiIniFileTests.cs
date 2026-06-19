using System;
using System.IO;
using VNO.Client.Services;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers the legacy ini reader against the quirks of the shipped data files
/// </summary>
public sealed class DelphiIniFileTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".ini");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private DelphiIniFile Load(string content)
    {
        File.WriteAllText(_path, content);
        return DelphiIniFile.Load(_path);
    }

    [Fact]
    public void Reads_values_with_comments_and_leading_whitespace()
    {
        var ini = Load(" [ObjectColor]\nedit_username=$00252525\n\t//comment line\n\\\\other comment\n; also comment\n");

        Assert.Equal("$00252525", ini.ReadString("ObjectColor", "edit_username", "x"));
    }

    [Fact]
    public void Lookups_are_case_insensitive_like_the_windows_api()
    {
        var ini = Load("[DesignStyle]\ndesign = Ace Attorney\n");

        Assert.Equal("Ace Attorney", ini.ReadString("designstyle", "DESIGN", "twewy"));
    }

    [Fact]
    public void First_duplicate_key_wins_like_the_windows_api()
    {
        var ini = Load("[Font]\nedit_password=Tahoma\nedit_password_size=12\nedit_password=Arial\nedit_password_size=10\n");

        Assert.Equal("Tahoma", ini.ReadString("Font", "edit_password", "x"));
        Assert.Equal(12, ini.ReadDouble("Font", "edit_password_size", 0));
    }

    [Fact]
    public void Missing_file_yields_fallbacks()
    {
        var ini = DelphiIniFile.Load(Path.Combine(Path.GetTempPath(), "does-not-exist-vno.ini"));

        Assert.Equal("twewy", ini.ReadString("DesignStyle", "design", "twewy"));
        Assert.Equal(10, ini.ReadInteger("User", "maxHP", 10));
    }

    [Fact]
    public void Fractional_sizes_parse_with_invariant_culture()
    {
        var ini = Load("[Font]\nmemo_ooc_size=9.5\n");

        Assert.Equal(9.5, ini.ReadDouble("Font", "memo_ooc_size", 0));
    }

    [Fact]
    public void WriteValue_updates_in_place_and_preserves_comments()
    {
        File.WriteAllText(_path, "[User]\nenabled=0\nuser=Username\n\n\\\\Ping comment survives\nping=1\n");

        DelphiIniFile.WriteValue(_path, "User", "enabled", "1");
        DelphiIniFile.WriteValue(_path, "User", "pass", "secret");

        var text = File.ReadAllText(_path);
        Assert.Contains("enabled=1", text);
        Assert.Contains("pass=secret", text);
        Assert.Contains(@"\\Ping comment survives", text);
        Assert.DoesNotContain("enabled=0", text);
    }

    [Fact]
    public void WriteValue_appends_missing_section()
    {
        DelphiIniFile.WriteValue(_path, "User", "enabled", "1");

        var ini = DelphiIniFile.Load(_path);
        Assert.Equal(1, ini.ReadInteger("User", "enabled", 0));
    }
}
