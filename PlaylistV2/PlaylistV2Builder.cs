using System.Text;
using System.Xml;

namespace VS.TestPlaylistTools.PlaylistV2;

/// <summary>
/// Provides functionality to build and serialize Visual Studio Test Playlist V2 format XML files.
/// </summary>
public static class PlaylistV2Builder
{
    /// <summary>
    /// Creates a new playlist with the specified rules.
    /// </summary>
    public static PlaylistRoot Create(IEnumerable<Rule>? rules = null)
    {
        return new PlaylistRoot(rules ?? []);
    }

    /// <summary>
    /// Serializes a playlist to XML string.
    /// </summary>
    public static string ToXmlString(PlaylistRoot playlist)
    {
        if (playlist is null) throw new ArgumentNullException(nameof(playlist));
        return playlist.ToString();
    }

    /// <summary>
    /// Saves a playlist to a file.
    /// </summary>
    public static void SaveToFile(PlaylistRoot playlist, string filePath)
    {
        if (playlist is null) throw new ArgumentNullException(nameof(playlist));
        if (filePath is null) throw new ArgumentNullException(nameof(filePath));
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        WriteToTextWriter(playlist, writer);
    }

    /// <summary>
    /// Writes a playlist to a TextWriter.
    /// </summary>
    public static void WriteToTextWriter(PlaylistRoot playlist, TextWriter writer)
    {
        if (playlist is null) throw new ArgumentNullException(nameof(playlist));
        if (writer is null) throw new ArgumentNullException(nameof(writer));
        playlist.Serialize(writer);
    }

    /// <summary>
    /// Writes a playlist to an XmlWriter.
    /// </summary>
    public static void WriteToXmlWriter(PlaylistRoot playlist, XmlWriter xmlWriter)
    {
        if (playlist is null) throw new ArgumentNullException(nameof(playlist));
        if (xmlWriter is null) throw new ArgumentNullException(nameof(xmlWriter));
        var namespaces = new System.Xml.Serialization.XmlSerializerNamespaces();
        namespaces.Add(string.Empty, string.Empty);
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PlaylistRoot));
        serializer.Serialize(xmlWriter, playlist, namespaces);
    }

    /// <summary>
    /// Creates a hierarchical playlist structure for a specific test
    /// </summary>
    public static PlaylistRoot CreateHierarchicalPlaylist(
        string projectName,
        string namespaceName,
        string className,
        string testWithNormalizedFullyQualifiedName,
        string displayName)
    {
        var rules = new List<Rule>();
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
        rules.Add(includeRule);
        return new PlaylistRoot(rules);
    }

    /// <summary>
    /// Creates a hierarchical playlist structure for multiple tests
    /// </summary>
    public static PlaylistRoot CreateHierarchicalPlaylist(
        string projectName,
        string namespaceName,
        string className,
        IEnumerable<(string testWithNormalizedFullyQualifiedName, string displayName)> tests)
    {
        var testRules = tests.Select(test =>
            BooleanRule.All(
                PropertyRule.TestWithNormalizedFullyQualifiedName(test.testWithNormalizedFullyQualifiedName),
                BooleanRule.Any(
                    PropertyRule.TestWithDisplayName(test.displayName)
                )
            )
        ).ToArray();
        var rules = new List<Rule>();
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
        rules.Add(includeRule);
        return new PlaylistRoot(rules);
    }

    /// <summary>
    /// Builder pattern class for creating playlists fluently.
    /// </summary>
    public class Builder
    {
        private readonly PlaylistRoot _playlistRoot;

        /// <summary>
        /// Initializes a new instance of the Builder class.
        /// </summary>
        public Builder()
        {
            _playlistRoot = new PlaylistRoot();
        }

        /// <summary>
        /// Adds a rule to the playlist being built.
        /// </summary>
        /// <param name="rule">The rule to add.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public Builder AddRule(Rule rule)
        {
            if (rule != null)
                _playlistRoot.Rules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds multiple rules to the playlist being built.
        /// </summary>
        /// <param name="rules">The rules to add.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public Builder AddRules(IEnumerable<Rule> rules)
        {
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    if (rule != null)
                        _playlistRoot.Rules.Add(rule);
                }
            }
            return this;
        }

        /// <summary>
        /// Builds and returns the playlist.
        /// </summary>
        /// <returns>The constructed PlaylistRoot object.</returns>
        public PlaylistRoot Build()
        {
            return _playlistRoot;
        }

        /// <summary>
        /// Builds the playlist and converts it to an XML string.
        /// </summary>
        /// <returns>The XML representation of the playlist.</returns>
        public string ToXmlString()
        {
            return _playlistRoot.ToString();
        }

        /// <summary>
        /// Builds the playlist and saves it to a file.
        /// </summary>
        /// <param name="filePath">The path where to save the playlist file.</param>
        public void SaveToFile(string filePath)
        {
            PlaylistV2Builder.SaveToFile(_playlistRoot, filePath);
        }
    }

    /// <summary>
    /// Creates a new builder instance for fluent playlist construction.
    /// </summary>
    /// <returns>A new Builder instance.</returns>
    public static Builder CreateBuilder()
    {
        return new Builder();
    }
}
