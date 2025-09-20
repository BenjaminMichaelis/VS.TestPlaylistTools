using System.CommandLine;

namespace VSTestPlaylistTools.TrxToPlaylist;

public sealed class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = GetRootCommand();
        var parseResult = rootCommand.Parse(args);
        var invocationConfig = new InvocationConfiguration();
        return await parseResult.InvokeAsync(invocationConfig);
    }

    public static RootCommand GetRootCommand()
    {
        Argument<string> trxPath = new("trx-file");
        trxPath.Description = "Path to the TRX file to convert";
        
        Option<string> playlistPath = new("--output", "-o");
        playlistPath.Description = "Path to the output playlist file. If not specified, the playlist will be saved in the same directory as the trx file with the same name but with .playlist extension.";
        
        Option<string[]> outcomesOption = new("--outcome", "-f");
        outcomesOption.Description = "Test outcomes to include (e.g. Passed Failed Skipped). Can be specified multiple times.";
        outcomesOption.Arity = ArgumentArity.ZeroOrMore;
        outcomesOption.AllowMultipleArgumentsPerToken = true;
        
        Option<bool> skipEmptyOption = new("--skip-empty");
        skipEmptyOption.Description = "Do not write a playlist file if there are no tests in the playlist.";

        Command convertCommand = new("convert", "Convert a TRX file to a Visual Studio Test Playlist");
        convertCommand.Add(trxPath);
        convertCommand.Add(playlistPath);
        convertCommand.Add(outcomesOption);
        convertCommand.Add(skipEmptyOption);
        
        convertCommand.SetAction((ParseResult parseResult) =>
        {
            string? trxFile = parseResult.GetValue(trxPath);
            string? playlistFile = parseResult.GetValue(playlistPath);
            string[]? outcomesRaw = parseResult.GetValue(outcomesOption);
            bool skipEmpty = parseResult.GetValue(skipEmptyOption);

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

        RootCommand rootCommand = new("Convert TRX files to Visual Studio Test Playlists");
        rootCommand.Add(convertCommand);
        return rootCommand;
    }

    // Keep the old method name for compatibility with tests, but return RootCommand now
    public static RootCommand GetConfiguration()
    {
        return GetRootCommand();
    }
}