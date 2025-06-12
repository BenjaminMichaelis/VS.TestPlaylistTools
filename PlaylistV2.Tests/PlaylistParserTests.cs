using System;
using System.IO;
using System.Xml;
using Xunit;

namespace PlaylistV2.Tests;

/// <summary>
/// Tests for parsing existing playlist XML files
/// </summary>
public class PlaylistParserTests
{
    [Fact]
    public void ParseFromString_Sample1_ParsesCorrectly()
    {
        // Arrange
        var xml = File.ReadAllText(Path.Join(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists", "Sample1.playlist"));

        // Act
        var playlist = PlaylistV2Parser.FromString(xml);

        // Assert
        Assert.NotNull(playlist);
        Assert.Equal("2.0", playlist.Version);
        Assert.Single(playlist.Rules);
        
        var includeRule = Assert.IsType<BooleanRule>(playlist.Rules[0]);
        Assert.Equal("Includes", includeRule.Name);
        Assert.Equal(BooleanRuleKind.Any, includeRule.Match);
    }

    [Fact]
    public void ParseFromString_Sample2_ParsesCorrectly()
    {
        // Arrange
        var xml = File.ReadAllText(Path.Join(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists", "Sample2.playlist"));

        // Act
        var playlist = PlaylistV2Parser.FromString(xml);

        // Assert
        Assert.NotNull(playlist);
        Assert.Equal("2.0", playlist.Version);
        Assert.Single(playlist.Rules);
        
        var includeRule = Assert.IsType<BooleanRule>(playlist.Rules[0]);
        Assert.Equal("Includes", includeRule.Name);
        Assert.Equal(BooleanRuleKind.Any, includeRule.Match);
    }

    [Fact]
    public void ParseFromString_V1Version_ThrowsNotSupportedException()
    {
        // Arrange
        var xml = "<Playlist Version=\"1.0\"><Add Test=\"Test1\" /></Playlist>";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PlaylistV2Parser.FromString(xml));
    }

    [Fact]
    public void ParseFromString_InvalidXml_ThrowsException()
    {
        // Arrange
        var xml = "<NotPlaylist>Invalid</NotPlaylist>";

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => PlaylistV2Parser.FromString(xml));
    }
}