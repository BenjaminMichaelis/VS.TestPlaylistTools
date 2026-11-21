# VSTestPlaylistTools.TrxToPlaylist

A command-line tool for converting TRX test result files into Visual Studio Test Playlist (V1) files. Supports filtering by test outcome, merging multiple TRX files, and combining existing playlists with automatic de-duplication.

## Installation and runtime compatibility

Install using the .NET 10 or newer SDK:

```powershell
dotnet tool install --global trx-to-vsplaylist
```

The SDK selects a Native AOT package on Windows x64/ARM64, glibc Linux x64/ARM64,
and macOS x64/ARM64. Alpine x64/ARM64 has explicit self-contained CoreCLR
packages. These packages do not require a separately installed managed runtime.
AOT binaries are built on matching OS/architecture runners; Linux AOT builds use
Ubuntu 22.04 as their build baseline. Native OS dependencies still apply, including
the supported system C/C++ libraries and ICU where required; AOT is not a
statically linked universal executable. Consult the
[.NET OS prerequisites](https://github.com/dotnet/core/blob/main/Documentation/prereqs.md).

Unlisted platforms use the portable `any` package, which requires .NET 10 or a
newer compatible stable runtime. `RollForward=Major` prefers the targeted major
when available and otherwise allows a newer major; .NET 8/9 cannot run this
net10.0 tool. Compatibility is limited to platforms supported by the runtime.
An installed native binary failing to launch does not trigger a CoreCLR retry.

CI exercises the actual installed native tool on all six AOT platforms and checks
musl installation in Alpine. On all six native runner environments, the same
portable package is exercised against the latest servicing runtime of every
currently supported stable .NET major >=10. Each test uses an isolated runtime
root, with host tracing to confirm runtime selection. Stable failures block
production publication. Next-major preview/RC checks are advisory; prerelease
roll-forward is enabled only for those checks, not for users by default.
The runtime matrix is discovered from official release metadata on PRs, releases,
and weekly scheduled runs; it does not claim coverage of future versions or every
historical servicing patch.

## Building packages

Use `eng/pack.ps1` from the repository root with `-Mode Pointer`, `-Mode Libraries`,
or `-Mode Payload -RuntimeIdentifier <rid>` and one shared `-Version`.
Native payloads require matching build/target operating systems and the
[Native AOT toolchain prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/).
The reusable CI workflow builds native architectures directly rather than relying
on emulation. A plain pack is no longer sufficient to produce the complete feed.
Publish every RID payload at the same version before publishing the pointer.
Native debug files remain in the payload packages; native symbol `.snupkg` files
are disabled because NuGet.org's symbol server accepts only managed Portable PDBs.

## Features

- Convert `.trx` test result files to Visual Studio `.playlist` files (V1 format)
- **Merge multiple TRX files** into a single playlist (automatically de-duplicates tests)
- **Create separate playlists** from multiple TRX files with `--separate` option
- **Merge multiple playlist files** into a single consolidated playlist
- Filter included tests by outcome (e.g., Passed, Failed, Skipped, NotExecuted, etc.)
- Specify output file path or use default naming
- Skip creation of empty playlists with `--skip-empty` option
- Simple, scriptable CLI interface

## Commands

### `convert` - Convert TRX file(s) to a playlist

Converts one or more TRX test result files into a Visual Studio Test Playlist. When multiple TRX files are provided, tests are automatically de-duplicated by default.

#### Arguments

- `<trx-files>`: Path(s) to the TRX file(s) to convert (one or more required)
- `--output`, `-o`: Path to the output playlist file or directory (optional). If not specified, the playlist will be saved in the same directory as the first TRX file with the same name but `.playlist` extension. When a directory is specified, the output file will be created in that directory.
- `--outcome`, `-f`: Test outcomes to include (optional, repeatable). Accepts one or more of: `Passed`, `Failed`, `Skipped`, `NotExecuted`, etc.
- `--skip-empty`: Do not create a playlist file if there are no tests (optional)
- `--separate`: When multiple TRX files are provided, create separate playlist files for each instead of merging into one. **Output path must be a directory** (optional)

#### Examples

Convert a single TRX file with all tests:
```bash
trx-to-vsplaylist convert results.trx
```

Convert a single TRX file to a specific directory:
```bash
trx-to-vsplaylist convert results.trx --output OutputDir
```

Convert only failed tests to a playlist:
```bash
trx-to-vsplaylist convert results.trx --outcome Failed
```

Convert passed and failed tests, specifying output file:
```bash
trx-to-vsplaylist convert results.trx --outcome Passed Failed --output my_playlist.playlist
```

**Merge multiple TRX files** (e.g., from different target frameworks) into a single playlist (default behavior):
```bash
trx-to-vsplaylist convert test-net8.0.trx test-net6.0.trx test-net48.trx --output merged.playlist
```

Merge multiple TRX files with only failed tests:
```bash
trx-to-vsplaylist convert test-net8.0.trx test-net6.0.trx --outcome Failed --output failures.playlist
```

**Create separate playlists** from multiple TRX files (output must be a directory):
```bash
trx-to-vsplaylist convert test-net8.0.trx test-net6.0.trx --output OutputDir --separate
```

Create separate playlists with only failed tests, skipping empty ones:
```bash
trx-to-vsplaylist convert test-net8.0.trx test-net6.0.trx --outcome Failed --separate --skip-empty --output OutputDir
```

Skip creating a playlist if no tests match the filter:
```bash
trx-to-vsplaylist convert results.trx --outcome Failed --skip-empty
```

#### Important Notes

- **Single file mode**: When converting a single TRX file, `--output` can be either a file path or a directory. If it's a directory, the playlist will be created in that directory with the same name as the TRX file.
- **Multiple files + --separate**: When using `--separate` with multiple TRX files, `--output` **must be a directory**, not a file path. This will create one playlist per TRX file in that directory.
- **Multiple files without --separate**: When merging multiple TRX files (default), `--output` can be a file path or directory. If it's a directory, the merged playlist will be created with the name of the first TRX file.

### `merge` - Merge playlist files

Merges multiple existing playlist files into a single playlist with automatic de-duplication of tests. Supports file globbing patterns for easy batch operations.

#### Arguments

- `<playlist-files>`: Path(s) or glob pattern(s) to the playlist file(s) to merge (one or more required). Supports wildcards like `*.playlist` or `**/*.playlist`
- `--output`, `-o`: Path to the output merged playlist file or directory (optional when all input files are in the same directory)
  - If the path exists and is a directory, the merged file will be created as `merged.playlist` in that directory
  - If the path exists and is a file, that file will be overwritten
  - If the path doesn't exist:
    - Paths with a file extension (e.g., `.playlist`) are treated as file paths
    - Paths without an extension are treated as directory paths (the directory will be created)
    - Paths ending with a directory separator (`\` or `/`) are treated as directory paths
- `--skip-empty`: Do not create a playlist file if there are no tests (optional)


#### Glob Pattern Support

The merge command supports file globbing patterns using standard wildcards:

- `*` matches zero or more characters (except directory separators)
- `**` matches any number of directory levels (recursive search)
https://learn.microsoft.com/dotnet/core/extensions/file-globbing?WT.mc_id=8B97120A00B57354

#### Examples

Merge multiple playlist files with explicit output:
```bash
trx-to-vsplaylist merge playlist1.playlist playlist2.playlist playlist3.playlist --output combined.playlist
```

Merge to a specific directory:
```bash
trx-to-vsplaylist merge playlist1.playlist playlist2.playlist --output C:\Users\foo\Downloads\playlists
# Creates C:\Users\foo\Downloads\playlists\merged.playlist
```

Recursively find and merge all playlists:
```bash
trx-to-vsplaylist merge "TestResults\**\*.playlist" --output ./all-frameworks.playlist
```

## Use Cases

### Multi-TFM Projects

When a project targets multiple frameworks (e.g., net8.0, net6.0, net48), running tests generates separate TRX files for each framework. Use the `convert` command with multiple TRX files to create a single, de-duplicated playlist:

```bash
trx-to-vsplaylist convert \
  TestResults/MyProject_net8.0.trx \
  TestResults/MyProject_net6.0.trx \
  TestResults/MyProject_net48.trx \
  --outcome Failed \
  --output failed-tests.playlist
```

Alternatively, create separate playlists for each framework:

```bash
trx-to-vsplaylist convert \
  TestResults/MyProject_net8.0.trx \
  TestResults/MyProject_net6.0.trx \
  TestResults/MyProject_net48.trx \
  --outcome Failed \
  --output TestResults \
  --separate
```

### CI/CD Integration

Generate a playlist of all failed tests across multiple test runs:

**Option 1: Direct conversion (recommended)**
```bash
trx-to-vsplaylist convert proj1-results.trx proj2-results.trx --outcome Failed --output all-failures.playlist
```

**Option 2: Convert then merge**
```bash
# Convert each TRX to a playlist
trx-to-vsplaylist convert proj1-results.trx --outcome Failed --output proj1-failures.playlist
trx-to-vsplaylist convert proj2-results.trx --outcome Failed --output proj2-failures.playlist

# Merge all failure playlists
trx-to-vsplaylist merge proj1-failures.playlist proj2-failures.playlist --output all-failures.playlist
```

### Separate Playlists for Analysis

Create individual playlists for each TRX file while filtering:

```bash
trx-to-vsplaylist convert \
  *.trx \
  --outcome Failed Skipped \
  --output playlists \
  --separate \
  --skip-empty
```

This creates one playlist per TRX file in the `playlists` directory, containing only failed and skipped tests, and skips files with no matching tests.

## Supported Test Outcomes

The following values are supported for the `--outcome` option (case-insensitive):
- `Passed`
- `Failed`
- `Skipped`
- `NotExecuted`
- `Inconclusive`
- `Timeout`
- `Pending`

## De-duplication

When multiple TRX or playlist files are merged, tests are automatically de-duplicated by their fully qualified test name (case-insensitive comparison). This is especially useful when:
- A project targets multiple frameworks and generates duplicate test results
- Tests are run multiple times across different configurations
- Combining playlists from different sources

## License

MIT License. See [LICENSE](../LICENSE) for details.