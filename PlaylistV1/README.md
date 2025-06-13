# PlaylistV1 - Visual Studio Test Playlist V1 Parser and Builder

This folder contains a clean implementation of a parser and builder for Visual Studio Test Playlist Version 1.0 format.

## Overview

The PlaylistV1 implementation provides functionality to:
- Parse Visual Studio Test Playlist V1.0 XML files
- Create and build new playlist files programmatically
- Validate playlist file format
- Perform round-trip conversions (parse and regenerate identical XML)

## Usage Examples

### Parsing a Playlist File

```csharp
using PlaylistV1;

// Parse from file
var playlist = PlaylistV1Parser.ParseFromFile("myplaylist.playlist");
Console.WriteLine($"Found {playlist.TestCount} tests");

// Parse from string
string xmlContent = "<Playlist Version=\"1.0\"><Add Test=\"MyTest\" /></Playlist>";
var playlist2 = PlaylistV1Parser.ParseFromString(xmlContent);
```

### Building a Playlist

```csharp
using PlaylistV1;

// Create a simple playlist
var playlist = PlaylistV1Builder.Create("Test1", "Test2", "Test3");

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

### Working with Playlist Objects

```csharp
var playlist = new PlaylistV1.Models.PlaylistV1();

// Add tests
playlist.AddTest("MyTest1");
playlist.AddTest("MyTest2");

// Remove tests
playlist.RemoveTest("MyTest1");

// Check test count
Console.WriteLine($"Playlist contains {playlist.TestCount} tests");
```

## XML Format

The V1 playlist format is simple and consists of:

```xml
<Playlist Version="1.0">
    <Add Test="FullyQualifiedTestName1" />
    <Add Test="FullyQualifiedTestName2" />
    <!-- ... more tests ... -->
</Playlist>
```

Key characteristics:
- Root element is `<Playlist>` with `Version="1.0"`
- Each test is represented by an `<Add>` element with a `Test` attribute
- Test names should be fully qualified (e.g., `Namespace.Class.Method`)