# VSTestPlaylistTools.TrxToPlaylist

A command-line tool for converting TRX test result files into Visual Studio Test Playlist (V1) files. Supports filtering by test outcome and flexible output options.

## Features

- Convert `.trx` test result files to Visual Studio `.playlist` files (V1 format)
- Filter included tests by outcome (e.g., Passed, Failed, Skipped, NotExecuted, etc.)
- Specify output file path or use default naming
- Simple, scriptable CLI interface

### Arguments

- `<trx-file>`: Path to the TRX file to convert (required)
- `--output`, `-o`: Path to the output playlist file (optional). If not specified, the playlist will be saved in the same directory as the TRX file with the same name but `.playlist` extension.
- `--outcome`, `-f`: Test outcomes to include (optional, repeatable). Accepts one or more of: `Passed`, `Failed`, `Skipped`, `NotExecuted`, etc.

### Examples

Convert all tests from a TRX file:
```bash
-- convert results.trx
```

Convert only failed tests to a playlist:
```bash
-- convert results.trx --outcome Failed
```

Convert passed and failed tests, specifying output file:
```bash
-- convert results.trx --outcome Passed --outcome Failed --output my_playlist.playlist
```
or 
```bash
-- convert results.trx --output my_playlist.playlist --outcome Passed Failed
```

## Supported Test Outcomes

The following values are supported for the `--outcome` option (case-insensitive):
- Passed
- Failed
- NotExecuted
- Inconclusive
- Timeout
- Pending

## License

MIT License. See [LICENSE](../LICENSE) for details.