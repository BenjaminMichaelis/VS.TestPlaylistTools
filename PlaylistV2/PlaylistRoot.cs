using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace PlaylistV2;

/// <summary>
/// Represents the root of a V2 format playlist
/// </summary>
[XmlRoot(ElementName = "Playlist")]
public class PlaylistRoot
{
    /// <summary>
    /// The playlist version (always "2.0" for V2 format)
    /// </summary>
    [XmlAttribute]
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// The rules contained in this playlist
    /// </summary>
    [XmlElement("Property", Type = typeof(PropertyRule))]
    [XmlElement("Rule", Type = typeof(BooleanRule))]
    public List<Rule> Rules { get; } = new List<Rule>();

    /// <summary>
    /// Default constructor
    /// </summary>
    public PlaylistRoot()
    {
    }

    /// <summary>
    /// Constructor with rules
    /// </summary>
    public PlaylistRoot(IEnumerable<Rule> rules)
    {
        Rules.AddRange(rules);
    }

    /// <summary>
    /// Parses a playlist from XML string
    /// </summary>
    public static PlaylistRoot FromString(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var reader = new StreamReader(stream);
        return FromStream(reader);
    }

    /// <summary>
    /// Parses a playlist from a TextReader
    /// </summary>
    public static PlaylistRoot FromStream(TextReader reader)
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

        var serializer = new XmlSerializer(typeof(PlaylistRoot));
        return (PlaylistRoot)serializer.Deserialize(xmlReader)!;
    }

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