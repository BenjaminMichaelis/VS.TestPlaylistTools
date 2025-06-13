using System.Xml;

namespace VS.TestPlaylistTools.PlaylistV2.Tests;

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
        string projectName = "PlaylistV2.Tests";
        string namespaceName = "PlaylistV2.Tests";
        string className = "PlaylistIntegrationTests";
        string testName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";
        string displayName = "PlaylistV2.Tests.PlaylistIntegrationTests.CreateHierarchicalPlaylist_WithProjectNamespaceClass_GeneratesCorrectXml";

        // Act
        PlaylistRoot playlist = PlaylistV2Builder.CreateHierarchicalPlaylist(
            projectName, namespaceName, className, testName, displayName);
        string generatedXml = playlist.ToString();

        // Assert - Generated XML should be valid and parseable
        Assert.NotNull(generatedXml);
        PlaylistRoot reparsedPlaylist = PlaylistV2Parser.FromString(generatedXml);
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
        string testResourcesPath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists");
        string[] sampleFiles = Directory.GetFiles(testResourcesPath, "*.playlist");
        TheoryData<string> theoryData = new TheoryData<string>();

        foreach (string fileName in sampleFiles)
        {
            string filePath = fileName;
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
        string originalContent = File.ReadAllText(filePath);

        // Parse original
        PlaylistRoot playlist = PlaylistV2Parser.FromString(originalContent);
        Assert.Equal("2.0", playlist.Version);

        // Serialize back to XML
        string regeneratedXml = playlist.ToString();

        // Parse regenerated XML
        PlaylistRoot reparsedPlaylist = PlaylistV2Parser.FromString(regeneratedXml);
        Assert.Equal("2.0", reparsedPlaylist.Version);

        // Compare normalized XML
        XmlDocument originalDoc = new();
        StringReader streamReader = new StringReader(originalContent);
        XmlReader xmlReader = XmlReader.Create(streamReader, new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true });
        originalDoc.Load(xmlReader);
        originalDoc.PreserveWhitespace = false;
        originalDoc.Normalize();

        XmlDocument regeneratedDoc = new();
        regeneratedDoc.LoadXml(regeneratedXml);
        regeneratedDoc.PreserveWhitespace = false;
        regeneratedDoc.Normalize();
        Assert.Equal(originalDoc.OuterXml, regeneratedDoc.OuterXml);

        Assert.Equal(playlist.Version, reparsedPlaylist.Version);
        Assert.Equal(playlist.Rules.Count, reparsedPlaylist.Rules.Count);
    }
}