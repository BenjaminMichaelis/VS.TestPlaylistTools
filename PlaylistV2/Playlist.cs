using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlaylistV2;

/// <summary>
/// High-level interface for working with playlists
/// </summary>
public class Playlist
{
    private readonly PlaylistRoot _root;

    /// <summary>
    /// Gets the underlying playlist root
    /// </summary>
    public PlaylistRoot Root => _root;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Playlist()
    {
        _root = new PlaylistRoot();
    }

    /// <summary>
    /// Constructor with existing root
    /// </summary>
    public Playlist(PlaylistRoot root)
    {
        _root = root;
    }

    /// <summary>
    /// Parses a playlist from XML string
    /// </summary>
    public static Playlist FromString(string xml)
    {
        return new Playlist(PlaylistRoot.FromString(xml));
    }

    /// <summary>
    /// Parses a playlist from a TextReader
    /// </summary>
    public static Playlist FromStream(TextReader reader)
    {
        return new Playlist(PlaylistRoot.FromStream(reader));
    }

    /// <summary>
    /// Serializes the playlist to XML string
    /// </summary>
    public override string ToString()
    {
        return _root.ToString();
    }

    /// <summary>
    /// Serializes the playlist to a TextWriter
    /// </summary>
    public void Serialize(TextWriter writer)
    {
        _root.Serialize(writer);
    }

    /// <summary>
    /// Creates a hierarchical playlist structure for a specific test
    /// </summary>
    public static Playlist CreateHierarchicalPlaylist(
        string projectName, 
        string namespaceName, 
        string className,
        string testWithNormalizedFullyQualifiedName,
        string displayName)
    {
        var playlist = new Playlist();

        // Create the hierarchical structure: Solution -> Project -> Namespace -> Class -> Test
        var includeRule = BooleanRule.Any("Includes",
            BooleanRule.All(
                PropertyRule.Solution(),
                BooleanRule.Any(
                    BooleanRule.All(
                        PropertyRule.Project(projectName),
                        BooleanRule.Any(
                            BooleanRule.All(
                                PropertyRule.Namespace(namespaceName),
                                BooleanRule.Any(
                                    BooleanRule.All(
                                        PropertyRule.Class(className),
                                        BooleanRule.Any(
                                            BooleanRule.All(
                                                PropertyRule.TestWithNormalizedFullyQualifiedName(testWithNormalizedFullyQualifiedName),
                                                BooleanRule.Any(
                                                    PropertyRule.TestWithDisplayName(displayName)
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );

        playlist._root.Rules.Add(includeRule);
        return playlist;
    }

    /// <summary>
    /// Creates a hierarchical playlist structure for multiple tests
    /// </summary>
    public static Playlist CreateHierarchicalPlaylist(
        string projectName,
        string namespaceName,
        string className,
        IEnumerable<(string testWithNormalizedFullyQualifiedName, string displayName)> tests)
    {
        var playlist = new Playlist();

        var testRules = tests.Select(test =>
            BooleanRule.All(
                PropertyRule.TestWithNormalizedFullyQualifiedName(test.testWithNormalizedFullyQualifiedName),
                BooleanRule.Any(
                    PropertyRule.TestWithDisplayName(test.displayName)
                )
            )
        ).ToArray();

        // Create the hierarchical structure: Solution -> Project -> Namespace -> Class -> Tests
        var includeRule = BooleanRule.Any("Includes",
            BooleanRule.All(
                PropertyRule.Solution(),
                BooleanRule.Any(
                    BooleanRule.All(
                        PropertyRule.Project(projectName),
                        BooleanRule.Any(
                            BooleanRule.All(
                                PropertyRule.Namespace(namespaceName),
                                BooleanRule.Any(
                                    BooleanRule.All(
                                        PropertyRule.Class(className),
                                        BooleanRule.Any(testRules)
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );

        playlist._root.Rules.Add(includeRule);
        return playlist;
    }
}