using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PlaylistV1;
using Xunit;
using System.Xml;

namespace PlaylistV1.Tests
{
    public class PlaylistV1Tests
    {
        [Fact]
        public void TestBuilder_BasicAndFluentAndXmlAndValidation()
        {
            // Test basic builder
            var playlist1 = PlaylistV1Builder.Create("Test1", "Test2", "Test3");
            Assert.Equal(3, playlist1.TestCount);

            // Test fluent builder
            var playlist2 = PlaylistV1Builder.CreateBuilder()
                .AddTest("FluentTest1")
                .AddTest("FluentTest2")
                .AddTests("FluentTest3", "FluentTest4")
                .Build();
            Assert.Equal(4, playlist2.TestCount);

            // Test XML generation
            var xmlContent = PlaylistV1Builder.ToXmlString(playlist2);
            Assert.Contains("FluentTest1", xmlContent);
            Assert.Contains("Version=\"1.0\"", xmlContent);

            // Test validation methods
            Assert.True(PlaylistV1Parser.IsValidV1Playlist(xmlContent));
        }

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
            var playlist = PlaylistV1Parser.ParseFromString(originalContent);
            var rebuiltContent = PlaylistV1Builder.ToXmlString(playlist);

            XmlDocument originalDoc = new XmlDocument();
            originalDoc.LoadXml(originalContent);
            originalDoc.PreserveWhitespace = false;
            originalDoc.Normalize();

            XmlDocument rebuiltDoc = new XmlDocument();
            rebuiltDoc.LoadXml(rebuiltContent);
            rebuiltDoc.PreserveWhitespace = false;
            rebuiltDoc.Normalize();
            Assert.Equal(originalDoc.OuterXml, rebuiltDoc.OuterXml);

            var reparsedPlaylist = PlaylistV1Parser.ParseFromString(rebuiltContent);
            Assert.Equal(playlist.TestCount, reparsedPlaylist.TestCount);
            var originalTests = playlist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            var reparsedTests = reparsedPlaylist.Tests.Select(t => t.Test).OrderBy(t => t).ToArray();
            Assert.Equal(originalTests, reparsedTests);
        }

        [Fact]
        public void ParseFromString_UnsupportedVersion_ThrowsException()
        {
            var xmlContent = """
                    <Playlist Version="2.0">
                        <Add Test="Test1" />
                    </Playlist>
                    """;
            Assert.Throws<NotSupportedException>(() => PlaylistV1Parser.ParseFromString(xmlContent));
        }
    }
}