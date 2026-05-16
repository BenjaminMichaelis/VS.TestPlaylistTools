using System.Xml;

namespace VS.TestPlaylistTools.PlaylistV2;

/// <summary>
/// AOT-safe manual XML serializer/deserializer for V2 playlist format.
/// </summary>
internal static class PlaylistV2Serializer
{
    internal static void WritePlaylist(XmlWriter xmlWriter, PlaylistRoot playlist)
    {
        xmlWriter.WriteStartElement("Playlist");
        xmlWriter.WriteAttributeString("Version", "2.0");

        foreach (Rule rule in playlist.Rules)
            WriteRule(xmlWriter, rule);

        xmlWriter.WriteEndElement();
    }

    private static void WriteRule(XmlWriter xmlWriter, Rule rule)
    {
        if (rule is BooleanRule boolRule)
        {
            xmlWriter.WriteStartElement("Rule");
            if (boolRule.Name != null)
                xmlWriter.WriteAttributeString("Name", boolRule.Name);
            xmlWriter.WriteAttributeString("Match", boolRule.Match.ToString());

            foreach (Rule child in boolRule.Rules)
                WriteRule(xmlWriter, child);

            xmlWriter.WriteEndElement();
        }
        else if (rule is PropertyRule propRule)
        {
            xmlWriter.WriteStartElement("Property");
            xmlWriter.WriteAttributeString("Name", propRule.Name);
            if (propRule.Value != null)
                xmlWriter.WriteAttributeString("Value", propRule.Value);
            xmlWriter.WriteEndElement();
        }
    }

    internal static PlaylistRoot ReadPlaylist(XmlReader xmlReader)
    {
        xmlReader.MoveToContent();

        if (!xmlReader.IsStartElement("Playlist"))
            throw new InvalidDataException("Invalid playlist format: Root element must be 'Playlist'");

        string? version = xmlReader.GetAttribute("Version");

        var playlist = new PlaylistRoot();
        if (version != null)
            playlist.Version = version;

        if (xmlReader.IsEmptyElement)
        {
            xmlReader.Read();
            return playlist;
        }

        xmlReader.ReadStartElement("Playlist");

        while (xmlReader.NodeType != XmlNodeType.EndElement && xmlReader.NodeType != XmlNodeType.None)
        {
            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                Rule? rule = ReadRule(xmlReader);
                if (rule != null)
                    playlist.Rules.Add(rule);
            }
            else
            {
                xmlReader.Read();
            }
        }

        xmlReader.ReadEndElement();
        return playlist;
    }

    private static Rule? ReadRule(XmlReader xmlReader)
    {
        if (xmlReader.Name == "Rule")
        {
            string? matchStr = xmlReader.GetAttribute("Match");
            string? name = xmlReader.GetAttribute("Name");

            if (matchStr is null || !Enum.TryParse(matchStr, out BooleanRuleKind match) || !Enum.IsDefined(typeof(BooleanRuleKind), match))
                throw new InvalidDataException($"Invalid or missing Rule Match attribute: '{matchStr}'.");

            var boolRule = new BooleanRule(match, [], name);

            if (xmlReader.IsEmptyElement)
            {
                xmlReader.Read();
                return boolRule;
            }

            xmlReader.ReadStartElement("Rule");

            while (xmlReader.NodeType != XmlNodeType.EndElement && xmlReader.NodeType != XmlNodeType.None)
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    Rule? child = ReadRule(xmlReader);
                    if (child != null)
                        boolRule.Rules.Add(child);
                }
                else
                {
                    xmlReader.Read();
                }
            }

            xmlReader.ReadEndElement();
            return boolRule;
        }
        else if (xmlReader.Name == "Property")
        {
            string? name = xmlReader.GetAttribute("Name");
            string? value = xmlReader.GetAttribute("Value");

            if (name is null)
                throw new InvalidDataException("Property element is missing required 'Name' attribute.");

            var propRule = new PropertyRule();
            propRule.Name = name;
            propRule.Value = value;

            if (xmlReader.IsEmptyElement)
                xmlReader.Read();
            else
            {
                xmlReader.ReadStartElement("Property");
                xmlReader.ReadEndElement();
            }

            return propRule;
        }
        else
        {
            xmlReader.Skip();
            return null;
        }
    }
}
