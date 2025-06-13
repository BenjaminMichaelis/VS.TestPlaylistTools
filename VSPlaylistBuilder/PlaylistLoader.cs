using System.Xml;

using VS.TestPlaylistTools.PlaylistV1;
using VS.TestPlaylistTools.PlaylistV2;

namespace VS.TestPlaylistTools
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
            if (filePath is null) throw new ArgumentNullException(nameof(filePath));
            using StreamReader reader = new StreamReader(filePath);
            using XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
            if (!xmlReader.ReadToFollowing("Playlist"))
                throw new InvalidDataException("Root <Playlist> element not found.");
            string version = xmlReader.GetAttribute("Version");
            if (string.IsNullOrWhiteSpace(version))
                throw new InvalidDataException("Playlist <Playlist> element missing Version attribute.");
            // Dispatch to correct parser
            if (version.StartsWith('1'))
            {
                return PlaylistV1Parser.FromFile(filePath);
            }
            else if (version.StartsWith('2'))
            {
                return PlaylistV2Parser.FromFile(filePath);
            }
            else
            {
                throw new NotSupportedException($"Unsupported playlist version: {version}");
            }
        }
    }
}
