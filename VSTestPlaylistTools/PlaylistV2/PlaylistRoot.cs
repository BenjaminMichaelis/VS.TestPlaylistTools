using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace VS.TestPlaylistTools.PlaylistV2;

/// <summary>
/// Represents the root of a V2 format playlist
/// </summary>
[XmlRoot("Playlist")]
public class PlaylistRoot
{
    /// <summary>
    /// The playlist version (always "2.0" for V2 format)
    /// </summary>
    [XmlAttribute]
    public string Version
    {
        get => _version;
        set
        {
            if (value != "2.0")
                throw new InvalidOperationException($"Playlist V2 must have Version=2.0, but got {value}.");
            _version = value;
        }
    }
    private string _version = "2.0";

    /// <summary>
    /// The rules contained in this playlist
    /// </summary>
    [XmlElement("Property", Type = typeof(PropertyRule))]
    [XmlElement("Rule", Type = typeof(BooleanRule))]
    public List<Rule> Rules { get; } = [];

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
    /// Serializes the playlist to XML string
    /// </summary>
    public override string ToString()
    {
        using StringWriter stringWriter = new StringWriter();
        Serialize(stringWriter);
        return stringWriter.ToString();
    }

    /// <summary>
    /// Serializes the playlist to a TextWriter
    /// </summary>
    public void Serialize(TextWriter writer)
    {
        using XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            CloseOutput = false,
            Indent = true
        });

        XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
        namespaces.Add(string.Empty, string.Empty);

        XmlSerializer serializer = new XmlSerializer(typeof(PlaylistRoot));
        serializer.Serialize(xmlWriter, this, namespaces);
    }
}