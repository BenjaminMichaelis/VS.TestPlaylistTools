# PlaylistV2 - Visual Studio Test Playlist V2 parser and builder

This folder documents the V2 API exposed from the `VSTestPlaylistTools` package.

## Overview

The V2 API provides functionality to:
- Parse Visual Studio Test Playlist V2.0 XML files
- Create and build new V2 playlist files programmatically using rules
- Serialize playlists to XML strings or files
- Compose complex filter rules using boolean logic (`Any`, `All`, `Not`) and property rules

## Core Types

| Type | Description |
|------|-------------|
| `PlaylistRoot` | Root object representing a V2 playlist |
| `BooleanRule` | Combines child rules with `Any`, `All`, or `Not` logic |
| `PropertyRule` | Matches a test against a single property (namespace, class, trait, etc.) |
| `PlaylistV2Builder` | Serializes and saves playlists; includes a fluent `Builder` |
| `PlaylistV2Parser` | Parses V2 playlists from files, strings, or streams |

## Usage Examples

### Parsing a playlist file

```csharp
using VS.TestPlaylistTools.PlaylistV2;

// Parse from file
var playlist = PlaylistV2Parser.FromFile("myplaylist.playlist");
Console.WriteLine($"Version: {playlist.Version}");

// Parse from string
string xmlContent = "<Playlist Version=\"2.0\">...</Playlist>";
var playlist2 = PlaylistV2Parser.FromString(xmlContent);
```

### Building a playlist with rules

```csharp
using VS.TestPlaylistTools.PlaylistV2;

var playlist = new PlaylistRoot();
playlist.Rules.Add(
    BooleanRule.Any(
        "MyTests",
        PropertyRule.Namespace("MyApp.Tests"),
        PropertyRule.Trait("Integration")));

PlaylistV2Builder.SaveToFile(playlist, "output.playlist");
```

### Fluent builder pattern

```csharp
using VS.TestPlaylistTools.PlaylistV2;

var playlist = PlaylistV2Builder.CreateBuilder()
    .AddRule(BooleanRule.Any(
        PropertyRule.Class("MyTestClass"),
        PropertyRule.Trait("Smoke")))
    .AddRule(BooleanRule.Not(
        PropertyRule.Trait("Slow")))
    .Build();

string xml = PlaylistV2Builder.ToXmlString(playlist);
```

### Convert to XML string

```csharp
using VS.TestPlaylistTools.PlaylistV2;

var playlist = new PlaylistRoot();
playlist.Rules.Add(PropertyRule.Namespace("MyNamespace"));

string xml = PlaylistV2Builder.ToXmlString(playlist);
```

## Rule Types

### `BooleanRule`

Combines multiple child rules with boolean logic:

| Factory method | Match behavior |
|---------------|----------------|
| `BooleanRule.Any(...)` | Include test if **any** child rule matches |
| `BooleanRule.All(...)` | Include test if **all** child rules match |
| `BooleanRule.Not(...)` | Include test if **no** child rules match |

All factory methods accept an optional name as the first `string` argument:

```csharp
BooleanRule.Any("IntegrationSuite",
    PropertyRule.Trait("Integration"),
    PropertyRule.Namespace("MyApp.Integration"));
```

### `PropertyRule`

Matches a single test property. Available factory methods:

| Method | Matches |
|--------|---------|
| `PropertyRule.Namespace(name)` | Namespace of the test class |
| `PropertyRule.Class(name)` | Test class name |
| `PropertyRule.Project(name)` | Project name |
| `PropertyRule.Solution()` | All tests in the solution |
| `PropertyRule.Trait(value)` | Tests with the given trait value |
| `PropertyRule.Test(name)` | Fully qualified test name |
| `PropertyRule.TestWithNormalizedFullyQualifiedName(name)` | Normalized FQN |
| `PropertyRule.TestWithDisplayName(displayName)` | Display name |
| `PropertyRule.ManagedType(name)` | CLR type name |
| `PropertyRule.ManagedMethod(name)` | CLR method name |

> **Note:** `PropertyRule.Trait` matches on trait *value* only (e.g., `Trait("Integration")`). In the VS playlist XML format the trait name is always `"Trait"` and the value contains the actual trait string.

## XML Format

A V2 playlist uses rule-based XML:

```xml
<Playlist Version="2.0">
  <Rule Match="Any" Name="MyTests">
    <Property Name="Namespace" Value="MyApp.Tests" />
    <Property Name="Trait" Value="Integration" />
  </Rule>
</Playlist>
```

Key characteristics:
- Root element is `<Playlist>` with `Version="2.0"`
- `<Rule>` elements carry a `Match` attribute (`Any`, `All`, or `Not`) and optional `Name`
- `<Property>` elements have `Name` and `Value` attributes
- Rules can be arbitrarily nested
