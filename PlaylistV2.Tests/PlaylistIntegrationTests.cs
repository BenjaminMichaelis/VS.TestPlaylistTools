using System;
using System.IO;
using System.Xml;
using Xunit;

namespace PlaylistV2.Tests;

/// <summary>
/// Integration tests that validate the complete parsing and generation workflow
/// </summary>
public class PlaylistIntegrationTests
{
    [Fact]
    public void CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml()
    {
        // This test validates the exact scenario from the issue description
        
        // Arrange
        var projectName = "PlaylistV2.Tests";
        var namespaceName = "PlaylistV2.Tests";
        var className = "PlaylistIntegrationTests";
        var testName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";
        var displayName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";

        // Act
        var playlist = PlaylistV2Builder.CreateHierarchicalPlaylist(
            projectName, namespaceName, className, testName, displayName);
        var generatedXml = playlist.ToString();

        // Assert - Generated XML should be valid and parseable
        Assert.NotNull(generatedXml);
        var reparsedPlaylist = PlaylistParser.FromString(generatedXml);
        Assert.Equal("2.0", reparsedPlaylist.Version);

        // The generated XML should have similar structure to Sample1.playlist
        Assert.Contains("Version=\"2.0\"", generatedXml);
        Assert.Contains("Name=\"Includes\"", generatedXml);
        Assert.Contains("Match=\"Any\"", generatedXml);
        Assert.Contains("Match=\"All\"", generatedXml);
        Assert.Contains("Name=\"Solution\"", generatedXml);
        Assert.Contains($"Value=\"{projectName}\"", generatedXml);
        Assert.Contains($"Value=\"{namespaceName}\"", generatedXml);
        Assert.Contains($"Value=\"{className}\"", generatedXml);
        Assert.Contains($"Value=\"{testName}\"", generatedXml);
        Assert.Contains($"Value=\"{displayName}\"", generatedXml);
    }

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
    public void CompareGeneratedWithSample_StructureShouldBeEquivalent(string sampleFilePath)
    {
        // Arrange
        var sampleXml = File.ReadAllText(sampleFilePath);
        var samplePlaylist = PlaylistParser.FromString(sampleXml);

        // Act - Extract structure information from the sample
        Assert.Single(samplePlaylist.Rules);
        var includeRule = Assert.IsType<BooleanRule>(samplePlaylist.Rules[0]);
        Assert.Equal("Includes", includeRule.Name);
        Assert.Equal(BooleanRuleKind.Any, includeRule.Match);

        // The structure should be parseable and re-serializable
        var regeneratedXml = samplePlaylist.ToString();
        var reparsedPlaylist = PlaylistParser.FromString(regeneratedXml);
        
        // Assert
        Assert.Equal(samplePlaylist.Version, reparsedPlaylist.Version);
        Assert.Equal(samplePlaylist.Rules.Count, reparsedPlaylist.Rules.Count);
    }

    [Fact]
    public void Playlist_EmptyPlaylist_CreatesValidXml()
    {
        // Arrange
        var playlist = new PlaylistV2Builder.Builder().Build();

        // Act
        var xml = playlist.ToString();

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("Version=\"2.0\"", xml);
        Assert.Contains("<Playlist", xml);
        // Empty playlist creates a self-closing tag
        Assert.True(xml.Contains("</Playlist>") || xml.Contains("/>"));

        // Should be parseable
        var reparsedPlaylist = PlaylistParser.FromString(xml);
        Assert.Equal("2.0", reparsedPlaylist.Version);
        Assert.Empty(reparsedPlaylist.Rules);
    }

    [Fact]
    public void Playlist_WithDirectRules_SerializesCorrectly()
    {
        // Arrange
        var playlistBuilder = new PlaylistV2Builder.Builder();
        playlistBuilder.AddRule(PropertyRule.Solution());
        playlistBuilder.AddRule(PropertyRule.Project("TestProject"));
        var playlist = playlistBuilder.Build();

        // Act
        var xml = playlist.ToString();

        // Assert
        Assert.Contains("Name=\"Solution\"", xml);
        Assert.Contains("Name=\"Project\" Value=\"TestProject\"", xml);
        var reparsedPlaylist = PlaylistParser.FromString(xml);
        Assert.Equal(2, reparsedPlaylist.Rules.Count);
    }
}