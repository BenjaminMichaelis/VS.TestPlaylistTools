using System.Text;
using System.Xml;

namespace VS.TestPlaylistTools.PlaylistV1
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
            if (xmlContent is null) throw new ArgumentNullException(nameof(xmlContent));

            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));
            using StreamReader reader = new StreamReader(stream);
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
            if (filePath is null) throw new ArgumentNullException(nameof(filePath));

            try
            {
                using StreamReader reader = new StreamReader(filePath);
                return FromStream(reader);
            }
            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException($"Playlist file not found: {filePath}", filePath, ex);
            }
        }

        /// <summary>
        /// Parses a playlist from a TextReader.
        /// </summary>
        /// <param name="reader">The TextReader containing the XML content.</param>
        /// <returns>A PlaylistV1 object representing the parsed playlist.</returns>
        /// <exception cref="InvalidDataException">Thrown when the XML format is invalid.</exception>
        public static PlaylistRoot FromStream(TextReader reader)
        {
            if (reader is null) throw new ArgumentNullException(nameof(reader));

            try
            {
                using XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    IgnoreComments = true
                });

                return ParseFromXmlReader(xmlReader);
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException($"Invalid XML format: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a playlist from an XmlReader.
        /// </summary>
        /// <param name="xmlReader">The XmlReader positioned on or before the root Playlist element.</param>
        /// <returns>A PlaylistV1 object representing the parsed playlist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when xmlReader is null.</exception>
        /// <exception cref="InvalidDataException">Thrown when the XML format is invalid.</exception>
        public static PlaylistRoot FromXmlReader(XmlReader xmlReader)
        {
            if (xmlReader is null) throw new ArgumentNullException(nameof(xmlReader));
            try
            {
                if (!xmlReader.IsStartElement("Playlist"))
                    throw new InvalidDataException("Root element must be 'Playlist'.");
                return ParseFromXmlReader(xmlReader);
            }
            catch (XmlException ex)
            {
                throw new InvalidDataException($"Invalid XML format: {ex.Message}", ex);
            }
        }

        private static PlaylistRoot ParseFromXmlReader(XmlReader xmlReader)
        {
            // Move to content (skip XML declaration etc.)
            xmlReader.MoveToContent();

            if (!xmlReader.IsStartElement("Playlist"))
                throw new InvalidDataException("Root element must be 'Playlist'.");

            string? version = xmlReader.GetAttribute("Version");

            var playlist = new PlaylistRoot();
            if (version != null)
                playlist.Version = version;

            if (!xmlReader.IsEmptyElement)
            {
                xmlReader.ReadStartElement("Playlist");

                while (xmlReader.NodeType != XmlNodeType.EndElement && xmlReader.NodeType != XmlNodeType.None)
                {
                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "Add")
                    {
                        string? testName = xmlReader.GetAttribute("Test");
                        if (!string.IsNullOrWhiteSpace(testName))
                            playlist.Tests.Add(new AddElement(testName));

                        if (xmlReader.IsEmptyElement)
                            xmlReader.Read();
                        else
                        {
                            xmlReader.ReadStartElement();
                            xmlReader.ReadEndElement();
                        }
                    }
                    else
                    {
                        xmlReader.Skip();
                    }
                }

                xmlReader.ReadEndElement();
            }
            else
            {
                xmlReader.Read();
            }

            return playlist;
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