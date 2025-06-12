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
            Assert.True(PlaylistV1Parser.IsValidPlaylist(xmlContent));
        }

        [Fact]
        public void ParseFromString_UnsupportedVersion_ThrowsException()
        {
            var xmlContent = """
                    <Playlist Version="2.0">
                        <Add Test="Test1" />
                    </Playlist>
                    """;
            Assert.Throws<NotSupportedException>(() => PlaylistV1Parser.FromString(xmlContent));
        }
    }
}