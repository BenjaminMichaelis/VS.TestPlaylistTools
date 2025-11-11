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
            TrxLib.TestResultSet parsedTrx = TrxLib.TrxParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Count, playlist.Tests.Count);

            // Make sure all test names match the ones in the TRX file
            HashSet<string> trxTestNames = parsedTrx.Select(r => r.FullyQualifiedTestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();

            Assert.Equal(trxTestNames, playlistTestNames);
        }

        [Theory]
        [MemberData(nameof(SuccessTrxFiles))]
        public void ConvertTrxToPlaylist_NoFailuresButOutcomeFilterForAllTestOutcomesExceptSuccess_NoTests(string trxFilePath)
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            TrxLib.TestOutcome[] allOutcomesExceptPassed = Enum.GetValues<TrxLib.TestOutcome>().Where(o => o != TrxLib.TestOutcome.Passed).ToArray();
            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, allOutcomesExceptPassed);

            Assert.NotNull(playlist);
            Assert.Empty(playlist.Tests);
        }

        [Theory]
        [MemberData(nameof(SuccessTrxFiles))]
        public void ConvertTrxToPlaylistFile_NoFailuresButOutcomeFilterOnlyPassed_AllTests(string trxFilePath)
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, TrxLib.TestOutcome.Passed);

            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);
            TrxLib.TestResultSet parsedTrx = TrxLib.TrxParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Count, playlist.Tests.Count);
            // Make sure all test names match the ones in the TRX file
            HashSet<string> trxTestNames = parsedTrx.Select(r => r.FullyQualifiedTestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();
            Assert.NotEmpty(playlistTestNames);
            Assert.Equal(trxTestNames, playlistTestNames);
        }


        public static TheoryData<string> SuccessTrxFiles()
        {
            string testResourcesPath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success");
            string[] sampleFiles = Directory.GetFiles(testResourcesPath, "*.trx");
            TheoryData<string> theoryData = new();

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
            TrxLib.TestResultSet parsedTrx = TrxLib.TrxParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Count, playlist.Tests.Count);
            // Make sure all test names match the ones in the TRX file
            HashSet<string> trxTestNames = parsedTrx.Select(r => r.FullyQualifiedTestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();
            Assert.NotEmpty(playlistTestNames);
            Assert.Equal(trxTestNames, playlistTestNames);
        }

        [Fact]
        public void ConvertTrxToPlaylist_OneFailureWithFailureFilter_OneTest()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, TrxLib.TestOutcome.Failed);

            Assert.NotNull(playlist);
            Assert.Single(playlist.Tests);

            // Check that the failure test is included
            TrxLib.TestResultSet parsedTrx = TrxLib.TrxParser.Parse(new FileInfo(trxFilePath));
            Assert.Equal(parsedTrx.Failed.Count, playlist.Tests.Count);
            Assert.Single(parsedTrx.Failed);

            // Make sure the test name matches the failure test in the TRX file
            string failedTestName = parsedTrx.Failed.First().FullyQualifiedTestName;
            Assert.Contains(playlist.Tests, t => t.Test == failedTestName);
            Assert.Contains(playlist.Tests, t => t.Test == "AddisonWesley.Michaelis.EssentialCSharp.Chapter01.Listing01_03.Tests.HelloWorldTests.Main_UpDown");
        }

        [Fact]
        public void ConvertTrxToPlaylist_OneFailureWithPassedFilter_AllButOneTest()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertTrxToPlaylist(trxFilePath, TrxLib.TestOutcome.Passed);

            Assert.NotNull(playlist);

            TrxLib.TestResultSet parsedTrx = TrxLib.TrxParser.Parse(new FileInfo(trxFilePath));

            Assert.Equal(parsedTrx.Passed.Count, playlist.Tests.Count);

            // Make sure all test names match the ones in the TRX file except the failed one
            HashSet<string> passedTestNames = parsedTrx.Passed.Select(r => r.FullyQualifiedTestName).ToHashSet();
            HashSet<string> playlistTestNames = playlist.Tests.Select(t => t.Test).WhereNotNull().ToHashSet();
            Assert.NotEmpty(playlistTestNames);
            Assert.Equal(passedTestNames, playlistTestNames);
        }

        #region Multiple TRX Files Tests

        [Fact]
        public void ConvertMultipleTrxToPlaylist_TwoFiles_MergesTests()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertMultipleTrxToPlaylist(new[] { trxFile1, trxFile2 });

            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);

            // Count total unique tests from both files
            TrxLib.TestResultSet parsedTrx1 = TrxLib.TrxParser.Parse(new FileInfo(trxFile1));
            TrxLib.TestResultSet parsedTrx2 = TrxLib.TrxParser.Parse(new FileInfo(trxFile2));

            HashSet<string> allTestNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (var result in parsedTrx1)
            {
                allTestNames.Add(result.FullyQualifiedTestName);
            }
            foreach (var result in parsedTrx2)
            {
                allTestNames.Add(result.FullyQualifiedTestName);
            }

            Assert.Equal(allTestNames.Count, playlist.Tests.Count);
        }

        [Fact]
        public void ConvertMultipleTrxToPlaylist_WithDuplicateTests_DeduplicatesTests()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            // Use the same file twice to ensure de-duplication works
            string trxFile = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertMultipleTrxToPlaylist(new[] { trxFile, trxFile });

            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);

            // Should have same count as single file since duplicates are removed
            TrxLib.TestResultSet parsedTrx = TrxLib.TrxParser.Parse(new FileInfo(trxFile));
            // Get unique test names from TRX file (handles parameterized tests that appear multiple times)
            int uniqueTestCount = parsedTrx.Select(r => r.FullyQualifiedTestName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            Assert.Equal(uniqueTestCount, playlist.Tests.Count);
        }

        [Fact]
        public void ConvertMultipleTrxToPlaylist_WithOutcomeFilter_FiltersAcrossAllFiles()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            VS.TestPlaylistTools.PlaylistV1.PlaylistRoot playlist = converter.ConvertMultipleTrxToPlaylist(
                new[] { trxFile1, trxFile2 },
                TrxLib.TestOutcome.Failed);

            Assert.NotNull(playlist);

            // Should only have failed tests (from second file)
            TrxLib.TestResultSet parsedTrx2 = TrxLib.TrxParser.Parse(new FileInfo(trxFile2));
            Assert.Equal(parsedTrx2.Failed.Count, playlist.Tests.Count);
        }

        [Fact]
        public void ConvertMultipleTrxToPlaylist_EmptyArray_ThrowsArgumentException()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();

            Assert.Throws<ArgumentException>(() =>
            {
                converter.ConvertMultipleTrxToPlaylist(Array.Empty<string>());
            });
        }

        [Fact]
        public void ConvertMultipleTrxToPlaylist_NullArray_ThrowsArgumentNullException()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();

            Assert.Throws<ArgumentNullException>(() =>
            {
                converter.ConvertMultipleTrxToPlaylist(null!);
            });
        }

        [Fact]
        public void ConvertMultipleTrxToPlaylist_NonExistentFile_ThrowsFileNotFoundException()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();

            Assert.Throws<FileNotFoundException>(() =>
            {
                converter.ConvertMultipleTrxToPlaylist(new[] { "nonexistent.trx" });
            });
        }

        [Fact]
        public void ConvertMultipleTrxToPlaylistFile_TwoFiles_CreatesFile()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            string outputPath = Path.Combine(Path.GetTempPath(), "MergedConverter.playlist");

            try
            {
                converter.ConvertMultipleTrxToPlaylistFile(new[] { trxFile1, trxFile2 }, outputPath);

                Assert.True(File.Exists(outputPath), "Output playlist file was not created.");

                var playlistContent = VS.TestPlaylistTools.PlaylistLoader.Load(outputPath);
                var playlist = Assert.IsType<VS.TestPlaylistTools.PlaylistV1.PlaylistRoot>(playlistContent);
                Assert.NotNull(playlist);
                Assert.NotEmpty(playlist.Tests);
            }
            finally
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
        }

        [Fact]
        public void ConvertMultipleTrxToPlaylistXml_TwoFiles_ReturnsValidXml()
        {
            TrxToPlaylist.TrxToPlaylistConverter converter = new VSTestPlaylistTools.TrxToPlaylist.TrxToPlaylistConverter();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            string xml = converter.ConvertMultipleTrxToPlaylistXml(new[] { trxFile1, trxFile2 });

            Assert.NotNull(xml);
            Assert.NotEmpty(xml);
            Assert.Contains("<Playlist", xml);
            Assert.Contains("Version=\"1.0\"", xml);
        }

        #endregion
    }
}