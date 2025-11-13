# VS.TestPlaylistTools

A collection of .NET libraries and tools for creating, parsing, and manipulating Visual Studio test playlist files.

## Packages

### Tools

- **[GitHub Action](https://github.com/marketplace/actions/trx-to-vs-playlist-converter)** - A GitHub action wrapping the .NET tool
- **[trx-to-vsplaylist](https://www.nuget.org/packages/trx-to-vsplaylist)** - .NET CLI tool for converting TRX files to VS playlists

### Libraries

- **[VSTestPlaylistTools](https://www.nuget.org/packages/VSTestPlaylistTools)** - Unified library with playlist loading utilities
- **[VSTestPlaylistTools.TrxToPlaylistConverter](https://www.nuget.org/packages/VSTestPlaylistTools.TrxToPlaylistConverter)** - Convert TRX test results to playlists

## Quick Start

### Installing the CLI Tool

`dotnet tool install --global trx-to-vsplaylist`

### Converting a TRX file to a VS Playlist

`trx-to-vsplaylist input.trx -o output.playlist`

### Convert failed tests only

`trx-to-vsplaylist input.trx -o failed.playlist --failed-only`

### Using the V2 Playlist Library

`dotnet add package VSTestPlaylistTools.V2Playlist`

```
using VS.TestPlaylistTools.PlaylistV2;
// Create a playlist with rules var playlist = new PlaylistRoot(); playlist.Rules.Add(BooleanRule.Any("MyTests", PropertyRule.Namespace("MyNamespace"), PropertyRule.Trait("Category", "Integration") ));
// Save to file playlist.SaveToFile("MyPlaylist.playlist");
```

### Using the V1 Playlist Library

`dotnet add package VSTestPlaylistTools.V1Playlist`

```
using VS.TestPlaylistTools.PlaylistV1;
// Create a simple V1 playlist var playlist = new Playlist(); playlist.AddTest("MyTest.FullyQualifiedName"); playlist.SaveToFile("MyPlaylist.playlist");
```

## 📚 Documentation

- [PlaylistV2 Documentation](./PlaylistV2/README.md)
- [PlaylistV1 Documentation](./PlaylistV1/README.md)
- [TRX Converter Documentation](./VSTestPlaylistTools.TrxToPlaylistConverter/README.md)
- [Sample Playlists](./PlaylistV2.Tests/SamplePlaylists)

## 🤝 Contributing

Contributions are welcome! Please open issues or submit pull requests on [GitHub](https://github.com/BenjaminMichaelis/VS.TestPlaylistTools).

## 📄 License

MIT License. See [LICENSE](./LICENSE) for details.
