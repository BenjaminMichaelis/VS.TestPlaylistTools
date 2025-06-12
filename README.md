# VSPlaylistBuilder

A .NET library for creating, parsing, and manipulating Visual Studio test playlist files (V1 and V2).
Easily build, serialize, and manage playlists for use with Visual Studio Test Explorer and automated test runners.

## Features

- Create Visual Studio test playlists (V1 and V2 formats)
- Parse and manipulate existing playlist files
- Fluent builder API for programmatic playlist construction
- Serialize playlists to XML or save directly to file
- Supports advanced rule-based playlist definitions

## Installation

Install via NuGet:
dotnet add package VSPlaylistBuilder
Or via the NuGet Package Manager:
PM> Install-Package VSPlaylistBuilder
## Usage

### Creating a V2 Playlist
using PlaylistV2;

// Create a simple playlist with rules
var playlist = PlaylistV2Builder.Create(new[] { /* your rules here */ });

// Serialize to XML string
string xml = PlaylistV2Builder.ToXmlString(playlist);

// Save to file
PlaylistV2Builder.SaveToFile(playlist, "MyPlaylist.playlist");
### Using the Fluent Builder
var builder = PlaylistV2Builder.CreateBuilder();
builder.AddRule(/* your rule */);
var playlist = builder.Build();
builder.SaveToFile("MyPlaylist.playlist");
### Parsing an Existing Playlist
using PlaylistV2;

// Load and parse a playlist file
var playlist = PlaylistV2Parser.ParseFromFile("Existing.playlist");

## Documentation

- [API Reference](https://github.com/BenjaminMichaelis/VSPlaylistBuilder)
- [Sample Playlists](./PlaylistV2.Tests/SamplePlaylists)

## Contributing

Contributions are welcome! Please open issues or submit pull requests on [GitHub](https://github.com/BenjaminMichaelis/VSPlaylistBuilder).

## License

MIT License. See [LICENSE](./LICENSE) for details.
