using System.Xml;

namespace VS.TestPlaylistTools.PlaylistV1.Tests
{
    public class PlaylistV1IntegrationTests
    {
        public static TheoryData<string> SamplePlaylistFiles()
        {
            string testResourcesPath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV1.Tests", "SamplePlaylists");
            // Get all files in testResourcesPath that are .playlist files
            string[] sampleFiles = Directory.GetFiles(testResourcesPath, "*.playlist");
            TheoryData<string> theoryData = new TheoryData<string>();
            foreach (string fileName in sampleFiles)
            {
                theoryData.Add(fileName);
            }
            return theoryData;
        }

        [Theory]
        [MemberData(nameof(SamplePlaylistFiles))]
        public void TestRoundTripConversion_FromPlaylistFile(string filePath)
        {
            string originalContent = File.ReadAllText(filePath);

            // Parse original
            PlaylistRoot playlist = PlaylistV1Parser.FromString(originalContent);
            Assert.Equal("1.0", playlist.Version);

            // Serialize back to XML
            string regeneratedXml = playlist.ToString();

            // Parse regenerated XML
            PlaylistRoot reparsedPlaylist = PlaylistV1Parser.FromString(regeneratedXml);
            Assert.Equal("1.0", reparsedPlaylist.Version);

            XmlDocument originalDoc = new();
            StringReader streamReader = new StringReader(originalContent);
            XmlReader xmlReader = XmlReader.Create(streamReader, new XmlReaderSettings { IgnoreWhitespace = true, IgnoreComments = true });
            originalDoc.Load(xmlReader);
            originalDoc.PreserveWhitespace = false;
            originalDoc.Normalize();

            XmlDocument rebuiltDoc = new();
            rebuiltDoc.LoadXml(regeneratedXml);
            rebuiltDoc.PreserveWhitespace = false;
            rebuiltDoc.Normalize();
            Assert.Equal(originalDoc.OuterXml, rebuiltDoc.OuterXml);

            Assert.Equal(playlist.Version, reparsedPlaylist.Version);
            Assert.Equal(playlist.TestCount, reparsedPlaylist.TestCount);
            string?[] originalTests = playlist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            string?[] reparsedTests = reparsedPlaylist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            Assert.Equal(originalTests, reparsedTests);
        }
    }
}