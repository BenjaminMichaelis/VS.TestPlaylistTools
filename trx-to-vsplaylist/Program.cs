using System.CommandLine;

using Microsoft.Extensions.FileSystemGlobbing;

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

    /// <summary>
    /// Determines whether the given path represents a directory or should be treated as one.
    /// </summary>
    /// <param name="path">The path to evaluate.</param>
    /// <returns>
    /// True if the path is an existing directory or should be treated as a directory;
    /// false if it should be treated as a file path.
    /// </returns>
    private static bool IsDirectory(string path)
    {
        // Check if the path exists
        bool pathExists = File.Exists(path) || Directory.Exists(path);

        if (pathExists)
        {
            FileAttributes attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Directory);
        }
        else
        {
            // Check for trailing directory separator as an explicit signal
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString()) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                return true;
            }

            // No extension and no trailing separator
            if (!Path.HasExtension(path))
            {
                return true;
            }

            // Has extension - treat as file
            return false;
        }
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
            else if (IsDirectory(playlistFile))
            {
                // Output is or should be a directory - generate filename based on first TRX
                if (!Directory.Exists(playlistFile))
                {
                    Directory.CreateDirectory(playlistFile);
                }
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
        playlistPaths.Description = "Path(s) to the playlist file(s) to merge. Supports glob patterns (e.g., *.playlist, **/*.playlist). Tests will be automatically de-duplicated.";
        playlistPaths.Arity = ArgumentArity.OneOrMore;

        Option<string> outputPath = new("--output", "-o");
        outputPath.Description = "Path to the output merged playlist file. If not specified and all input files are in the same directory, the merged file will be created in that directory.";

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
            string[]? playlistPatterns = parseResult.GetValue(playlistPaths);
            string? outputFile = parseResult.GetValue(outputPath);
            bool skipEmpty = parseResult.GetValue(skipEmptyOption);

            if (playlistPatterns == null || playlistPatterns.Length == 0)
                throw new ArgumentException("At least one playlist file path or pattern must be specified.");

            // Expand glob patterns to actual file paths
            List<string> playlistFiles = new();
            foreach (string pattern in playlistPatterns)
            {
                if (string.IsNullOrEmpty(pattern))
                    throw new ArgumentException("Playlist file path or pattern cannot be null or empty.");

                // Check if the pattern contains glob characters (* or **)
                // Note: Microsoft.Extensions.FileSystemGlobbing does not support ? wildcard
                if (pattern.Contains('*'))
                {
                    // Use file globbing
                    Matcher matcher = new();

                    // Determine the base directory and glob pattern
                    string directory;
                    string globPattern;
                    
                    // Split the pattern into path segments
                    var segments = pattern.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    int wildcardIndex = -1;
                    for (int i = 0; i < segments.Length; i++)
                    {
                        if (segments[i].Contains('*'))
                        {
                            wildcardIndex = i;
                            break;
                        }
                    }
                    
                    if (wildcardIndex == -1)
                    {
                        // No wildcard found (shouldn't happen given outer if condition, but handle defensively)
                        directory = Path.GetDirectoryName(pattern) ?? Directory.GetCurrentDirectory();
                        globPattern = Path.GetFileName(pattern);
                    }
                    else if (wildcardIndex == 0)
                    {
                        // Wildcard in the first segment - use current directory
                        directory = Directory.GetCurrentDirectory();
                        globPattern = pattern;
                    }
                    else
                    {
                        // Base directory is everything before the first wildcard segment
                        directory = Path.Combine(segments.Take(wildcardIndex).ToArray());
                        if (!Path.IsPathRooted(directory))
                        {
                            directory = Path.Combine(Directory.GetCurrentDirectory(), directory);
                        }
                        
                        // The glob pattern is everything from the first wildcard onward
                        // Use forward slashes for glob patterns as expected by Matcher
                        globPattern = string.Join("/", segments.Skip(wildcardIndex));
                    }

                    matcher.AddInclude(globPattern);
                    IEnumerable<string> matchedFiles = matcher.GetResultsInFullPath(directory);

                    if (!matchedFiles.Any())
                    {
                        throw new FileNotFoundException($"No files matched the pattern: {pattern}");
                    }

                    playlistFiles.AddRange(matchedFiles);
                }
                else
                {
                    // Regular file path - validate it exists
                    if (!File.Exists(pattern))
                        throw new FileNotFoundException($"Playlist file not found: {pattern}");

                    playlistFiles.Add(Path.GetFullPath(pattern));
                }
            }

            // Remove duplicates (in case patterns overlap)
            playlistFiles = playlistFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (playlistFiles.Count == 0)
                throw new ArgumentException("No playlist files found to merge.");

            // Auto-detect output directory if not specified
            if (string.IsNullOrEmpty(outputFile))
            {
                // Check if all files are in the same directory
                string? firstDirectory = Path.GetDirectoryName(Path.GetFullPath(playlistFiles[0]));
                bool allInSameDirectory = playlistFiles.All(f =>
                    string.Equals(Path.GetDirectoryName(Path.GetFullPath(f)), firstDirectory, StringComparison.OrdinalIgnoreCase));

                if (allInSameDirectory && !string.IsNullOrEmpty(firstDirectory))
                {
                    // All files are in the same directory - use that directory
                    outputFile = Path.Combine(firstDirectory, "merged.playlist");
                    parseResult.InvocationConfiguration.Output.WriteLine($"Output directory not specified. Using '{firstDirectory}' (same directory as input files).");
                }
                else
                {
                    // Files are in different directories - require explicit output
                    throw new ArgumentException("Output file path must be specified (use --output or -o) when merging files from different directories.");
                }
            }
            else
            {
                // Output was specified - determine if it's a directory or file path
                if (IsDirectory(outputFile))
                {
                    // It's a directory - create it if needed and use merged.playlist as filename
                    if (!Directory.Exists(outputFile))
                    {
                        Directory.CreateDirectory(outputFile);
                    }
                    outputFile = Path.Combine(outputFile, "merged.playlist");
                }
                // else: It's a file path - use it as-is
            }

            // Validate all playlist files exist (should already be validated, but double-check)
            foreach (string playlistFile in playlistFiles)
            {
                if (!File.Exists(playlistFile))
                    throw new FileNotFoundException($"Playlist file not found: {playlistFile}");
            }

            // Use a HashSet to automatically de-duplicate test names
            HashSet<string> uniqueTestNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (string playlistFile in playlistFiles)
            {
                PlaylistRoot rootPlaylist = PlaylistV1Parser.FromFile(playlistFile);

                foreach (var test in rootPlaylist.Tests)
                {
                    if (!string.IsNullOrWhiteSpace(test.Test))
                    {
                        uniqueTestNames.Add(test.Test);
                    }
                }
            }

            if (skipEmpty && uniqueTestNames.Count == 0)
            {
                parseResult.InvocationConfiguration.Output.WriteLine($"No tests found in {playlistFiles.Count} playlist files. Merged playlist file was not created due to --skip-empty.");
                return;
            }

            // Create merged playlist
            PlaylistRoot mergedPlaylist = PlaylistV1Builder.Create(uniqueTestNames.ToList());
            PlaylistV1Builder.SaveToFile(mergedPlaylist, outputFile);

            parseResult.InvocationConfiguration.Output.WriteLine($"Merged {playlistFiles.Count} playlist files into '{outputFile}' ({mergedPlaylist.TestCount} unique tests).");
        });

        return mergeCommand;
    }

    // Keep the old method name for compatibility with tests, but return RootCommand now
    public static RootCommand GetConfiguration()
    {
        return GetRootCommand();
    }
}