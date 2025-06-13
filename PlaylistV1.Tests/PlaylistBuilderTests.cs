namespace VS.TestPlaylistTools.PlaylistV1.Tests
{
    public class PlaylistBuilderTests
    {
        [Fact]
        public void TestBuilder_BasicAndFluentAndXmlAndValidation()
        {
            // Test basic builder
            PlaylistRoot playlist1 = PlaylistV1Builder.Create("Test1", "Test2", "Test3");
            Assert.Equal(3, playlist1.TestCount);

            // Test fluent builder
            PlaylistRoot playlist2 = PlaylistV1Builder.CreateBuilder()
                .AddTest("FluentTest1")
                .AddTest("FluentTest2")
                .AddTests("FluentTest3", "FluentTest4")
                .Build();
            Assert.Equal(4, playlist2.TestCount);

            // Test XML generation
            string xmlContent = PlaylistV1Builder.ToXmlString(playlist2);
            Assert.Contains("FluentTest1", xmlContent);
            Assert.Contains("Version=\"1.0\"", xmlContent);

            // Test validation methods
            Assert.True(PlaylistV1Parser.IsValidPlaylist(xmlContent));
        }
    }
}