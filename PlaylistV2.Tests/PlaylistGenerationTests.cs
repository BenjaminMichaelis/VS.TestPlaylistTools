using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PlaylistV2.Tests;

/// <summary>
/// Tests for generating playlist XML files
/// </summary>
public class PlaylistGenerationTests
{
    [Fact]
    public void CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml()
    {
        // Arrange
        var projectName = "PlaylistV2.Tests";
        var namespaceName = "PlaylistV2.Tests";
        var className = "PlaylistIntegrationTests";
        var testName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";
        var displayName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";

        // Act
        var playlist = Playlist.CreateHierarchicalPlaylist(
            projectName, namespaceName, className, testName, displayName);
        var xml = playlist.ToString();

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
        var reparsedPlaylist = Playlist.FromString(xml);
        Assert.NotNull(reparsedPlaylist);
        Assert.Equal("2.0", reparsedPlaylist.Root.Version);
    }

    [Fact]
    public void CreateHierarchicalPlaylist_WithMultipleTests_GeneratesCorrectXml()
    {
        // Arrange
        var projectName = "PlaylistV1.Tests";
        var namespaceName = "PlaylistV1.Tests";
        var className = "PlaylistV1Tests";
        var tests = new[]
        {
            ("PlaylistV1.Tests.PlaylistV1Tests.ParseFromString_UnsupportedVersion_ThrowsException",
             "PlaylistV1.Tests.PlaylistV1Tests.ParseFromString_UnsupportedVersion_ThrowsException"),
            ("PlaylistV1.Tests.PlaylistV1Tests.TestRoundTripConversion_FromPlaylistFile",
             "PlaylistV1.Tests.PlaylistV1Tests.TestRoundTripConversion_FromPlaylistFile(filePath: ")
        };

        // Act
        var playlist = Playlist.CreateHierarchicalPlaylist(
            projectName, namespaceName, className, tests);
        var xml = playlist.ToString();

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("Version=\"2.0\"", xml);
        Assert.Contains($"Name=\"Project\" Value=\"{projectName}\"", xml);
        Assert.Contains($"Name=\"Namespace\" Value=\"{namespaceName}\"", xml);
        Assert.Contains($"Name=\"Class\" Value=\"{className}\"", xml);
        
        foreach (var (testName, displayName) in tests)
        {
            Assert.Contains($"Name=\"TestWithNormalizedFullyQualifiedName\" Value=\"{testName}\"", xml);
            // For display names with special characters, check for the XML-escaped version
            var escapedDisplayName = displayName.Replace("\"", "&quot;").Replace("\\", "\\");
            Assert.Contains($"Name=\"DisplayName\" Value=\"{escapedDisplayName}\"", xml);
        }

        // Ensure the generated XML can be parsed back
        var reparsedPlaylist = Playlist.FromString(xml);
        Assert.NotNull(reparsedPlaylist);
        Assert.Equal("2.0", reparsedPlaylist.Root.Version);
    }

    [Fact]
    public void PropertyRule_CreatesCorrectXmlNames()
    {
        // Arrange & Act & Assert
        Assert.Equal("Solution", PropertyRule.Solution().Name);
        Assert.Equal("Project", PropertyRule.Project("TestProject").Name);
        Assert.Equal("TestProject", PropertyRule.Project("TestProject").Value);
        Assert.Equal("Namespace", PropertyRule.Namespace("TestNamespace").Name);
        Assert.Equal("TestNamespace", PropertyRule.Namespace("TestNamespace").Value);
        Assert.Equal("Class", PropertyRule.Class("TestClass").Name);
        Assert.Equal("TestClass", PropertyRule.Class("TestClass").Value);
        Assert.Equal("TestWithNormalizedFullyQualifiedName", PropertyRule.TestWithNormalizedFullyQualifiedName("TestMethod").Name);
        Assert.Equal("TestMethod", PropertyRule.TestWithNormalizedFullyQualifiedName("TestMethod").Value);
        Assert.Equal("DisplayName", PropertyRule.TestWithDisplayName("Test Display Name").Name);
        Assert.Equal("Test Display Name", PropertyRule.TestWithDisplayName("Test Display Name").Value);
    }

    [Fact]
    public void BooleanRule_CreatesCorrectStructure()
    {
        // Arrange
        var rule = BooleanRule.Any("TestRule",
            PropertyRule.Solution(),
            BooleanRule.All(
                PropertyRule.Project("TestProject"),
                PropertyRule.Namespace("TestNamespace")
            )
        );

        // Act
        var playlist = new Playlist();
        playlist.Root.Rules.Add(rule);
        var xml = playlist.ToString();

        // Assert
        Assert.Contains("Name=\"TestRule\"", xml);
        Assert.Contains("Match=\"Any\"", xml);
        Assert.Contains("Match=\"All\"", xml);
        Assert.Contains("Name=\"Solution\"", xml);
        Assert.Contains("Name=\"Project\" Value=\"TestProject\"", xml);
        Assert.Contains("Name=\"Namespace\" Value=\"TestNamespace\"", xml);
    }
}