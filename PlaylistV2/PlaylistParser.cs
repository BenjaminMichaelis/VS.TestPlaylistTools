using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PlaylistV2;

/// <summary>
/// Provides parsing and validation for Visual Studio Test Playlist V2 format XML files.
/// </summary>
public static class PlaylistParser
{
    public static PlaylistV2Root FromString(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var reader = new StreamReader(stream);
        return FromStream(reader);
    }

    public static PlaylistV2Root FromStream(TextReader reader)
    {
        using var xmlReader = XmlReader.Create(reader, new XmlReaderSettings
        {
            CloseInput = false
        });

        if (!xmlReader.IsStartElement("Playlist"))
        {
            throw new InvalidDataException("Invalid playlist format: Root element must be 'Playlist'");
        }

        var version = xmlReader.GetAttribute("Version");
        if (version == "1.0")
        {
            throw new NotSupportedException("Playlist V1 format is not supported by this parser");
        }

        var serializer = new XmlSerializer(typeof(PlaylistV2Root));
        return (PlaylistV2Root)serializer.Deserialize(xmlReader)!;
    }

    public static PlaylistV2Root FromFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        return FromStream(reader);
    }

    public static PlaylistV2Root FromXmlReader(XmlReader xmlReader)
    {
        ArgumentNullException.ThrowIfNull(xmlReader);
        if (!xmlReader.IsStartElement("Playlist"))
            throw new InvalidDataException("Invalid playlist format: Root element must be 'Playlist'");
        var version = xmlReader.GetAttribute("Version");
        if (version == "1.0")
            throw new NotSupportedException("Playlist V1 format is not supported by this parser");
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(PlaylistV2Root));
        return (PlaylistV2Root)serializer.Deserialize(xmlReader)!;
    }

    public static bool IsValidPlaylist(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return false;
        if (string.IsNullOrWhiteSpace(xmlContent))
            return false;
        try
        {
            var root = FromString(xmlContent);
            return root.Version == "2.0";
        }
        catch
        {
            return false;
        }
    }
}
