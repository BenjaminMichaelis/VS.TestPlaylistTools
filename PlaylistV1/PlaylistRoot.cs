using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace PlaylistV1
{
    /// <summary>
    /// Represents a Visual Studio Test Playlist Version 1.0
    /// </summary>
    [XmlRoot("Playlist")]
    public class PlaylistRoot
    {
        /// <summary>
        /// The version of the playlist format. Always "1.0" for V1 playlists.
        /// </summary>
        [XmlAttribute("Version")]
        public string Version
        {
            get => _version;
            set
            {
                if (value != "1.0")
                    throw new InvalidOperationException($"Playlist V1 must have Version=1.0, but got {value}.");
                _version = value;
            }
        }
        private string _version = "1.0";

        /// <summary>
        /// The collection of tests to be included in this playlist.
        /// </summary>
        [XmlElement("Add")]
        public List<AddElement> Tests { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the PlaylistV1 class.
        /// </summary>
        public PlaylistRoot()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PlaylistV1 class with the specified tests.
        /// </summary>
        /// <param name="testNames">The test names to include in the playlist.</param>
        public PlaylistRoot(IEnumerable<string> testNames)
        {
            ArgumentNullException.ThrowIfNull(testNames);

            foreach (var testName in testNames)
            {
                Tests.Add(new AddElement(testName ?? string.Empty));
            }
        }

        /// <summary>
        /// Adds a test to the playlist.
        /// </summary>
        /// <param name="testName">The fully qualified name of the test to add.</param>
        public void AddTest(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                throw new ArgumentException("Test name cannot be null or empty.", nameof(testName));

            Tests.Add(new AddElement(testName));
        }

        /// <summary>
        /// Removes a test from the playlist.
        /// </summary>
        /// <param name="testName">The fully qualified name of the test to remove.</param>
        /// <returns>True if the test was found and removed; otherwise, false.</returns>
        public bool RemoveTest(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                return false;

            return Tests.RemoveAll(t => string.Equals(t.Test, testName, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Gets the count of tests in the playlist.
        /// </summary>
        public int TestCount => Tests.Count;

        /// <summary>
        /// Serializes the playlist to XML string
        /// </summary>
        public override string ToString()
        {
            using var stringWriter = new StringWriter();
            Serialize(stringWriter);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Serializes the playlist to a TextWriter
        /// </summary>
        public void Serialize(TextWriter writer)
        {
            using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                CloseOutput = false,
                Indent = true
            });

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            var serializer = new XmlSerializer(typeof(PlaylistRoot));
            serializer.Serialize(xmlWriter, this, namespaces);
        }
    }
}