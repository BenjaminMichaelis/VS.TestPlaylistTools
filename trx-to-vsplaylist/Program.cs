using System.CommandLine;

namespace VSTestPlaylistTools.TrxToPlaylist;

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        CommandLineConfiguration configuration = GetConfiguration();
        return configuration.InvokeAsync(args);
    }

    public static CommandLineConfiguration GetConfiguration()
    {
        Argument<string> trxPath = new("trx-file")
        {
            Description = "Path to the TRX file to convert"
        };
        // Change from CliArgument to CliOption to make it optional
        Option<string> playlistPath = new("--output", "-o")
        {
            Description = "Path to the output playlist file. If not specified, the playlist will be saved in the same directory as the trx file with the same name but with .playlist extension."
        };
        // Option for specifying outcomes (repeatable)
        Option<string[]> outcomesOption = new("--outcome", "-f")
        {
            Description = "Test outcomes to include (e.g. Passed Failed Skipped). Can be specified multiple times.",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true
        };
        // Option to skip writing empty playlists
        Option<bool> skipEmptyOption = new("--skip-empty")
        {
            Description = "Do not write a playlist file if there are no tests in the playlist."
        };

        Command convertCommand = new("convert", "Convert a TRX file to a Visual Studio Test Playlist")
        {
            trxPath,
            playlistPath,
            outcomesOption,
            skipEmptyOption
        };
        convertCommand.SetAction((ParseResult parseResult) =>
        {
            string? trxFile = parseResult.CommandResult.GetValue(trxPath);
            string? playlistFile = parseResult.CommandResult.GetValue(playlistPath);
            string[]? outcomesRaw = parseResult.CommandResult.GetValue(outcomesOption);
            bool skipEmpty = parseResult.CommandResult.GetValue(skipEmptyOption);

            if (string.IsNullOrEmpty(trxFile))
                throw new ArgumentException("TRX file path must be specified.");

            // If playlistFile is not specified, create a default path based on the trx file
            if (string.IsNullOrEmpty(playlistFile))
            {
                string directory = Path.GetDirectoryName(trxFile) ?? string.Empty;
                string fileName = Path.GetFileNameWithoutExtension(trxFile);
                playlistFile = Path.Combine(directory, $"{fileName}.playlist");
            }

            TrxLib.TestOutcome[]? outcomes = null;
            if (outcomesRaw is { Length: > 0 })
            {
                List<TrxLib.TestOutcome> parsed = new List<TrxLib.TestOutcome>();
                foreach (string outcomeStr in outcomesRaw)
                {
                    if (Enum.TryParse<TrxLib.TestOutcome>(outcomeStr, ignoreCase: true, out TrxLib.TestOutcome outcome))
                        parsed.Add(outcome);
                    else
                        throw new ArgumentException($"Invalid outcome: {outcomeStr}");
                }
                outcomes = parsed.ToArray();
            }

            TrxToPlaylistConverter converter = new TrxToPlaylistConverter();
            var playlist = converter.ConvertTrxToPlaylist(trxFile, outcomes ?? []);

            if (skipEmpty && (playlist.Tests == null || playlist.Tests.Count == 0))
            {
                parseResult.Configuration.Output.WriteLine($"No tests found in '{trxFile}'. Playlist file was not created due to --skip-empty.");
                return;
            }

            converter.ConvertTrxToPlaylistFile(trxFile, playlistFile, outcomes ?? []);
            parseResult.Configuration.Output.WriteLine($"Converted '{trxFile}' to playlist '{playlistFile}'.");
        });

        RootCommand rootCommand = new("Convert TRX files to Visual Studio Test Playlists")
        {
            convertCommand
        };
        return new CommandLineConfiguration(rootCommand);
    }
}