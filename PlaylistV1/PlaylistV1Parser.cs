using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

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
        public static PlaylistRoot FromString(string xmlContent)
        {
            ArgumentNullException.ThrowIfNull(xmlContent);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));
            using var reader = new StreamReader(stream);
            return FromStream(reader);
        }

        /// <summary>
        /// Parses a playlist from a file.
        /// </summary>
        /// <param name="filePath">The path to the playlist file.</param>
        /// <returns>A PlaylistV1 object representing the parsed playlist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        /// <exception cref="InvalidDataException">Thrown when the XML format is invalid.</exception>
        public static PlaylistRoot FromFile(string filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Playlist file not found: {filePath}");

            using var reader = new StreamReader(filePath);
            return FromStream(reader);
        }

        /// <summary>
        /// Parses a playlist from a TextReader.
        /// </summary>
        /// <param name="reader">The TextReader containing the XML content.</param>
        /// <returns>A PlaylistV1 object representing the parsed playlist.</returns>
        /// <exception cref="InvalidDataException">Thrown when the XML format is invalid.</exception>
        public static PlaylistRoot FromStream(TextReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            try
            {
                using var xmlReader = XmlReader.Create(reader, new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                });

                // Validate that this is a Playlist element
                if (!xmlReader.IsStartElement("Playlist"))
                {
                    throw new InvalidDataException("Root element must be 'Playlist'.");
                }

                // Use XML serialization to deserialize the playlist
                var serializer = new XmlSerializer(typeof(PlaylistRoot));
                var playlist = (PlaylistRoot)serializer.Deserialize(xmlReader)!;

                return playlist;
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException($"Invalid XML format: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates whether the given XML content represents a valid V1 playlist.
        /// </summary>
        /// <param name="xmlContent">The XML content to validate.</param>
        /// <returns>True if the content is a valid V1 playlist; otherwise, false.</returns>
        public static bool IsValidPlaylist(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                return false;

            try
            {
                FromString(xmlContent);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}