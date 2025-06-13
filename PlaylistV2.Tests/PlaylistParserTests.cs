namespace VS.TestPlaylistTools.PlaylistV2.Tests;

/// <summary>
/// Tests for parsing existing playlist XML files
/// </summary>
public class PlaylistParserTests
{
    [Fact]
    public void ParseFromString_Sample1_ParsesCorrectly()
    {
        // Arrange
        string xml = File.ReadAllText(Path.Join(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists", "Sample1.playlist"));

        // Act
        PlaylistRoot playlist = PlaylistV2Parser.FromString(xml);

        // Assert
        Assert.NotNull(playlist);
        Assert.Equal("2.0", playlist.Version);
        Assert.Single(playlist.Rules);

        BooleanRule includeRule = Assert.IsType<BooleanRule>(playlist.Rules[0]);
        Assert.Equal("Includes", includeRule.Name);
        Assert.Equal(BooleanRuleKind.Any, includeRule.Match);
    }

    [Fact]
    public void ParseFromString_Sample2_ParsesCorrectly()
    {
        // Arrange
        string xml = File.ReadAllText(Path.Join(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists", "Sample2.playlist"));

        // Act
        PlaylistRoot playlist = PlaylistV2Parser.FromString(xml);

        // Assert
        Assert.NotNull(playlist);
        Assert.Equal("2.0", playlist.Version);
        Assert.Single(playlist.Rules);

        BooleanRule includeRule = Assert.IsType<BooleanRule>(playlist.Rules[0]);
        Assert.Equal("Includes", includeRule.Name);
        Assert.Equal(BooleanRuleKind.Any, includeRule.Match);
    }

    [Fact]
    public void ParseFromString_V1Version_ThrowsNotSupportedException()
    {
        // Arrange
        string xml = "<Playlist Version=\"1.0\"><Add Test=\"Test1\" /></Playlist>";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => PlaylistV2Parser.FromString(xml));
    }

    [Fact]
    public void ParseFromString_InvalidXml_ThrowsException()
    {
        // Arrange
        string xml = "<NotPlaylist>Invalid</NotPlaylist>";

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => PlaylistV2Parser.FromString(xml));
    }
}