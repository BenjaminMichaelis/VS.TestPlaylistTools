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
            var playlist = PlaylistV1Parser.FromString(originalContent);
            var rebuiltContent = PlaylistV1Builder.ToXmlString(playlist);

            XmlDocument originalDoc = new();
            originalDoc.LoadXml(originalContent);
            originalDoc.PreserveWhitespace = false;
            originalDoc.Normalize();

            XmlDocument rebuiltDoc = new();
            rebuiltDoc.LoadXml(rebuiltContent);
            rebuiltDoc.PreserveWhitespace = false;
            rebuiltDoc.Normalize();
            Assert.Equal(originalDoc.OuterXml, rebuiltDoc.OuterXml);

            var reparsedPlaylist = PlaylistV1Parser.FromString(rebuiltContent);
            Assert.Equal(playlist.TestCount, reparsedPlaylist.TestCount);
            var originalTests = playlist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            var reparsedTests = reparsedPlaylist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            Assert.Equal(originalTests, reparsedTests);
        }
    }
}