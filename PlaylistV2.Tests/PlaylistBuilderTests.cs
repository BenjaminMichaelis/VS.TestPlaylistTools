namespace VS.TestPlaylistTools.PlaylistV2.Tests;

/// <summary>
/// Tests for generating playlist XML files
/// </summary>
public class PlaylistBuilderTests
{
    [Fact]
    public void CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml()
    {
        // Arrange
        string projectName = "PlaylistV2.Tests";
        string namespaceName = "PlaylistV2.Tests";
        string className = "PlaylistIntegrationTests";
        string testName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";
        string displayName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";

        // Act
        PlaylistRoot playlist = PlaylistV2Builder.CreateHierarchicalPlaylist(
            projectName, namespaceName, className, testName, displayName);
        string xml = playlist.ToString();

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("Version=\"2.0\"", xml);
        Assert.Contains("Name=\"Includes\"", xml);
        Assert.Contains("Match=\"Any\"", xml);
        Assert.Contains("Match=\"All\"", xml);
        Assert.Contains("Name=\"Solution\"", xml);
        Assert.Contains($"Name=\"Project\" Value=\"{projectName}\"", xml);
        Assert.Contains($"Name=\"Namespace\" Value=\"{namespaceName}\"", xml);
        Assert.Contains($"Name=\"Class\" Value=\"{className}\"", xml);
        Assert.Contains($"Name=\"TestWithNormalizedFullyQualifiedName\" Value=\"{testName}\"", xml);
        Assert.Contains($"Name=\"DisplayName\" Value=\"{displayName}\"", xml);

        // Ensure the generated XML can be parsed back
        PlaylistRoot reparsedPlaylist = PlaylistV2Parser.FromString(xml);
        Assert.NotNull(reparsedPlaylist);
        Assert.Equal("2.0", reparsedPlaylist.Version);
    }

    [Fact]
    public void CreateHierarchicalPlaylist_WithMultipleTests_GeneratesCorrectXml()
    {
        // Arrange
        string projectName = "PlaylistV1.Tests";
        string namespaceName = "PlaylistV1.Tests";
        string className = "PlaylistV1Tests";
        (string, string)[] tests = new[]
        {
            ("PlaylistV1.Tests.PlaylistV1Tests.ParseFromString_UnsupportedVersion_ThrowsException",
             "PlaylistV1.Tests.PlaylistV1Tests.ParseFromString_UnsupportedVersion_ThrowsException"),
            ("PlaylistV1.Tests.PlaylistV1Tests.TestRoundTripConversion_FromPlaylistFile",
             "PlaylistV1.Tests.PlaylistV1Tests.TestRoundTripConversion_FromPlaylistFile(filePath: ")
        };

        // Act
        PlaylistRoot playlist = PlaylistV2Builder.CreateHierarchicalPlaylist(
            projectName, namespaceName, className, tests);
        string xml = playlist.ToString();

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("Version=\"2.0\"", xml);
        Assert.Contains($"Name=\"Project\" Value=\"{projectName}\"", xml);
        Assert.Contains($"Name=\"Namespace\" Value=\"{namespaceName}\"", xml);
        Assert.Contains($"Name=\"Class\" Value=\"{className}\"", xml);

        foreach ((string testName, string displayName) in tests)
        {
            Assert.Contains($"Name=\"TestWithNormalizedFullyQualifiedName\" Value=\"{testName}\"", xml);
            // For display names with special characters, check for the XML-escaped version
            string escapedDisplayName = displayName.Replace("\"", "&quot;").Replace("\\", "\\");
            Assert.Contains($"Name=\"DisplayName\" Value=\"{escapedDisplayName}\"", xml);
        }

        // Ensure the generated XML can be parsed back
        PlaylistRoot reparsedPlaylist = PlaylistV2Parser.FromString(xml);
        Assert.NotNull(reparsedPlaylist);
        Assert.Equal("2.0", reparsedPlaylist.Version);
    }


    [Fact]
    public void Playlist_EmptyPlaylist_CreatesValidXml()
    {
        // Arrange
        PlaylistRoot playlist = new PlaylistV2Builder.Builder().Build();

        // Act
        string xml = playlist.ToString();

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("Version=\"2.0\"", xml);
        Assert.Contains("<Playlist", xml);
        // Empty playlist creates a self-closing tag
        Assert.True(xml.Contains("</Playlist>") || xml.Contains("/>"));

        // Should be parseable
        PlaylistRoot reparsedPlaylist = PlaylistV2Parser.FromString(xml);
        Assert.Equal("2.0", reparsedPlaylist.Version);
        Assert.Empty(reparsedPlaylist.Rules);
    }

    [Fact]
    public void Playlist_WithDirectRules_SerializesCorrectly()
    {
        // Arrange
        PlaylistV2Builder.Builder playlistBuilder = new PlaylistV2Builder.Builder();
        playlistBuilder.AddRule(PropertyRule.Solution());
        playlistBuilder.AddRule(PropertyRule.Project("TestProject"));
        PlaylistRoot playlist = playlistBuilder.Build();

        // Act
        string xml = playlist.ToString();

        // Assert
        Assert.Contains("Name=\"Solution\"", xml);
        Assert.Contains("Name=\"Project\" Value=\"TestProject\"", xml);
        PlaylistRoot reparsedPlaylist = PlaylistV2Parser.FromString(xml);
        Assert.Equal(2, reparsedPlaylist.Rules.Count);
    }
}