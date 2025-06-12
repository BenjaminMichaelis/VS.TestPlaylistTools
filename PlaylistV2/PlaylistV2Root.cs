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
[XmlRoot("Playlist")]
public class PlaylistV2Root
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
    public List<Rule> Rules { get; } = [];

    /// <summary>
    /// Default constructor
    /// </summary>
    public PlaylistV2Root()
    {
    }

    /// <summary>
    /// Constructor with rules
    /// </summary>
    public PlaylistV2Root(IEnumerable<Rule> rules)
    {
        Rules.AddRange(rules);
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

        var serializer = new XmlSerializer(typeof(PlaylistV2Root));
        serializer.Serialize(xmlWriter, this, namespaces);
    }
}