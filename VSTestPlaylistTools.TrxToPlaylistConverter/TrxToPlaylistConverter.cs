using VS.TestPlaylistTools.PlaylistV1;

namespace VSTestPlaylistTools.TrxToPlaylist
{
    /// <summary>
    /// Provides functionality to convert TRX test result files to Visual Studio Test Playlist format.
    /// </summary>
    public class TrxToPlaylistConverter
    {
        /// <summary>
        /// Converts a TRX file to a Visual Studio Test Playlist.
        /// </summary>
        /// <param name="trxFilePath">Path to the TRX file to convert.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        /// <returns>A PlaylistRoot object containing the filtered tests.</returns>
        public PlaylistRoot ConvertTrxToPlaylist(string trxFilePath, params TRexLib.TestOutcome[] outcomes)
        {
            if (string.IsNullOrEmpty(trxFilePath))
                throw new ArgumentNullException(nameof(trxFilePath));

            if (!File.Exists(trxFilePath))
                throw new FileNotFoundException($"TRX file not found: {trxFilePath}", trxFilePath);

            // Parse the TRX file using TRexLib
            TRexLib.TestResultSet testRun = TRexLib.TestOutputDocumentParser.Parse(new FileInfo(trxFilePath));

            // Get the results
            List<TRexLib.TestResult> allResults = [.. testRun];

            // Filter by outcome if specified
            IEnumerable<TRexLib.TestResult> filteredResults = allResults;
            if (outcomes != null && outcomes.Length > 0)
            {
                filteredResults = allResults.Where(r => outcomes.Contains(r.Outcome));
            }

            // Extract test names
            List<string> testNames = filteredResults.Select(r => r.TestName).ToList();

            // Create a playlist
            return PlaylistV1Builder.Create(testNames);
        }

        /// <summary>
        /// Converts a TRX file to a Visual Studio Test Playlist and saves it to the specified file path.
        /// </summary>
        /// <param name="trxFilePath">Path to the TRX file to convert.</param>
        /// <param name="playlistFilePath">Path where the playlist file should be saved.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        public void ConvertTrxToPlaylistFile(string trxFilePath, string playlistFilePath, params TRexLib.TestOutcome[] outcomes)
        {
            if (string.IsNullOrEmpty(playlistFilePath))
                throw new ArgumentNullException(nameof(playlistFilePath));

            PlaylistRoot playlist = ConvertTrxToPlaylist(trxFilePath, outcomes);
            PlaylistV1Builder.SaveToFile(playlist, playlistFilePath);
        }

        /// <summary>
        /// Converts a TRX file to a Visual Studio Test Playlist XML string.
        /// </summary>
        /// <param name="trxFilePath">Path to the TRX file to convert.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        /// <returns>An XML string representation of the playlist.</returns>
        public string ConvertTrxToPlaylistXml(string trxFilePath, params TRexLib.TestOutcome[] outcomes)
        {
            PlaylistRoot playlist = ConvertTrxToPlaylist(trxFilePath, outcomes);
            return PlaylistV1Builder.ToXmlString(playlist);
        }
    }
}
