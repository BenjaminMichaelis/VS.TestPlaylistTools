using System;
using System.IO;
using System.Xml;
using PlaylistV1; // Reference to PlaylistV1Parser
using PlaylistV2; // Reference to PlaylistV2 parser/model

namespace VSPlaylistBuilder
{
    /// <summary>
    /// Provides unified loading for both V1 and V2 playlist files.
    /// </summary>
    public static class PlaylistLoader
    {
        /// <summary>
        /// Loads a playlist file and returns the parsed object (V1 or V2).
        /// </summary>
        /// <param name="filePath">Path to the playlist file.</param>
        /// <returns>Parsed playlist object (PlaylistV1 or PlaylistV2).</returns>
        public static object Load(string filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            using var reader = new StreamReader(filePath);
            return Load(reader);
        }

        /// <summary>
        /// Loads a playlist from a TextReader and returns the parsed object (V1 or V2).
        /// </summary>
        /// <param name="reader">TextReader for the playlist XML.</param>
        /// <returns>Parsed playlist object (PlaylistV1 or PlaylistV2).</returns>
        public static object Load(TextReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);
            using var xmlReader = XmlReader.Create(reader, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
            if (!xmlReader.ReadToFollowing("Playlist"))
                throw new InvalidDataException("Root <Playlist> element not found.");
            var version = xmlReader.GetAttribute("Version");
            if (string.IsNullOrWhiteSpace(version))
                throw new InvalidDataException("Playlist <Playlist> element missing Version attribute.");
            xmlReader.MoveToElement();
            // Dispatch to correct parser
            if (version.StartsWith('1'))
            {
                return PlaylistV1Parser.FromStream(reader);
            }
            else if (version.StartsWith('2'))
            {
                return PlaylistParser.FromStream(reader);
            }
            else
            {
                throw new NotSupportedException($"Unsupported playlist version: {version}");
            }
        }
    }
}
