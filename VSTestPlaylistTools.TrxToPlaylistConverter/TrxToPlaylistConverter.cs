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
        public PlaylistRoot ConvertTrxToPlaylist(string trxFilePath, params TrxLib.TestOutcome[] outcomes)
        {
            if (string.IsNullOrEmpty(trxFilePath))
                throw new ArgumentNullException(nameof(trxFilePath));

            if (!File.Exists(trxFilePath))
                throw new FileNotFoundException($"TRX file not found: {trxFilePath}", trxFilePath);

            // Parse the TRX file using TrxLib
            var testRun = TrxLib.TrxParser.Parse(new FileInfo(trxFilePath));

            // Get the results
            List<TrxLib.TestResult> allResults = [.. testRun];

            // Filter by outcome if specified
            IEnumerable<TrxLib.TestResult> filteredResults = allResults;
            if (outcomes != null && outcomes.Length > 0)
            {
                filteredResults = allResults.Where(r => outcomes.Contains(r.Outcome));
            }

            // Extract test names
            List<string> testNames = filteredResults.Select(r => r.FullyQualifiedTestName).ToList();

            // Create a playlist
            return PlaylistV1Builder.Create(testNames);
        }

        /// <summary>
        /// Converts multiple TRX files to a single Visual Studio Test Playlist with de-duplicated tests.
        /// </summary>
        /// <param name="trxFilePaths">Paths to the TRX files to convert.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        /// <returns>A PlaylistRoot object containing the filtered and de-duplicated tests.</returns>
        public PlaylistRoot ConvertMultipleTrxToPlaylist(IEnumerable<string> trxFilePaths, params TrxLib.TestOutcome[] outcomes)
        {
            ArgumentNullException.ThrowIfNull(trxFilePaths);

            string[] filesArray = trxFilePaths.ToArray();
            if (filesArray.Length == 0)
                throw new ArgumentException("At least one TRX file path must be specified.", nameof(trxFilePaths));

            // Use a HashSet to automatically de-duplicate test names
            HashSet<string> uniqueTestNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (string trxFilePath in filesArray)
            {
                if (string.IsNullOrEmpty(trxFilePath))
                    throw new ArgumentException("TRX file path cannot be null or empty.", nameof(trxFilePaths));

                if (!File.Exists(trxFilePath))
                    throw new FileNotFoundException($"TRX file not found: {trxFilePath}", trxFilePath);

                // Parse the TRX file using TrxLib
                var testRun = TrxLib.TrxParser.Parse(new FileInfo(trxFilePath));

                // Get the results
                List<TrxLib.TestResult> allResults = [.. testRun];

                // Filter by outcome if specified
                IEnumerable<TrxLib.TestResult> filteredResults = allResults;
                if (outcomes != null && outcomes.Length > 0)
                {
                    filteredResults = allResults.Where(r => outcomes.Contains(r.Outcome));
                }

                // Add test names to the set (duplicates are automatically ignored)
                foreach (var result in filteredResults)
                {
                    uniqueTestNames.Add(result.FullyQualifiedTestName);
                }
            }

            // Create a playlist with the de-duplicated test names
            return PlaylistV1Builder.Create(uniqueTestNames.ToList());
        }

        /// <summary>
        /// Converts a TRX file to a Visual Studio Test Playlist and saves it to the specified file path.
        /// </summary>
        /// <param name="trxFilePath">Path to the TRX file to convert.</param>
        /// <param name="playlistFilePath">Path where the playlist file should be saved.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        public void ConvertTrxToPlaylistFile(string trxFilePath, string playlistFilePath, params TrxLib.TestOutcome[] outcomes)
        {
            if (string.IsNullOrEmpty(playlistFilePath))
                throw new ArgumentNullException(nameof(playlistFilePath));

            PlaylistRoot playlist = ConvertTrxToPlaylist(trxFilePath, outcomes);
            PlaylistV1Builder.SaveToFile(playlist, playlistFilePath);
        }

        /// <summary>
        /// Converts multiple TRX files to a single Visual Studio Test Playlist file with de-duplicated tests.
        /// </summary>
        /// <param name="trxFilePaths">Paths to the TRX files to convert.</param>
        /// <param name="playlistFilePath">Path where the playlist file should be saved.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        public void ConvertMultipleTrxToPlaylistFile(IEnumerable<string> trxFilePaths, string playlistFilePath, params TrxLib.TestOutcome[] outcomes)
        {
            if (string.IsNullOrEmpty(playlistFilePath))
                throw new ArgumentNullException(nameof(playlistFilePath));

            PlaylistRoot playlist = ConvertMultipleTrxToPlaylist(trxFilePaths, outcomes);
            PlaylistV1Builder.SaveToFile(playlist, playlistFilePath);
        }

        /// <summary>
        /// Converts a TRX file to a Visual Studio Test Playlist XML string.
        /// </summary>
        /// <param name="trxFilePath">Path to the TRX file to convert.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        /// <returns>An XML string representation of the playlist.</returns>
        public string ConvertTrxToPlaylistXml(string trxFilePath, params TrxLib.TestOutcome[] outcomes)
        {
            PlaylistRoot playlist = ConvertTrxToPlaylist(trxFilePath, outcomes);
            return PlaylistV1Builder.ToXmlString(playlist);
        }

        /// <summary>
        /// Converts multiple TRX files to a Visual Studio Test Playlist XML string with de-duplicated tests.
        /// </summary>
        /// <param name="trxFilePaths">Paths to the TRX files to convert.</param>
        /// <param name="outcomes">Optional filter for test outcomes to include. If not specified, all tests are included.</param>
        /// <returns>An XML string representation of the playlist.</returns>
        public string ConvertMultipleTrxToPlaylistXml(IEnumerable<string> trxFilePaths, params TrxLib.TestOutcome[] outcomes)
        {
            PlaylistRoot playlist = ConvertMultipleTrxToPlaylist(trxFilePaths, outcomes);
            return PlaylistV1Builder.ToXmlString(playlist);
        }
    }
}
