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
        /// Loads a playlist file and returns the parsed playlist.
        /// The file is read once; the version is detected and parsing dispatched without reopening.
        /// </summary>
        /// <param name="filePath">Path to the playlist file.</param>
        /// <returns>Parsed playlist as <see cref="IPlaylistRoot"/> (either <see cref="PlaylistV1.PlaylistRoot"/> or <see cref="PlaylistV2.PlaylistRoot"/>).</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the playlist file cannot be found.</exception>
        /// <exception cref="InvalidDataException">Thrown when the file is not a recognized playlist format.</exception>
        /// <exception cref="NotSupportedException">Thrown when the playlist version is not supported.</exception>
        public static IPlaylistRoot Load(string filePath)
        {
            if (filePath is null) throw new ArgumentNullException(nameof(filePath));

            try
            {
                using StreamReader reader = new StreamReader(filePath);
                using XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings
                {
                    IgnoreComments = true,
                    IgnoreWhitespace = true
                });

                // Advance to first element and validate it is the root <Playlist> element.
                // MoveToContent() positions on the first non-whitespace/comment node;
                // we then verify it is an Element at depth 0 named "Playlist" rather than
                // using ReadToFollowing() which would accept a nested <Playlist> child.
                if (xmlReader.MoveToContent() != XmlNodeType.Element
                    || xmlReader.Depth != 0
                    || xmlReader.LocalName != "Playlist")
                    throw new InvalidDataException("Root element must be <Playlist>.");

                string? version = xmlReader.GetAttribute("Version");
                if (string.IsNullOrWhiteSpace(version))
                    throw new InvalidDataException("<Playlist> element is missing the Version attribute.");

                if (version == "1.0")
                    return PlaylistV1Parser.FromXmlReader(xmlReader);
                else if (version == "2.0")
                    return PlaylistV2Parser.FromXmlReader(xmlReader);
                else
                    throw new NotSupportedException($"Unsupported playlist version: {version}");
            }
            catch (FileNotFoundException ex)
            {
                throw new FileNotFoundException($"Playlist file not found: {filePath}", filePath, ex);
            }
        }
    }
}
