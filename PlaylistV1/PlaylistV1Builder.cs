using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using PlaylistV1.Models;

namespace PlaylistV1
{
    /// <summary>
    /// Provides functionality to build and serialize Visual Studio Test Playlist V1 format XML files.
    /// </summary>
    public static class PlaylistV1Builder
    {
        /// <summary>
        /// Creates a new playlist with the specified test names.
        /// </summary>
        /// <param name="testNames">The test names to include in the playlist.</param>
        /// <returns>A new PlaylistV1 object.</returns>
        public static PlaylistRoot Create(IEnumerable<string>? testNames = null)
        {
            return new PlaylistRoot(testNames ?? Array.Empty<string>());
        }

        /// <summary>
        /// Creates a new playlist with the specified test names.
        /// </summary>
        /// <param name="testNames">The test names to include in the playlist.</param>
        /// <returns>A new PlaylistV1 object.</returns>
        public static PlaylistRoot Create(params string[] testNames)
        {
            return new PlaylistRoot(testNames ?? Array.Empty<string>());
        }

        /// <summary>
        /// Serializes a playlist to XML string.
        /// </summary>
        /// <param name="playlist">The playlist to serialize.</param>
        /// <returns>The XML representation of the playlist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when playlist is null.</exception>
        public static string ToXmlString(PlaylistRoot playlist)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist));

            using var stringWriter = new StringWriter();
            WriteToTextWriter(playlist, stringWriter);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Saves a playlist to a file.
        /// </summary>
        /// <param name="playlist">The playlist to save.</param>
        /// <param name="filePath">The path where to save the playlist file.</param>
        /// <exception cref="ArgumentNullException">Thrown when playlist or filePath is null.</exception>
        public static void SaveToFile(PlaylistRoot playlist, string filePath)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist));
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            // Ensure directory exists
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
        /// <param name="playlist">The playlist to write.</param>
        /// <param name="writer">The TextWriter to write to.</param>
        /// <exception cref="ArgumentNullException">Thrown when playlist or writer is null.</exception>
        public static void WriteToTextWriter(PlaylistRoot playlist, TextWriter writer)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                CloseOutput = false,
                Indent = true,
                IndentChars = "    ",
                CheckCharacters = false
            });

            WriteToXmlWriter(playlist, xmlWriter);
        }

        /// <summary>
        /// Writes a playlist to an XmlWriter.
        /// </summary>
        /// <param name="playlist">The playlist to write.</param>
        /// <param name="xmlWriter">The XmlWriter to write to.</param>
        /// <exception cref="ArgumentNullException">Thrown when playlist or xmlWriter is null.</exception>
        public static void WriteToXmlWriter(PlaylistRoot playlist, XmlWriter xmlWriter)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist));
            if (xmlWriter == null)
                throw new ArgumentNullException(nameof(xmlWriter));

            // Create XML serializer namespaces to avoid default namespaces
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            var serializer = new XmlSerializer(typeof(PlaylistRoot));
            serializer.Serialize(xmlWriter, playlist, namespaces);
        }

        /// <summary>
        /// Builder pattern class for creating playlists fluently.
        /// </summary>
        public class Builder
        {
            private readonly PlaylistRoot _playlist;

            /// <summary>
            /// Initializes a new instance of the Builder class.
            /// </summary>
            public Builder()
            {
                _playlist = new PlaylistRoot();
            }

            /// <summary>
            /// Adds a test to the playlist being built.
            /// </summary>
            /// <param name="testName">The fully qualified name of the test to add.</param>
            /// <returns>This builder instance for method chaining.</returns>
            public Builder AddTest(string testName)
            {
                _playlist.AddTest(testName);
                return this;
            }

            /// <summary>
            /// Adds multiple tests to the playlist being built.
            /// </summary>
            /// <param name="testNames">The test names to add.</param>
            /// <returns>This builder instance for method chaining.</returns>
            public Builder AddTests(IEnumerable<string> testNames)
            {
                if (testNames != null)
                {
                    foreach (var testName in testNames)
                    {
                        _playlist.AddTest(testName);
                    }
                }
                return this;
            }

            /// <summary>
            /// Adds multiple tests to the playlist being built.
            /// </summary>
            /// <param name="testNames">The test names to add.</param>
            /// <returns>This builder instance for method chaining.</returns>
            public Builder AddTests(params string[] testNames)
            {
                return AddTests((IEnumerable<string>)testNames);
            }

            /// <summary>
            /// Builds and returns the playlist.
            /// </summary>
            /// <returns>The constructed PlaylistV1 object.</returns>
            public PlaylistRoot Build()
            {
                return _playlist;
            }

            /// <summary>
            /// Builds the playlist and converts it to an XML string.
            /// </summary>
            /// <returns>The XML representation of the playlist.</returns>
            public string ToXmlString()
            {
                return PlaylistV1Builder.ToXmlString(_playlist);
            }

            /// <summary>
            /// Builds the playlist and saves it to a file.
            /// </summary>
            /// <param name="filePath">The path where to save the playlist file.</param>
            public void SaveToFile(string filePath)
            {
                PlaylistV1Builder.SaveToFile(_playlist, filePath);
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
}