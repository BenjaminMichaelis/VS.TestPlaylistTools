using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace VS.TestPlaylistTools.PlaylistV2;

/// <summary>
/// Provides parsing and validation for Visual Studio Test Playlist V2 format XML files.
/// </summary>
public static class PlaylistV2Parser
{
    /// <summary>
    /// Parses a playlist from an XML string.
    /// </summary>
    /// <param name="xml">The XML string containing the playlist data.</param>
    /// <returns>The parsed PlaylistRoot object.</returns>
    public static PlaylistRoot FromString(string xml)
    {
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using StreamReader reader = new StreamReader(stream);
        return FromStream(reader);
    }

    /// <summary>
    /// Parses a playlist from a TextReader.
    /// </summary>
    /// <param name="reader">The TextReader containing the playlist data.</param>
    /// <returns>The parsed PlaylistRoot object.</returns>
    public static PlaylistRoot FromStream(TextReader reader)
    {
        using XmlReader xmlReader = XmlReader.Create(reader, new XmlReaderSettings
        {
            CloseInput = false
        });

        if (!xmlReader.IsStartElement("Playlist"))
        {
            throw new InvalidDataException("Invalid playlist format: Root element must be 'Playlist'");
        }

        XmlSerializer serializer = new XmlSerializer(typeof(PlaylistRoot));
        return (PlaylistRoot)serializer.Deserialize(xmlReader)!;
    }

    /// <summary>
    /// Parses a playlist from a file.
    /// </summary>
    /// <param name="filePath">The path to the playlist file.</param>
    /// <returns>The parsed PlaylistRoot object.</returns>
    public static PlaylistRoot FromFile(string filePath)
    {
        if (filePath is null) throw new ArgumentNullException(nameof(filePath));
        using StreamReader reader = new StreamReader(filePath, Encoding.UTF8);
        return FromStream(reader);
    }

    /// <summary>
    /// Parses a playlist from an XML reader.
    /// </summary>
    /// <param name="xmlReader">The XML reader containing the playlist data.</param>
    /// <returns>The parsed PlaylistRoot object.</returns>
    public static PlaylistRoot FromXmlReader(XmlReader xmlReader)
    {
        if (xmlReader is null) throw new ArgumentNullException(nameof(xmlReader));
        if (!xmlReader.IsStartElement("Playlist"))
            throw new InvalidDataException("Invalid playlist format: Root element must be 'Playlist'");
        XmlSerializer serializer = new XmlSerializer(typeof(PlaylistRoot));
        return (PlaylistRoot)serializer.Deserialize(xmlReader)!;
    }

    /// <summary>
    /// Validates if the provided XML content is a valid Playlist V2 format.
    /// </summary>
    /// <param name="xmlContent">The XML content to validate.</param>
    /// <returns>True if the XML is a valid Playlist V2 format; otherwise, false.</returns>
    public static bool IsValidPlaylist(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return false;
        if (string.IsNullOrWhiteSpace(xmlContent))
            return false;
        try
        {
            PlaylistRoot root = FromString(xmlContent);
            return root.Version == "2.0";
        }
        catch
        {
            return false;
        }
    }
}
