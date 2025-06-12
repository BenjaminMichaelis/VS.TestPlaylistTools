namespace VS.TestPlaylistTools.Tests
{
    public class PlaylistLoaderTests
    {
        public static TheoryData<string> SampleV1PlaylistFiles()
        {
            var data = new TheoryData<string>();
            var dir = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV1.Tests", "SamplePlaylists");
            foreach (var file in Directory.GetFiles(dir, "*.playlist"))
            {
                data.Add(file);
            }
            return data;
        }

        public static TheoryData<string> SampleV2PlaylistFiles()
        {
            var data = new TheoryData<string>();
            var dir = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists");
            foreach (var file in Directory.GetFiles(dir, "*.playlist"))
            {
                data.Add(file);
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(SampleV1PlaylistFiles))]
        public void LoadPlaylistV1_ParsesCorrectVersion(string samplePath)
        {
            Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}");

            var result = PlaylistLoader.Load(samplePath);

            Assert.NotNull(result);
            var playlist = Assert.IsType<PlaylistV1.PlaylistRoot>(result);
            Assert.Equal("1.0", playlist.Version);
        }

        [Theory]
        [MemberData(nameof(SampleV2PlaylistFiles))]
        public void LoadPlaylistV2_ParsesCorrectVersion(string samplePath)
        {
            Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}");
            var result = PlaylistLoader.Load(samplePath);

            Assert.NotNull(result);
            var playlist = Assert.IsType<PlaylistV2.PlaylistRoot>(result);
            Assert.Equal("2.0", playlist.Version);
        }
    }
}
