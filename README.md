# VS.TestPlaylistTools

A collection of .NET libraries and tools for creating, parsing, and manipulating Visual Studio test playlist files.

## Packages

### Tools

- **[GitHub Action](https://github.com/marketplace/actions/trx-to-vs-playlist-converter)** - A GitHub action wrapping the .NET tool
- **[trx-to-vsplaylist](https://www.nuget.org/packages/trx-to-vsplaylist)** - .NET CLI tool for converting TRX files to VS playlists

### Libraries

- **[VSTestPlaylistTools](https://www.nuget.org/packages/VSTestPlaylistTools)** - Playlist V1/V2 types, builders/parsers, and unified loading utilities
- **[VSTestPlaylistTools.TrxToPlaylistConverter](https://www.nuget.org/packages/VSTestPlaylistTools.TrxToPlaylistConverter)** - Library for converting TRX test results to V1 playlists

## AOT compatibility status

Both `VSTestPlaylistTools` and `VSTestPlaylistTools.TrxToPlaylistConverter` are **fully AOT-compatible**:

- Both multi-target `netstandard2.1`, `net8.0`, and `net10.0`
- Both enable `IsAotCompatible=true` on their `net8.0+` targets (AOT support requires .NET 8 or later)
- All `XmlSerializer` usage has been replaced with manual `XmlWriter`/`XmlReader` throughout both packages
- Both packages pass full AOT publishing and smoke testing in CI

## Quick Start

### Installing the CLI Tool

`dotnet tool install --global trx-to-vsplaylist`

### Converting a TRX file to a VS Playlist

`trx-to-vsplaylist convert input.trx -o output.playlist`

### Convert failed tests only

`trx-to-vsplaylist convert input.trx -o failed.playlist --outcome Failed`

### Using the V2 Playlist API

`dotnet add package VSTestPlaylistTools`

```csharp
using VS.TestPlaylistTools.PlaylistV2;

var playlist = new PlaylistRoot();
playlist.Rules.Add(
    BooleanRule.Any(
        "MyTests",
        PropertyRule.Namespace("MyNamespace"),
        PropertyRule.Trait("Integration"))); // matches tests with trait value "Integration"

PlaylistV2Builder.SaveToFile(playlist, "MyPlaylist.playlist");
```

### Using the V1 Playlist API

`dotnet add package VSTestPlaylistTools`

```csharp
using VS.TestPlaylistTools.PlaylistV1;

var playlist = PlaylistV1Builder.Create(["MyTest.FullyQualifiedName"]);
PlaylistV1Builder.SaveToFile(playlist, "MyPlaylist.playlist");
```

### Parsing a V2 Playlist

```csharp
using VS.TestPlaylistTools.PlaylistV2;

var playlist = PlaylistV2Parser.FromFile("MyPlaylist.playlist");
foreach (var rule in playlist.Rules)
    Console.WriteLine(rule);
```

### Loading a Playlist (auto-detect V1 or V2)

Use `PlaylistLoader` when you don't know the playlist version ahead of time:

```csharp
using VS.TestPlaylistTools;

// Returns IPlaylistRoot — works with both V1 and V2 files
IPlaylistRoot playlist = PlaylistLoader.Load("MyPlaylist.playlist");
Console.WriteLine($"Version: {playlist.Version}");
```

## 📚 Documentation

- [PlaylistV2 API Documentation](./VSTestPlaylistTools/PlaylistV2/README.md)
- [PlaylistV1 Documentation](./PlaylistV1/README.md)
- [TRX Converter CLI Documentation](./trx-to-vsplaylist/README.md)
- [Sample Playlists](./PlaylistV2.Tests/SamplePlaylists)

## 🤝 Contributing

Contributions are welcome! Please open issues or submit pull requests on [GitHub](https://github.com/BenjaminMichaelis/VS.TestPlaylistTools).

## 📄 License

MIT License. See [LICENSE](./LICENSE) for details.
