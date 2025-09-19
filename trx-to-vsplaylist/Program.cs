using System.CommandLine;

namespace VSTestPlaylistTools.TrxToPlaylist;

public sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = GetRootCommand();
        return await rootCommand.InvokeAsync(args);
    }

    public static RootCommand GetRootCommand()
    {
        Argument<string> trxPath = new("trx-file", "Path to the TRX file to convert");
        
        Option<string?> playlistPath = new(["--output", "-o"], "Path to the output playlist file. If not specified, the playlist will be saved in the same directory as the trx file with the same name but with .playlist extension.");
        
        Option<string[]?> outcomesOption = new(["--outcome", "-f"], "Test outcomes to include (e.g. Passed Failed Skipped). Can be specified multiple times.")
        {
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true
        };
        
        Option<bool> skipEmptyOption = new("--skip-empty", "Do not write a playlist file if there are no tests in the playlist.");

        Command convertCommand = new("convert", "Convert a TRX file to a Visual Studio Test Playlist")
        {
            trxPath,
            playlistPath,
            outcomesOption,
            skipEmptyOption
        };

        convertCommand.SetHandler(async (trxFile, output, outcome, skipEmpty) =>
        {
            if (string.IsNullOrEmpty(trxFile))
                throw new ArgumentException("TRX file path must be specified.");

            // If playlistFile is not specified, create a default path based on the trx file
            string playlistFile = output ?? Path.Combine(
                Path.GetDirectoryName(trxFile) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(trxFile)}.playlist");

            TrxLib.TestOutcome[]? outcomes = null;
            if (outcome is { Length: > 0 })
            {
                List<TrxLib.TestOutcome> parsed = new();
                foreach (string outcomeStr in outcome)
                {
                    if (Enum.TryParse<TrxLib.TestOutcome>(outcomeStr, ignoreCase: true, out TrxLib.TestOutcome outcomeValue))
                        parsed.Add(outcomeValue);
                    else
                        throw new ArgumentException($"Invalid outcome: {outcomeStr}");
                }
                outcomes = parsed.ToArray();
            }

            TrxToPlaylistConverter converter = new();
            var playlist = converter.ConvertTrxToPlaylist(trxFile, outcomes ?? []);

            if (skipEmpty && (playlist.Tests == null || playlist.Tests.Count == 0))
            {
                Console.WriteLine($"No tests found in '{trxFile}'. Playlist file was not created due to --skip-empty.");
                return;
            }

            converter.ConvertTrxToPlaylistFile(trxFile, playlistFile, outcomes ?? []);
            Console.WriteLine($"Converted '{trxFile}' to playlist '{playlistFile}'.");
        }, trxPath, playlistPath, outcomesOption, skipEmptyOption);

        return new RootCommand("Convert TRX files to Visual Studio Test Playlists")
        {
            convertCommand
        };
    }
}