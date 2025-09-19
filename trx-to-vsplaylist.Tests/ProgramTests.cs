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

    private static Task<int> Invoke(string commandLine, StringWriter console)
    {
        // Parse the command line string into arguments
        var args = ParseCommandLine(commandLine);
        
        // Temporarily redirect console output to capture it
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(console);
            // Use the same entry point as the main program
            var method = typeof(Program).GetMethod("Main", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                var result = method.Invoke(null, new object[] { args });
                return Task.FromResult(result is int exitCode ? exitCode : 0);
            }
            return Task.FromResult(0);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string[] ParseCommandLine(string commandLine)
    {
        // Simple command line parsing - split on spaces but handle quoted strings
        List<string> args = new();
        bool inQuotes = false;
        var currentArg = new System.Text.StringBuilder();
        
        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (currentArg.Length > 0)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                }
            }
            else
            {
                currentArg.Append(c);
            }
        }
        
        if (currentArg.Length > 0)
        {
            args.Add(currentArg.ToString());
        }
        
        return args.ToArray();
    }
}