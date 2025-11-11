using System.CommandLine;

using VS.TestPlaylistTools;
using VS.TestPlaylistTools.PlaylistV1;

namespace VSTestPlaylistTools.TrxToPlaylist.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Invoke_WithHelpOption_DisplaysHelp()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("--help", stdOut);

        Assert.Equal(0, exitCode);
        Assert.Contains("--help", stdOut.ToString());
    }

    [Fact]
    public async Task Invoke_WithSuccessTrxFile_CreatesPlaylistWithAllTests()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "Sample.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            int exitCode = await Invoke($"convert \"{trxFilePath}\" --output \"{playlistFilePath}\"", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(playlistFilePath), "Playlist file was not created.");

            var playlistContent = PlaylistLoader.Load(playlistFilePath);
            PlaylistRoot? playlist = playlistContent as PlaylistRoot;
            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);
        }
        finally
        {
            // Clean up the created playlist file if it exists
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithSuccessTrxFileAndFailedFilter_CreatesPlaylistFileWithNoTests()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "Filtered.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            int exitCode = await Invoke($"convert \"{trxFilePath}\" --output \"{playlistFilePath}\" --outcome Failed", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(playlistFilePath), "Playlist file was not created.");
            var playlistContent = PlaylistLoader.Load(playlistFilePath);
            PlaylistRoot? playlist = playlistContent as PlaylistRoot;
            Assert.NotNull(playlist);
            Assert.Empty(playlist.Tests);
        }
        finally
        {
            // Clean up the created playlist file if it exists
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithSingleFailureTrxFileAndFailedFilter_CreatesPlaylistWithSingleTest()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "SingleFailure.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            int exitCode = await Invoke($"convert \"{trxFilePath}\" --output \"{playlistFilePath}\" --outcome Failed", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(playlistFilePath), "Playlist file was not created.");
            var playlistContent = PlaylistLoader.Load(playlistFilePath);
            PlaylistRoot? playlist = playlistContent as PlaylistRoot;
            Assert.NotNull(playlist);
            Assert.Single(playlist.Tests);
        }
        finally
        {
            // Clean up the created playlist file if it exists
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithSingleFailureTrxFileAndFailedAndPassedFilter_CreatesPlaylistWithAllTests()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "SingleFailureWithPassed.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            int exitCode = await Invoke($"convert \"{trxFilePath}\" --output \"{playlistFilePath}\" --outcome Failed Passed", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(playlistFilePath), "Playlist file was not created.");
            var playlistContent = PlaylistLoader.Load(playlistFilePath);
            PlaylistRoot? playlist = playlistContent as PlaylistRoot;
            Assert.NotNull(playlist);
            Assert.True(playlist.TestCount > 1, "Playlist should contain both passed and failed tests.");
        }
        finally
        {
            // Clean up the created playlist file if it exists
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
            // Clean up if a directory was accidentally created
            if (Directory.Exists(playlistFilePath))
            {
                Directory.Delete(playlistFilePath, true);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithSuccessTrxFileNoOutputPath_CreatesPlaylistWithDefaultName()
    {
        // Get a TRX file path for testing
        string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");

        // Calculate the expected default playlist path
        string expectedPlaylistPath = Path.Combine(Path.GetDirectoryName(trxFilePath)!, $"{Path.GetFileNameWithoutExtension(trxFilePath)}.playlist");

        try
        {
            using StringWriter stdOut = new();
            int exitCode = await Invoke($"convert \"{trxFilePath}\"", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedPlaylistPath), "Playlist file was not created at the default location.");

            var playlistContent = PlaylistLoader.Load(expectedPlaylistPath);
            PlaylistRoot? playlist = playlistContent as PlaylistRoot;
            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);
        }
        finally
        {
            // Clean up the created playlist file if it exists
            if (File.Exists(expectedPlaylistPath))
            {
                File.Delete(expectedPlaylistPath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithSuccessTrxFileAndFailedFilterAndSkipEmpty_DoesNotCreatePlaylistFile()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "FilteredSkipEmpty.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFilePath = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            int exitCode = await Invoke($"convert \"{trxFilePath}\" --output \"{playlistFilePath}\" --outcome Failed --skip-empty", stdOut);
            Assert.Equal(0, exitCode);
            Assert.False(File.Exists(playlistFilePath), "Playlist file should not be created when --skip-empty is used and there are no tests.");
            Assert.Contains("Playlist file was not created due to --skip-empty", stdOut.ToString());
        }
        finally
        {
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    #region Multiple TRX Files Tests

    [Fact]
    public async Task Invoke_WithMultipleTrxFiles_CreatesMergedPlaylistWithAllTests()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "MergedMultiple.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            int exitCode = await Invoke($"convert \"{trxFile1}\" \"{trxFile2}\" --output \"{playlistFilePath}\"", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(playlistFilePath), "Merged playlist file was not created.");

            var playlistContent = PlaylistLoader.Load(playlistFilePath);
            PlaylistRoot? playlist = playlistContent as PlaylistRoot;
            Assert.NotNull(playlist);
            Assert.NotEmpty(playlist.Tests);

            // Verify the output message mentions multiple files
            Assert.Contains("TRX files", stdOut.ToString());
            Assert.Contains("unique tests", stdOut.ToString());
        }
        finally
        {
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithMultipleTrxFilesAndFailedFilter_CreatesMergedPlaylistWithOnlyFailedTests()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "MergedFailed.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            int exitCode = await Invoke($"convert \"{trxFile1}\" \"{trxFile2}\" --outcome Failed --output \"{playlistFilePath}\"", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(playlistFilePath), "Merged playlist file was not created.");

            var playlistContent = PlaylistLoader.Load(playlistFilePath);
            PlaylistRoot? playlist = playlistContent as PlaylistRoot;
            Assert.NotNull(playlist);
            // Should have at least one failed test from the second file
            Assert.NotEmpty(playlist.Tests);
        }
        finally
        {
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithMultipleTrxFilesAndSeparateOption_CreatesMultiplePlaylists()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), "SeparatePlaylists");
        Directory.CreateDirectory(outputDir);

        try
        {
            using StringWriter stdOut = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            int exitCode = await Invoke($"convert \"{trxFile1}\" \"{trxFile2}\" --output \"{outputDir}\" --separate", stdOut);
            Assert.Equal(0, exitCode);

            // Verify separate playlist files were created
            string playlist1 = Path.Combine(outputDir, "AllTestsPass.playlist");
            string playlist2 = Path.Combine(outputDir, "OneTestFailure.playlist");

            Assert.True(File.Exists(playlist1), "First playlist file was not created.");
            Assert.True(File.Exists(playlist2), "Second playlist file was not created.");

            // Verify output message
            Assert.Contains("Created 2 playlist file(s)", stdOut.ToString());
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithMultipleTrxFilesSeparateAndSkipEmpty_OnlyCreatesNonEmptyPlaylists()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), "SeparatePlaylistsSkipEmpty");
        Directory.CreateDirectory(outputDir);

        try
        {
            using StringWriter stdOut = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            // Filter for failed tests only - first file will be empty
            int exitCode = await Invoke($"convert \"{trxFile1}\" \"{trxFile2}\" --output \"{outputDir}\" --separate --skip-empty --outcome Failed", stdOut);
            Assert.Equal(0, exitCode);

            // Verify only the second playlist was created (has failed tests)
            string playlist1 = Path.Combine(outputDir, "AllTestsPass.playlist");
            string playlist2 = Path.Combine(outputDir, "OneTestFailure.playlist");

            Assert.False(File.Exists(playlist1), "First playlist should have been skipped (no failed tests).");
            Assert.True(File.Exists(playlist2), "Second playlist file was not created.");

            // Verify output mentions skipping
            Assert.Contains("Skipped", stdOut.ToString());
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithMultipleTrxFilesSeparateAndFileOutput_ThrowsArgumentException()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "ShouldNotWork.playlist");

        try
        {
            using StringWriter stdOut = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");

            // Should fail because we're providing a file path with --separate
            int exitCode = await Invoke($"convert \"{trxFile1}\" \"{trxFile2}\" --output \"{playlistFilePath}\" --separate", stdOut);
            Assert.Equal(1, exitCode); // Expect failure exit code
        }
        finally
        {
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithSingleTrxFileAndSeparateOption_ShowsWarning()
    {
        string playlistFilePath = Path.Combine(Path.GetTempPath(), "SingleWithSeparate.playlist");
        try
        {
            using StringWriter stdOut = new();
            string trxFile = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");

            int exitCode = await Invoke($"convert \"{trxFile}\" --output \"{playlistFilePath}\" --separate", stdOut);
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(playlistFilePath), "Playlist file was not created.");

            // Should show warning about --separate having no effect
            Assert.Contains("Warning", stdOut.ToString());
            Assert.Contains("no effect", stdOut.ToString());
        }
        finally
        {
            if (File.Exists(playlistFilePath))
            {
                File.Delete(playlistFilePath);
            }
        }
    }

    [Fact]
    public async Task Invoke_WithSingleTrxFileAndDirectoryOutput_CreatesPlaylistInDirectory()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), "SingleFileToDirectory");
        Directory.CreateDirectory(outputDir);

        try
        {
            using StringWriter stdOut = new();
            string trxFile = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");

            int exitCode = await Invoke($"convert \"{trxFile}\" --output \"{outputDir}\"", stdOut);
            Assert.Equal(0, exitCode);

            // Verify playlist was created in the directory with the TRX file's name
            string expectedPlaylist = Path.Combine(outputDir, "AllTestsPass.playlist");
            Assert.True(File.Exists(expectedPlaylist), "Playlist file was not created in the specified directory.");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    #endregion

    #region Merge Command Tests

    [Fact]
    public async Task Invoke_MergeCommand_CombinesMultiplePlaylists()
    {
        // First create some playlists to merge
        string playlist1Path = Path.Combine(Path.GetTempPath(), "ToMerge1.playlist");
        string playlist2Path = Path.Combine(Path.GetTempPath(), "ToMerge2.playlist");
        string mergedPath = Path.Combine(Path.GetTempPath(), "MergedResult.playlist");

        try
        {
            using StringWriter stdOut1 = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            await Invoke($"convert \"{trxFile1}\" --output \"{playlist1Path}\"", stdOut1);

            using StringWriter stdOut2 = new();
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            await Invoke($"convert \"{trxFile2}\" --output \"{playlist2Path}\"", stdOut2);

            // Now merge them
            using StringWriter stdOut3 = new();
            int exitCode = await Invoke($"merge \"{playlist1Path}\" \"{playlist2Path}\" --output \"{mergedPath}\"", stdOut3);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(mergedPath), "Merged playlist was not created.");

            var playlistContent = PlaylistLoader.Load(mergedPath);
            PlaylistRoot? mergedPlaylist = playlistContent as PlaylistRoot;
            Assert.NotNull(mergedPlaylist);
            Assert.NotEmpty(mergedPlaylist.Tests);

            // Verify output message
            Assert.Contains("Merged 2 playlist files", stdOut3.ToString());
            Assert.Contains("unique tests", stdOut3.ToString());
        }
        finally
        {
            if (File.Exists(playlist1Path)) File.Delete(playlist1Path);
            if (File.Exists(playlist2Path)) File.Delete(playlist2Path);
            if (File.Exists(mergedPath)) File.Delete(mergedPath);
        }
    }

    [Fact]
    public async Task Invoke_MergeCommandWithoutOutput_ThrowsException()
    {
        string playlist1Path = Path.Combine(Path.GetTempPath(), "ToMergeNoOutput.playlist");

        try
        {
            // Create a playlist first
            using StringWriter stdOut1 = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            await Invoke($"convert \"{trxFile1}\" --output \"{playlist1Path}\"", stdOut1);

            // Try to merge without output - should fail
            using StringWriter stdOut2 = new();
            int exitCode = await Invoke($"merge \"{playlist1Path}\"", stdOut2);
            Assert.Equal(1, exitCode); // Expect failure exit code
        }
        finally
        {
            if (File.Exists(playlist1Path)) File.Delete(playlist1Path);
            if (Directory.Exists(playlist1Path)) Directory.Delete(playlist1Path, true);
        }
    }

    [Fact]
    public async Task Invoke_MergeCommandWithGlobPattern_MergesMatchingFiles()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "GlobPatternTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        string playlist1Path = Path.Combine(testDir, "test1.playlist");
        string playlist2Path = Path.Combine(testDir, "test2.playlist");
        string mergedPath = Path.Combine(testDir, "merged.playlist");

        try
        {
            // Create some playlists to merge
            using StringWriter stdOut1 = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            await Invoke($"convert \"{trxFile1}\" --output \"{playlist1Path}\"", stdOut1);

            using StringWriter stdOut2 = new();
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            await Invoke($"convert \"{trxFile2}\" --output \"{playlist2Path}\"", stdOut2);

            // Merge using glob pattern
            using StringWriter stdOut3 = new();
            string globPattern = Path.Combine(testDir, "test*.playlist");
            int exitCode = await Invoke($"merge \"{globPattern}\" --output \"{mergedPath}\"", stdOut3);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(mergedPath), "Merged playlist was not created.");

            var playlistContent = PlaylistLoader.Load(mergedPath);
            PlaylistRoot? mergedPlaylist = playlistContent as PlaylistRoot;
            Assert.NotNull(mergedPlaylist);
            Assert.NotEmpty(mergedPlaylist.Tests);

            // Verify output message
            Assert.Contains("Merged 2 playlist files", stdOut3.ToString());
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [Fact]
    public async Task Invoke_MergeCommandWithoutOutputAndSameDirectory_AutoDetectsOutputDirectory()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "AutoDetectTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        string playlist1Path = Path.Combine(testDir, "auto1.playlist");
        string playlist2Path = Path.Combine(testDir, "auto2.playlist");
        string expectedMergedPath = Path.Combine(testDir, "merged.playlist");

        try
        {
            // Create some playlists in the same directory
            using StringWriter stdOut1 = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            await Invoke($"convert \"{trxFile1}\" --output \"{playlist1Path}\"", stdOut1);

            using StringWriter stdOut2 = new();
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            await Invoke($"convert \"{trxFile2}\" --output \"{playlist2Path}\"", stdOut2);

            // Merge without specifying output - should auto-detect
            using StringWriter stdOut3 = new();
            int exitCode = await Invoke($"merge \"{playlist1Path}\" \"{playlist2Path}\"", stdOut3);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedMergedPath), "Merged playlist was not created in auto-detected directory.");

            var playlistContent = PlaylistLoader.Load(expectedMergedPath);
            PlaylistRoot? mergedPlaylist = playlistContent as PlaylistRoot;
            Assert.NotNull(mergedPlaylist);
            Assert.NotEmpty(mergedPlaylist.Tests);

            // Verify output message mentions auto-detection
            Assert.Contains("Output directory not specified", stdOut3.ToString());
            Assert.Contains("same directory as input files", stdOut3.ToString());
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [Fact]
    public async Task Invoke_MergeCommandWithGlobPatternAndAutoDetect_MergesInSameDirectory()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "GlobAutoDetectTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        string playlist1Path = Path.Combine(testDir, "glob1.playlist");
        string playlist2Path = Path.Combine(testDir, "glob2.playlist");
        string expectedMergedPath = Path.Combine(testDir, "merged.playlist");

        try
        {
            // Create some playlists
            using StringWriter stdOut1 = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            await Invoke($"convert \"{trxFile1}\" --output \"{playlist1Path}\"", stdOut1);

            using StringWriter stdOut2 = new();
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            await Invoke($"convert \"{trxFile2}\" --output \"{playlist2Path}\"", stdOut2);

            // Merge using glob pattern without output - should auto-detect
            using StringWriter stdOut3 = new();
            string globPattern = Path.Combine(testDir, "glob*.playlist");
            int exitCode = await Invoke($"merge \"{globPattern}\"", stdOut3);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(expectedMergedPath), "Merged playlist was not created.");

            var playlistContent = PlaylistLoader.Load(expectedMergedPath);
            PlaylistRoot? mergedPlaylist = playlistContent as PlaylistRoot;
            Assert.NotNull(mergedPlaylist);
            Assert.NotEmpty(mergedPlaylist.Tests);

            // Verify both messages appear
            Assert.Contains("Merged 2 playlist files", stdOut3.ToString());
            Assert.Contains("same directory as input files", stdOut3.ToString());
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [Fact]
    public async Task Invoke_MergeCommandWithDifferentDirectories_RequiresOutput()
    {
        string testDir1 = Path.Combine(Path.GetTempPath(), "DiffDir1_" + Guid.NewGuid().ToString("N"));
        string testDir2 = Path.Combine(Path.GetTempPath(), "DiffDir2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir1);
        Directory.CreateDirectory(testDir2);

        string playlist1Path = Path.Combine(testDir1, "diff1.playlist");
        string playlist2Path = Path.Combine(testDir2, "diff2.playlist");

        try
        {
            // Create playlists in different directories
            using StringWriter stdOut1 = new();
            string trxFile1 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "Success", "AllTestsPass.trx");
            await Invoke($"convert \"{trxFile1}\" --output \"{playlist1Path}\"", stdOut1);

            using StringWriter stdOut2 = new();
            string trxFile2 = Path.Combine(IntelliTect.Multitool.RepositoryPaths.GetDefaultRepoRoot(), "VSTestPlaylistTools.TrxToPlaylistConverter.Tests", "SampleTrxFiles", "OneTestFailure.trx");
            await Invoke($"convert \"{trxFile2}\" --output \"{playlist2Path}\"", stdOut2);

            // Try to merge without output - should fail because directories differ
            using StringWriter stdOut3 = new();
            int exitCode = await Invoke($"merge \"{playlist1Path}\" \"{playlist2Path}\"", stdOut3);

            Assert.Equal(1, exitCode); // Expect failure exit code
        }
        finally
        {
            if (Directory.Exists(testDir1)) Directory.Delete(testDir1, true);
            if (Directory.Exists(testDir2)) Directory.Delete(testDir2, true);
        }
    }

    [Fact]
    public async Task Invoke_MergeCommandWithInvalidGlobPattern_ThrowsException()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "NoMatchTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            // Try to merge with a pattern that matches nothing
            using StringWriter stdOut = new();
            string globPattern = Path.Combine(testDir, "nonexistent*.playlist");
            string outputPath = Path.Combine(testDir, "output.playlist");
            int exitCode = await Invoke($"merge \"{globPattern}\" --output \"{outputPath}\"", stdOut);

            Assert.Equal(1, exitCode); // Expect failure exit code
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    #endregion

    private static async Task<int> Invoke(string commandLine, StringWriter console)
    {
        RootCommand rootCommand = Program.GetConfiguration();
        var parseResult = rootCommand.Parse(commandLine);
        var invocationConfig = new InvocationConfiguration();

        // Redirect output to the provided console
        var originalOut = Console.Out;
        Console.SetOut(console);

        try
        {
            return await parseResult.InvokeAsync(invocationConfig);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}