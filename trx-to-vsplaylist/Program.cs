using System.CommandLine;

namespace VSTestPlaylistTools.TrxToPlaylist;

public sealed class Program
{
    private static int Main(string[] args)
    {
        RootCommand rootCommand = GetRootCommand();
        
        // Simple parsing approach for beta5
        try 
        {
            // Use reflection to find the correct method
            var method = rootCommand.GetType().GetMethod("Invoke", new[] { typeof(string[]) });
            if (method != null)
            {
                var result = method.Invoke(rootCommand, new object[] { args });
                return result is int exitCode ? exitCode : 0;
            }
            
            // If direct invoke doesn't work, try alternative approaches
            var parseMethod = rootCommand.GetType().GetMethod("Parse", new[] { typeof(string[]) });
            if (parseMethod != null)
            {
                var parseResult = parseMethod.Invoke(rootCommand, new object[] { args });
                // This is a fallback - just run the convert logic directly for now
                return RunConvert(args);
            }
            
            return RunConvert(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int RunConvert(string[] args)
    {
        // Simple argument parsing for now to get it working
        string? trxFile = null;
        string? output = null;
        List<string> outcomes = new();
        bool skipEmpty = false;
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "convert" && i + 1 < args.Length)
            {
                trxFile = args[i + 1];
                i++;
            }
            else if ((args[i] == "--output" || args[i] == "-o") && i + 1 < args.Length)
            {
                output = args[i + 1];
                i++;
            }
            else if ((args[i] == "--outcome" || args[i] == "-f"))
            {
                // Handle multiple outcomes - they can be space-separated after the flag
                i++; // Move past the flag
                while (i < args.Length && !args[i].StartsWith("-"))
                {
                    outcomes.Add(args[i]);
                    i++;
                }
                i--; // Back up one since the loop will increment
            }
            else if (args[i] == "--skip-empty")
            {
                skipEmpty = true;
            }
            else if (args[i] == "--help" || args[i] == "-h")
            {
                Console.WriteLine("Convert TRX files to Visual Studio Test Playlists");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  trx-to-vsplaylist convert <trx-file> [options]");
                Console.WriteLine();
                Console.WriteLine("Arguments:");
                Console.WriteLine("  <trx-file>  Path to the TRX file to convert");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -o, --output <output>    Path to the output playlist file");
                Console.WriteLine("  -f, --outcome <outcome>  Test outcomes to include");
                Console.WriteLine("  --skip-empty             Do not write empty playlist files");
                Console.WriteLine("  -?, -h, --help           Show help and usage information");
                return 0;
            }
        }

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
        if (outcomes.Count > 0)
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
    }

    public static RootCommand GetRootCommand()
    {
        // This is mainly for testing - the actual parsing logic is in RunConvert for now
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

        return new RootCommand("Convert TRX files to Visual Studio Test Playlists")
        {
            convertCommand
        };
    }
}