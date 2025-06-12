using System;
using System.IO;
using PlaylistV1;
using PlaylistV2;
using VSPlaylistBuilder;
using Xunit;

namespace VSPlaylistBuilder.Tests
{
    public class PlaylistLoaderTests
    {
        public static IEnumerable<object[]> SampleV1PlaylistFiles()
        {
            // Adjust the path as needed for your test data location
            var dir = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV1.Tests", "SamplePlaylists");
            foreach (var file in Directory.GetFiles(dir, "*.playlist"))
            {
                yield return new object[] { file };
            }
        }

        public static IEnumerable<object[]> SampleV2PlaylistFiles()
        {
            var dir = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "PlaylistV2.Tests", "SamplePlaylists");
            foreach (var file in Directory.GetFiles(dir, "*.playlist"))
            {
                yield return new object[] { file};
            }
        }

        [Theory]
        [MemberData(nameof(SampleV1PlaylistFiles))]
        public void LoadPlaylistV1_ParsesCorrectVersion(string samplePath)
        {
            Assert.True(File.Exists(samplePath), $"Sample file not found: {samplePath}");

            var result = PlaylistLoader.Load(samplePath);

            Assert.NotNull(result);
            var playlist = Assert.IsType<PlaylistV1.PlaylistRoot>(result);
            // Assuming result has a Version property
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
            // Assuming result has a Version property
            Assert.Equal("2.0", playlist.Version);
        }
    }
}
