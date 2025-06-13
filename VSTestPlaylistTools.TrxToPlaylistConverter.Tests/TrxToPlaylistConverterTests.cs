using IntelliTect.Multitool.Extensions;

namespace VSTestPlaylistTools.TrxToPlaylistConverter.Tests
{
    public class TrxToPlaylistConverterTests
    {
        [Theory]
        [MemberData(nameof(SuccessTrxFiles))]
        public void ConvertTrxToPlaylist_Success(string trxFilePath)
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new TrxToPlaylist.TrxToPlaylistConverter();
            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath);
            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);
            TRexLib.TestResultSet parsedTrx = TRexLib.TestOutputDocumentParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Count, playlist.Tests.Count);

            // Make sure all test names match the ones in the TRX file
            HashSet<string> trxTestNames = parsedTrx.Select(r => r.TestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();

            Assert.Equal(trxTestNames, playlistTestNames);
        }

        [Theory]
        [MemberData(nameof(SuccessTrxFiles))]
        public void ConvertTrxToPlaylist_NoFailuresButOutcomeFilterForAllTestOutcomesExceptSuccess_NoTests(string trxFilePath)
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            TRexLib.TestOutcome[] allOutcomesExceptPassed = Enum.GetValues<TRexLib.TestOutcome>().Where(o => o != TRexLib.TestOutcome.Passed).ToArray();
            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, allOutcomesExceptPassed);

            Assert.NotNull(playlist);
            Assert.Empty(playlist.Tests);
        }

        [Theory]
        [MemberData(nameof(SuccessTrxFiles))]
        public void ConvertTrxToPlaylistFile_NoFailuresButOutcomeFilterOnlyPassed_AllTests(string trxFilePath)
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, TRexLib.TestOutcome.Passed);

            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);
            TRexLib.TestResultSet parsedTrx = TRexLib.TestOutputDocumentParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Count, playlist.Tests.Count);
            // Make sure all test names match the ones in the TRX file
            HashSet<string> trxTestNames = parsedTrx.Select(r => r.TestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();
            Assert.NotEmpty(playlistTestNames);
            Assert.Equal(trxTestNames, playlistTestNames);
        }


        public static TheoryData<string> SuccessTrxFiles()
        {
            string testResourcesPath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success");
            string[] sampleFiles = Directory.GetFiles(testResourcesPath, "*.trx");
            TheoryData<string> theoryData = new TheoryData<string>();

            foreach (string fileName in sampleFiles)
            {
                string filePath = fileName;
                if (File.Exists(filePath))
                {
                    theoryData.Add(filePath);
                }
            }

            return theoryData;
        }

        [Fact]
        public void ConvertTrxToPlaylist_OneFailureWithNoOutcomeFilter_AllTests()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath);

            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);

            // Check that the failure test is included
            TRexLib.TestResultSet parsedTrx = TRexLib.TestOutputDocumentParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Count, playlist.Tests.Count);
            // Make sure all test names match the ones in the TRX file
            HashSet<string> trxTestNames = parsedTrx.Select(r => r.TestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();
            Assert.NotEmpty(playlistTestNames);
            Assert.Equal(trxTestNames, playlistTestNames);
        }

        [Fact]
        public void ConvertTrxToPlaylist_OneFailureWithFailureFilter_OneTest()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, TRexLib.TestOutcome.Failed);

            Assert.NotNull(playlist);
            Assert.Single(playlist.Tests);

            // Check that the failure test is included
            TRexLib.TestResultSet parsedTrx = TRexLib.TestOutputDocumentParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Failed.Count, playlist.Tests.Count);

            // Make sure the test name matches the failure test in the TRX file
            string failedTestName = parsedTrx.Failed.First().TestName;
            Assert.Contains(playlist.Tests, t => t.Test == failedTestName);
        }

        [Fact]
        public void ConvertTrxToPlaylist_OneFailureWithPassedFilter_AllButOneTest()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, TRexLib.TestOutcome.Passed);

            Assert.NotNull(playlist);

            TRexLib.TestResultSet parsedTrx = TRexLib.TestOutputDocumentParser.Parse(new FileInfo(trxFilePath));

            Assert.Equal(parsedTrx.Passed.Count, playlist.Tests.Count);

            // Make sure all test names match the ones in the TRX file except the failed one
            HashSet<string> passedTestNames = parsedTrx.Passed.Select(r => r.TestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();
            Assert.NotEmpty(playlistTestNames);
            Assert.Equal(passedTestNames, playlistTestNames);
        }
    }
}