using PlaylistV1 = VS.TestPlaylistTools.PlaylistV1;
using PlaylistV2 = VS.TestPlaylistTools.PlaylistV2;
using VSTestPlaylistTools.TrxToPlaylist;

// --- V1: object creation and round-trip serialization ---

PlaylistV1.PlaylistRoot v1Playlist = PlaylistV1.PlaylistV1Builder.Create(["Ns.Class.Method1", "Ns.Class.Method2"]);

if (v1Playlist.Tests.Count != 2)
{
    Console.Error.WriteLine($"FAIL: Expected 2 tests, got {v1Playlist.Tests.Count}");
    return 1;
}

string v1Xml = PlaylistV1.PlaylistV1Builder.ToXmlString(v1Playlist);

if (!v1Xml.Contains("Ns.Class.Method1") || !v1Xml.Contains("Ns.Class.Method2"))
{
    Console.Error.WriteLine("FAIL: V1 XML missing expected test names");
    return 2;
}

PlaylistV1.PlaylistRoot v1Roundtrip = PlaylistV1.PlaylistV1Parser.FromString(v1Xml);

if (v1Roundtrip.Tests.Count != 2)
{
    Console.Error.WriteLine($"FAIL: V1 round-trip expected 2 tests, got {v1Roundtrip.Tests.Count}");
    return 3;
}

// --- V2: object creation and round-trip serialization ---

PlaylistV2.PlaylistRoot v2Playlist = PlaylistV2.PlaylistV2Builder.Create([
    PlaylistV2.BooleanRule.Any("Includes",
        PlaylistV2.BooleanRule.All(
            PlaylistV2.PropertyRule.Solution(),
            PlaylistV2.BooleanRule.Any(
                PlaylistV2.BooleanRule.All(
                    PlaylistV2.PropertyRule.Project("MyProject"),
                    PlaylistV2.BooleanRule.Any(
                        PlaylistV2.PropertyRule.Class("MyClass")
                    )
                )
            )
        )
    )
]);

string v2Xml = PlaylistV2.PlaylistV2Builder.ToXmlString(v2Playlist);

if (!v2Xml.Contains("MyProject") || !v2Xml.Contains("MyClass"))
{
    Console.Error.WriteLine("FAIL: V2 XML missing expected values");
    return 4;
}

PlaylistV2.PlaylistRoot v2Roundtrip = PlaylistV2.PlaylistV2Parser.FromString(v2Xml);

if (v2Roundtrip.Rules.Count != 1)
{
    Console.Error.WriteLine($"FAIL: V2 round-trip expected 1 root rule, got {v2Roundtrip.Rules.Count}");
    return 5;
}

// --- Converter: exercise code path (FileNotFoundException expected) ---

var converter = new TrxToPlaylistConverter();

try
{
    converter.ConvertTrxToPlaylist("nonexistent-file-that-does-not-exist.trx");
    Console.Error.WriteLine("FAIL: Expected FileNotFoundException was not thrown");
    return 6;
}
catch (FileNotFoundException)
{
    // Expected
}

Console.WriteLine("AOT smoke test passed.");
return 0;
