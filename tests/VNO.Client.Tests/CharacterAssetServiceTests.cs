using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VNO.Client.Services;
using Xunit;

namespace VNO.Client.Tests;

/// <summary>
/// Covers loading a character folder into emotes and a show name
/// </summary>
public sealed class CharacterAssetServiceTests : System.IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vno-assets-" + Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private CharacterAssetService Build()
    {
        var settings = Options.Create(new ClientSettings { DataDirectory = _root });
        return new CharacterAssetService(settings, NullLogger<CharacterAssetService>.Instance);
    }

    private string MakeCharacter(string name)
    {
        var folder = Path.Combine(_root, "characters", name);
        Directory.CreateDirectory(Path.Combine(folder, "buttons"));
        File.WriteAllText(Path.Combine(folder, "char.ini"),
            "[Options]\nname = " + name + "\nshowname = Archer\n");
        File.WriteAllText(Path.Combine(folder, "Default.ini"),
            "[Emotions]\nnumber = 2\n1 = StandNorm\n2 = FaceGlare\n");
        return folder;
    }

    [Fact]
    public void Loads_showname_and_ordered_emotes()
    {
        MakeCharacter("Servant Archer");
        var service = Build();

        var character = service.LoadCharacter("Servant Archer");

        Assert.NotNull(character);
        Assert.Equal("Archer", character!.ShowName);
        Assert.Equal(2, character.Emotes.Count);
        Assert.Equal("StandNorm", character.Emotes[0].Name);
        Assert.Equal("FaceGlare", character.Emotes[1].Name);
    }

    [Fact]
    public void Missing_character_returns_null()
    {
        var service = Build();
        Assert.Null(service.LoadCharacter("Nobody"));
    }

    [Fact]
    public void Showname_falls_back_to_folder_name_when_absent()
    {
        var folder = Path.Combine(_root, "characters", "Plain");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "char.ini"), "[Options]\nname = Plain\n");
        File.WriteAllText(Path.Combine(folder, "Default.ini"), "[Emotions]\nnumber = 0\n");
        var service = Build();

        var character = service.LoadCharacter("Plain");

        Assert.NotNull(character);
        Assert.Equal("Plain", character!.ShowName);
        Assert.Empty(character.Emotes);
    }
}
