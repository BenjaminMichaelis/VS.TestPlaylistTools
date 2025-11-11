using System.CommandLine;
using VS.TestPlaylistTools.PlaylistV1;

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
        RootCommand rootCommand = new("Convert TRX files to Visual Studio Test Playlists");
        rootCommand.Add(CreateConvertCommand());
        rootCommand.Add(CreateMergeCommand());
        return rootCommand;
    }

    private static Command CreateConvertCommand()
    {
        Argument<string[]> trxPaths = new("trx-files");
        trxPaths.Description = "Path(s) to the TRX file(s) to convert. Multiple files can be specified to merge into a single playlist.";
        trxPaths.Arity = ArgumentArity.OneOrMore;

        Option<string> playlistPath = new("--output", "-o");
        playlistPath.Description = "Path to the output playlist file or directory. If not specified, the playlist will be saved in the same directory as the first trx file with the same name but with .playlist extension.";

        Option<string[]> outcomesOption = new("--outcome", "-f");
        outcomesOption.Description = "Test outcomes to include (e.g. Passed Failed Skipped). Can be specified multiple times.";
        outcomesOption.Arity = ArgumentArity.ZeroOrMore;
        outcomesOption.AllowMultipleArgumentsPerToken = true;

        Option<bool> skipEmptyOption = new("--skip-empty");
        skipEmptyOption.Description = "Do not write a playlist file if there are no tests in the playlist.";

        Option<bool> separateOption = new("--separate");
        separateOption.Description = "When multiple TRX files are provided, create separate playlist files for each instead of merging into one. Output path must be a directory.";

        Command convertCommand = new("convert", "Convert TRX file(s) to a Visual Studio Test Playlist")
        {
            trxPaths,
            playlistPath,
            outcomesOption,
            skipEmptyOption,
            separateOption
        };

        convertCommand.SetAction((ParseResult parseResult) =>
        {
            string[]? trxFiles = parseResult.GetValue(trxPaths);
            string? playlistFile = parseResult.GetValue(playlistPath);
            string[]? outcomesRaw = parseResult.GetValue(outcomesOption);
            bool skipEmpty = parseResult.GetValue(skipEmptyOption);
            bool separate = parseResult.GetValue(separateOption);

            if (trxFiles == null || trxFiles.Length == 0)
                throw new ArgumentException("At least one TRX file path must be specified.");

            // Validate all TRX files exist
            foreach (string trxFile in trxFiles)
            {
                if (string.IsNullOrEmpty(trxFile))
                    throw new ArgumentException("TRX file path cannot be null or empty.");

                if (!File.Exists(trxFile))
                    throw new FileNotFoundException($"TRX file not found: {trxFile}");
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

            // Handle separate playlist generation for multiple files
            if (separate && trxFiles.Length > 1)
            {
                // Validate that output is not a file path when using --separate
                if (!string.IsNullOrEmpty(playlistFile) && Path.HasExtension(playlistFile) && !Directory.Exists(playlistFile))
                {
                    throw new ArgumentException($"When using --separate with multiple TRX files, --output must be a directory path, not a file path. Received: '{playlistFile}'");
                }

                string outputDirectory = playlistFile ?? Path.GetDirectoryName(trxFiles[0]) ?? string.Empty;

                // Ensure the output directory exists
                if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                int filesCreated = 0;
                foreach (string trxFile in trxFiles)
                {
                    var playlist = converter.ConvertTrxToPlaylist(trxFile, outcomes ?? []);

                    if (skipEmpty && (playlist.Tests == null || playlist.Tests.Count == 0))
                    {
                        parseResult.InvocationConfiguration.Output.WriteLine($"Skipped '{trxFile}' - no tests found.");
                        continue;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(trxFile);
                    string outputFile = Path.Combine(outputDirectory, $"{fileName}.playlist");

                    converter.ConvertTrxToPlaylistFile(trxFile, outputFile, outcomes ?? []);
                    parseResult.InvocationConfiguration.Output.WriteLine($"Converted '{trxFile}' to playlist '{outputFile}'.");
                    filesCreated++;
                }

                parseResult.InvocationConfiguration.Output.WriteLine($"Created {filesCreated} playlist file(s) from {trxFiles.Length} TRX file(s).");
                return;
            }

            // Handle --separate with single file (just a warning, proceed normally)
            if (separate && trxFiles.Length == 1)
            {
                parseResult.InvocationConfiguration.Output.WriteLine("Warning: --separate flag has no effect with a single TRX file.");
            }

            // Default behavior: merge into single playlist or handle single file
            // Determine the output file path
            if (string.IsNullOrEmpty(playlistFile))
            {
                // No output specified - use default based on first TRX file
                string directory = Path.GetDirectoryName(trxFiles[0]) ?? string.Empty;
                string fileName = Path.GetFileNameWithoutExtension(trxFiles[0]);
                playlistFile = Path.Combine(directory, $"{fileName}.playlist");
            }
            else if (Directory.Exists(playlistFile) && !File.Exists(playlistFile))
            {
                // Output exists as a directory (and not as a file) - generate filename based on first TRX
                string fileName = Path.GetFileNameWithoutExtension(trxFiles[0]);
                playlistFile = Path.Combine(playlistFile, $"{fileName}.playlist");
            }
            else if (!Path.HasExtension(playlistFile) && !File.Exists(playlistFile) && !Directory.Exists(playlistFile))
            {
                // Output path has no extension and doesn't exist - treat as directory
                string fileName = Path.GetFileNameWithoutExtension(trxFiles[0]);
                playlistFile = Path.Combine(playlistFile, $"{fileName}.playlist");
            }
            // else: Output is a file path - use it as-is

            // Use the appropriate method based on number of TRX files
            var mergedPlaylist = trxFiles.Length == 1
                ? converter.ConvertTrxToPlaylist(trxFiles[0], outcomes ?? [])
                : converter.ConvertMultipleTrxToPlaylist(trxFiles, outcomes ?? []);

            if (skipEmpty && (mergedPlaylist.Tests == null || mergedPlaylist.Tests.Count == 0))
            {
                string filesDescription = trxFiles.Length == 1 ? $"'{trxFiles[0]}'" : $"{trxFiles.Length} TRX files";
                parseResult.InvocationConfiguration.Output.WriteLine($"No tests found in {filesDescription}. Playlist file was not created due to --skip-empty.");
                return;
            }

            // Save the playlist
            if (trxFiles.Length == 1)
            {
                converter.ConvertTrxToPlaylistFile(trxFiles[0], playlistFile, outcomes ?? []);
                parseResult.InvocationConfiguration.Output.WriteLine($"Converted '{trxFiles[0]}' to playlist '{playlistFile}'.");
            }
            else
            {
                converter.ConvertMultipleTrxToPlaylistFile(trxFiles, playlistFile, outcomes ?? []);
                parseResult.InvocationConfiguration.Output.WriteLine($"Converted {trxFiles.Length} TRX files to playlist '{playlistFile}' ({mergedPlaylist.TestCount} unique tests).");
            }
        });

        return convertCommand;
    }

    private static Command CreateMergeCommand()
    {
        Argument<string[]> playlistPaths = new("playlist-files");
        playlistPaths.Description = "Path(s) to the playlist file(s) to merge. Tests will be automatically de-duplicated.";
        playlistPaths.Arity = ArgumentArity.OneOrMore;

        Option<string> outputPath = new("--output", "-o");
        outputPath.Description = "Path to the output merged playlist file (required).";

        Option<bool> skipEmptyOption = new("--skip-empty");
        skipEmptyOption.Description = "Do not write a playlist file if there are no tests in the merged playlist.";

        Command mergeCommand = new("merge", "Merge multiple playlist files into a single playlist with de-duplicated tests")
        {
            playlistPaths,
            outputPath,
            skipEmptyOption
        };

        mergeCommand.SetAction((ParseResult parseResult) =>
        {
            string[]? playlistFiles = parseResult.GetValue(playlistPaths);
            string? outputFile = parseResult.GetValue(outputPath);
            bool skipEmpty = parseResult.GetValue(skipEmptyOption);

            if (playlistFiles == null || playlistFiles.Length == 0)
                throw new ArgumentException("At least one playlist file path must be specified.");

            if (string.IsNullOrEmpty(outputFile))
                throw new ArgumentException("Output file path must be specified (use --output or -o).");

            // Validate all playlist files exist
            foreach (string playlistFile in playlistFiles)
            {
                if (string.IsNullOrEmpty(playlistFile))
                    throw new ArgumentException("Playlist file path cannot be null or empty.");

                if (!File.Exists(playlistFile))
                    throw new FileNotFoundException($"Playlist file not found: {playlistFile}");
            }

            // Use a HashSet to automatically de-duplicate test names
            HashSet<string> uniqueTestNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (string playlistFile in playlistFiles)
            {
                PlaylistRoot playlist = PlaylistV1Parser.FromFile(playlistFile);

                foreach (var test in playlist.Tests)
                {
                    if (!string.IsNullOrWhiteSpace(test.Test))
                    {
                        uniqueTestNames.Add(test.Test);
                    }
                }
            }

            if (skipEmpty && uniqueTestNames.Count == 0)
            {
                parseResult.InvocationConfiguration.Output.WriteLine($"No tests found in {playlistFiles.Length} playlist files. Merged playlist file was not created due to --skip-empty.");
                return;
            }

            // Create merged playlist
            PlaylistRoot mergedPlaylist = PlaylistV1Builder.Create(uniqueTestNames.ToList());
            PlaylistV1Builder.SaveToFile(mergedPlaylist, outputFile);

            parseResult.InvocationConfiguration.Output.WriteLine($"Merged {playlistFiles.Length} playlist files into '{outputFile}' ({mergedPlaylist.TestCount} unique tests).");
        });

        return mergeCommand;
    }

    // Keep the old method name for compatibility with tests, but return RootCommand now
    public static RootCommand GetConfiguration()
    {
        return GetRootCommand();
    }
}