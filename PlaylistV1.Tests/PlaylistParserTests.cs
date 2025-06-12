namespace VS.TestPlaylistTools.PlaylistV1.Tests
{
    public class PlaylistParserTests
    {
        [Fact]
        public void ParseFromString_UnsupportedVersion_ThrowsException()
        {
            var xmlContent = """
                    <Playlist Version="2.0">
                        <Add Test="Test1" />
                    </Playlist>
                    """;
            Assert.Throws<InvalidOperationException>(() => PlaylistV1Parser.FromString(xmlContent));
        }
    }
}