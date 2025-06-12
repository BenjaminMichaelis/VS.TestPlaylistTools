using System;
using System.IO;
using System.Xml;

using Xunit;

namespace PlaylistV2.Tests;

/// <summary>
/// Tests for parsing existing playlist XML files
/// </summary>
public class PlaylistParsingTests
{
    [Fact]
    public void ParseFromString_Sample1_ParsesCorrectly()
    {
        // Arrange
        var xml = File.ReadAllText(Path.Join(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists", "Sample1.playlist"));

        // Act
        var playlist = Playlist.FromString(xml);

        // Assert
        Assert.NotNull(playlist);
        Assert.Equal("2.0", playlist.Root.Version);
        Assert.Single(playlist.Root.Rules);
        
        var includeRule = Assert.IsType<BooleanRule>(playlist.Root.Rules[0]);
        Assert.Equal("Includes", includeRule.Name);
        Assert.Equal(BooleanRuleKind.Any, includeRule.Match);
    }

    [Fact]
    public void ParseFromString_Sample2_ParsesCorrectly()
    {
        // Arrange
        var xml = File.ReadAllText(Path.Join(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists", "Sample2.playlist"));

        // Act
        var playlist = Playlist.FromString(xml);

        // Assert
        Assert.NotNull(playlist);
        Assert.Equal("2.0", playlist.Root.Version);
        Assert.Single(playlist.Root.Rules);
        
        var includeRule = Assert.IsType<BooleanRule>(playlist.Root.Rules[0]);
        Assert.Equal("Includes", includeRule.Name);
        Assert.Equal(BooleanRuleKind.Any, includeRule.Match);
    }

    [Fact]
    public void ParseFromString_V1Version_ThrowsNotSupportedException()
    {
        // Arrange
        var xml = "<Playlist Version=\"1.0\"><Add Test=\"Test1\" /></Playlist>";

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => Playlist.FromString(xml));
    }

    [Fact]
    public void ParseFromString_InvalidXml_ThrowsException()
    {
        // Arrange
        var xml = "<NotPlaylist>Invalid</NotPlaylist>";

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => Playlist.FromString(xml));
    }

    // Update the return type of SamplePlaylistFiles to use TheoryData<string> for better type safety
    public static TheoryData<string> SamplePlaylistFiles()
    {
        var testResourcesPath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists");
        var sampleFiles = Directory.GetFiles(testResourcesPath, "*.playlist");

        var theoryData = new TheoryData<string>();
        foreach (var fileName in sampleFiles)
        {
            var filePath = fileName;
            if (File.Exists(filePath))
            {
                theoryData.Add(filePath);
            }
        }
        return theoryData;
    }

    [Theory]
    [MemberData(nameof(SamplePlaylistFiles))]
    public void TestRoundTripConversion_FromPlaylistFiles(string filePath)
    {
        // Read original XML
        var originalContent = File.ReadAllText(filePath);

        // Parse original
        var playlist = Playlist.FromString(originalContent);
        Assert.Equal("2.0", playlist.Root.Version);

        // Serialize back to XML
        var regeneratedXml = playlist.ToString();

        // Parse regenerated XML
        var reparsedPlaylist = Playlist.FromString(regeneratedXml);
        Assert.Equal("2.0", reparsedPlaylist.Root.Version);

        // Compare normalized XML
        XmlDocument originalDoc = new XmlDocument();
        originalDoc.LoadXml(originalContent);
        originalDoc.PreserveWhitespace = false;
        originalDoc.Normalize();

        XmlDocument regeneratedDoc = new XmlDocument();
        regeneratedDoc.LoadXml(regeneratedXml);
        regeneratedDoc.PreserveWhitespace = false;
        regeneratedDoc.Normalize();
        Assert.Equal(originalDoc.OuterXml, regeneratedDoc.OuterXml);

        // Compare rule counts
        Assert.Equal(playlist.Root.Rules.Count, reparsedPlaylist.Root.Rules.Count);
    }
}