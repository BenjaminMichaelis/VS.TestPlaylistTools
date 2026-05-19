# PlaylistV1 - Visual Studio Test Playlist V1 parser and builder

This folder documents the V1 API exposed from the `VSTestPlaylistTools` package.

## Overview

The V1 API provides functionality to:
- Parse Visual Studio Test Playlist V1.0 XML files
- Create and build new playlist files programmatically
- Validate playlist file format
- Perform round-trip conversions (parse and regenerate XML)

## Usage examples

### Parsing a playlist file

```csharp
using VS.TestPlaylistTools.PlaylistV1;

// Parse from file
var playlist = PlaylistV1Parser.FromFile("myplaylist.playlist");
Console.WriteLine($"Found {playlist.TestCount} tests");

// Parse from string
string xmlContent = "<Playlist Version=\"1.0\"><Add Test=\"MyTest\" /></Playlist>";
var playlist2 = PlaylistV1Parser.FromString(xmlContent);
```

### Building a playlist

```csharp
using VS.TestPlaylistTools.PlaylistV1;

// Create a simple playlist
var playlist = PlaylistV1Builder.Create(["Test1", "Test2", "Test3"]);

// Use fluent builder pattern
var playlist2 = PlaylistV1Builder.CreateBuilder()
    .AddTest("MyNamespace.MyClass.TestMethod1")
    .AddTest("MyNamespace.MyClass.TestMethod2")
    .AddTests("Test3", "Test4", "Test5")
    .Build();

// Convert to XML
string xml = PlaylistV1Builder.ToXmlString(playlist);

// Save to file
PlaylistV1Builder.SaveToFile(playlist, "output.playlist");
```

### Working with playlist objects

```csharp
using VS.TestPlaylistTools.PlaylistV1;

var playlist = new PlaylistRoot();

// Add tests
playlist.AddTest("MyTest1");
playlist.AddTest("MyTest2");

// Remove tests
playlist.RemoveTest("MyTest1");

// Check test count
Console.WriteLine($"Playlist contains {playlist.TestCount} tests");
```

## XML format

The V1 playlist format is simple and consists of:

```xml
<Playlist Version="1.0">
    <Add Test="FullyQualifiedTestName1" />
    <Add Test="FullyQualifiedTestName2" />
</Playlist>
```

Key characteristics:
- Root element is `<Playlist>` with `Version="1.0"`
- Each test is represented by an `<Add>` element with a `Test` attribute
- Test names should be fully qualified (for example: `Namespace.Class.Method`)
