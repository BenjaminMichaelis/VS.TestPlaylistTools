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

The CLI uses Native AOT for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`,
`osx-x64`, and `osx-arm64`. The explicit `linux-musl-x64` and `linux-musl-arm64`
packages remain self-contained CoreCLR builds for Alpine. All eight explicit
platform packages run without a separately installed .NET runtime.

Unlisted RIDs use a framework-dependent `any` package targeting .NET 10 with
`RollForward=Major`: it prefers .NET 10 and can use a newer stable major if
.NET 10 is absent. It cannot run on .NET 8 or 9. Tool installation requires the
.NET 10+ SDK; native execution does not.

CI tests the complete installed CLI, independently of the library AOT smoke
application. It discovers every supported stable .NET channel >=10 from official
release metadata and tests the portable package with only that channel's latest
servicing runtime installed. These checks block releases; next-major preview/RC
checks are advisory and opt into prerelease runtime selection only in CI.
Weekly scheduled runs catch runtime updates even without code changes. Future
and end-of-life runtimes, every historical patch, and every OS distribution are
not guaranteed by this policy.

## Quick Start

### Installing the CLI Tool

`dotnet tool install --global trx-to-vsplaylist`

The tool ships self-contained packages for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64` (Alpine), `osx-x64` and `osx-arm64`, so no .NET runtime is required on those platforms. Other platforms fall back to a portable package that requires the .NET 10 runtime.

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

### Package smoke tests

CI and releases share `.github/workflows/tool-packages.yml`: each AOT payload is
packed on its native OS/architecture, while the pointer, portable fallback, and
musl CoreCLR payloads are packed separately. Tested artifacts are combined with
the library packages; every payload must publish successfully before the pointer.
All packages and assembly versions use the same `Version` property.
Nightly and manual non-tag deployments use the next patch version with a
`beta.<run-number>` suffix. They pass through the same native builds, stable
runtime checks, package verification, and consumer smoke tests as tagged releases;
the resolved version is reused for publishing instead of being recalculated.

On a Windows x64 machine with the Native AOT C++ prerequisites, for example:

```powershell
.\eng\pack.ps1 -Mode Pointer -Version 0.0.0-local -Output .\PackedNuget
.\eng\pack.ps1 -Mode Payload -RuntimeIdentifier win-x64 -Version 0.0.0-local -Output .\PackedNuget
.\eng\pack.ps1 -Mode Payload -RuntimeIdentifier any -Version 0.0.0-local -Output .\PackedNuget
.\.github\scripts\Test-ToolPackages.ps1 -Feed .\PackedNuget -Version 0.0.0-local -Partial
.\.github\scripts\Test-InstalledTool.ps1 -Feed .\PackedNuget -Version 0.0.0-local -ExpectedRid win-x64
```

Use `-Mode Libraries` to pack both libraries. After collecting the complete
cross-platform package set, run `eng/verify-packages.ps1` for library
TFMs/dependencies/version parity and `Test-ToolPackages.ps1` without `-Partial`
for the exact tool inventory, architectures, and deployment modes.
`eng/smoke-test-packages.ps1 -Library` additionally consumes the library NuGets
on net8.0 and net10.0.

Pass `-InstallRuntimeId freebsd-x64 -ExpectedRid any` to
`Test-InstalledTool.ps1` to exercise the SDK's portable fallback selection. This
simulates package selection, not FreeBSD execution. Alpine selects an explicit
musl package, not `any`, because its RID graph can otherwise choose glibc Linux.

## 📄 License

MIT License. See [LICENSE](./LICENSE) for details.
