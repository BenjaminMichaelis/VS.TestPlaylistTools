using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using PlaylistV1.Models;

namespace PlaylistV1
{
    /// <summary>
    /// Provides functionality to parse Visual Studio Test Playlist V1 format XML files.
    /// </summary>
    public static class PlaylistV1Parser
    {
        /// <summary>
        /// Parses a playlist from XML string content.
        /// </summary>
        /// <param name="xmlContent">The XML content to parse.</param>
        /// <returns>A PlaylistV1 object representing the parsed playlist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when xmlContent is null.</exception>
        /// <exception cref="InvalidDataException">Thrown when the XML format is invalid.</exception>
        public static Models.PlaylistV1 ParseFromString(string xmlContent)
        {
            if (xmlContent == null)
                throw new ArgumentNullException(nameof(xmlContent));

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));
            using var reader = new StreamReader(stream);
            return Parse(reader);
        }

        /// <summary>
        /// Parses a playlist from a file.
        /// </summary>
        /// <param name="filePath">The path to the playlist file.</param>
        /// <returns>A PlaylistV1 object representing the parsed playlist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        /// <exception cref="InvalidDataException">Thrown when the XML format is invalid.</exception>
        public static Models.PlaylistV1 ParseFromFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Playlist file not found: {filePath}");

            using var reader = new StreamReader(filePath);
            return Parse(reader);
        }

        /// <summary>
        /// Parses a playlist from a TextReader.
        /// </summary>
        /// <param name="reader">The TextReader containing the XML content.</param>
        /// <returns>A PlaylistV1 object representing the parsed playlist.</returns>
        /// <exception cref="InvalidDataException">Thrown when the XML format is invalid.</exception>
        public static Models.PlaylistV1 Parse(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            try
            {
                using var xmlReader = XmlReader.Create(reader, new XmlReaderSettings
                {
                    CloseInput = false,
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                });

                // Validate that this is a Playlist element
                if (!xmlReader.IsStartElement("Playlist"))
                {
                    throw new InvalidDataException("Root element must be 'Playlist'.");
                }

                // Validate that this is version 1.0
                var version = xmlReader.GetAttribute("Version");
                if (!string.Equals(version, "1.0", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException($"Only Playlist version 1.0 is supported. Found version: {version ?? "null"}");
                }

                // Use XML serialization to deserialize the playlist
                var serializer = new XmlSerializer(typeof(Models.PlaylistV1));
                var playlist = (Models.PlaylistV1)serializer.Deserialize(xmlReader)!;

                return playlist;
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException($"Invalid XML format: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException($"Invalid playlist format: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates whether the given XML content represents a valid V1 playlist.
        /// </summary>
        /// <param name="xmlContent">The XML content to validate.</param>
        /// <returns>True if the content is a valid V1 playlist; otherwise, false.</returns>
        public static bool IsValidV1Playlist(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                return false;

            try
            {
                ParseFromString(xmlContent);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}