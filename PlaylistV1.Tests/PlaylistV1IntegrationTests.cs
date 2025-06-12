using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PlaylistV1;
using Xunit;
using System.Xml;

namespace PlaylistV1.Tests
{
    public class PlaylistV1IntegrationTests
    {
        public static TheoryData<string> SamplePlaylistFiles()
        {
            var testResourcesPath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV1.Tests", "SamplePlaylists");
            // Get all files in testResourcesPath that are .playlist files
            var sampleFiles = Directory.GetFiles(testResourcesPath, "*.playlist");
            var theoryData = new TheoryData<string>();
            foreach (var fileName in sampleFiles)
            {
                theoryData.Add(fileName);
            }
            return theoryData;
        }

        [Theory]
        [MemberData(nameof(SamplePlaylistFiles))]
        public void TestRoundTripConversion_FromPlaylistFile(string filePath)
        {
            var originalContent = File.ReadAllText(filePath);

            // Parse original
            var playlist = PlaylistV1Parser.FromString(originalContent);
            Assert.Equal("1.0", playlist.Version);

            // Serialize back to XML
            var regeneratedXml = playlist.ToString();

            // Parse regenerated XML
            var reparsedPlaylist = PlaylistV1Parser.FromString(regeneratedXml);
            Assert.Equal("1.0", reparsedPlaylist.Version);

            XmlDocument originalDoc = new();
            var streamReader = new StringReader(originalContent);
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
            var originalTests = playlist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            var reparsedTests = reparsedPlaylist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            Assert.Equal(originalTests, reparsedTests);
        }
    }
}