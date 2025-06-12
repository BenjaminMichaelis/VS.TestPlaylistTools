using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

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
    /// Parses a playlist from a file path
    /// </summary>
    public static Playlist FromFile(string filePath)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        return FromStream(reader);
    }

    /// <summary>
    /// Parses a playlist from an XmlReader
    /// </summary>
    public static Playlist FromXmlReader(XmlReader xmlReader)
    {
        if (xmlReader == null)
            throw new ArgumentNullException(nameof(xmlReader));
        if (!xmlReader.IsStartElement("Playlist"))
            throw new InvalidDataException("Invalid playlist format: Root element must be 'Playlist'");
        var version = xmlReader.GetAttribute("Version");
        if (version == "1.0")
            throw new NotSupportedException("Playlist V1 format is not supported by this parser");
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PlaylistRoot));
        var root = (PlaylistRoot)serializer.Deserialize(xmlReader)!;
        return new Playlist(root);
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
    /// Serializes the playlist to a file
    /// </summary>
    public void SaveToFile(string filePath)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        Serialize(writer);
    }

    /// <summary>
    /// Serializes the playlist to a TextWriter
    /// </summary>
    public void WriteToTextWriter(TextWriter writer)
    {
        Serialize(writer);
    }

    /// <summary>
    /// Serializes the playlist to an XmlWriter
    /// </summary>
    public void WriteToXmlWriter(XmlWriter xmlWriter)
    {
        if (xmlWriter == null)
            throw new ArgumentNullException(nameof(xmlWriter));
        var namespaces = new System.Xml.Serialization.XmlSerializerNamespaces();
        namespaces.Add(string.Empty, string.Empty);
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PlaylistRoot));
        serializer.Serialize(xmlWriter, _root, namespaces);
    }

    /// <summary>
    /// Checks if the given XML content is a valid V2 playlist
    /// </summary>
    public static bool IsValidV2Playlist(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return false;
        try
        {
            var root = PlaylistRoot.FromString(xmlContent);
            return root.Version == "2.0";
        }
        catch
        {
            return false;
        }
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
        /// <returns>The constructed Playlist object.</returns>
        public Playlist Build()
        {
            return new Playlist(_playlistRoot);
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
            var playlist = Build();
            playlist.SaveToFile(filePath);
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