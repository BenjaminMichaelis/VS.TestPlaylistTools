using System.CommandLine;

namespace VSTestPlaylistTools.TrxToPlaylist;

public sealed class Program
{
    private static int Main(string[] args)
    {
        RootCommand rootCommand = GetRootCommand();
        var parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    public static RootCommand GetRootCommand()
    {
        Argument<string> trxPath = new("trx-file")
        {
            Description = "Path to the TRX file to convert"
        };
        
        Option<string?> playlistPath = new("--output", "-o")
        {
            Description = "Path to the output playlist file"
        };
        
        Option<string[]?> outcomesOption = new("--outcome", "-f")
        {
            Description = "Test outcomes to include",
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true
        };
        
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

        convertCommand.SetAction((parseResult) =>
        {
            var trxFile = parseResult.GetValue(trxPath);
            var output = parseResult.GetValue(playlistPath);
            var outcomes = parseResult.GetValue(outcomesOption);
            var skipEmpty = parseResult.GetValue(skipEmptyOption);
            
            if (string.IsNullOrEmpty(trxFile))
            {
                Console.WriteLine("Error: TRX file path must be specified.");
                return 1;
            }
            
            // If playlistFile is not specified, create a default path based on the trx file
            string playlistFile = output ?? Path.Combine(
                Path.GetDirectoryName(trxFile) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(trxFile)}.playlist");

            TrxLib.TestOutcome[]? outcomeArray = null;
            if (outcomes != null && outcomes.Length > 0)
            {
                List<TrxLib.TestOutcome> parsed = new();
                foreach (string outcomeStr in outcomes)
                {
                    if (Enum.TryParse<TrxLib.TestOutcome>(outcomeStr, ignoreCase: true, out TrxLib.TestOutcome outcomeValue))
                        parsed.Add(outcomeValue);
                    else
                    {
                        Console.WriteLine($"Invalid outcome: {outcomeStr}");
                        return 1;
                    }
                }
                outcomeArray = parsed.ToArray();
            }

            try
            {
                TrxToPlaylistConverter converter = new();
                var playlist = converter.ConvertTrxToPlaylist(trxFile, outcomeArray ?? []);

                if (skipEmpty && (playlist.Tests == null || playlist.Tests.Count == 0))
                {
                    Console.WriteLine($"No tests found in '{trxFile}'. Playlist file was not created due to --skip-empty.");
                    return 0;
                }

                converter.ConvertTrxToPlaylistFile(trxFile, playlistFile, outcomeArray ?? []);
                Console.WriteLine($"Converted '{trxFile}' to playlist '{playlistFile}'.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting TRX file: {ex.Message}");
                return 1;
            }
        });

        return new RootCommand("Convert TRX files to Visual Studio Test Playlists")
        {
            convertCommand
        };
    }
}