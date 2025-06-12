using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace PlaylistV2;

/// <summary>
/// Represents a boolean logic rule that can contain other rules
/// </summary>
public class BooleanRule : Rule
{
    /// <summary>
    /// Optional name for the rule
    /// </summary>
    [XmlAttribute(AttributeName = "Name")]
    public string? Name { get; set; }

    /// <summary>
    /// The matching behavior (All, Any, or Not)
    /// </summary>
    [XmlAttribute(AttributeName = "Match")]
    public BooleanRuleKind Match { get; set; }

    /// <summary>
    /// Child rules contained within this boolean rule
    /// </summary>
    [XmlElement("Property", Type = typeof(PropertyRule))]
    [XmlElement("Rule", Type = typeof(BooleanRule))]
    public List<Rule> Rules { get; } = [];

    /// <summary>
    /// Default constructor for XML serialization
    /// </summary>
    public BooleanRule()
    {
    }

    /// <summary>
    /// Creates a boolean rule with the specified match type and child rules
    /// </summary>
    public BooleanRule(BooleanRuleKind match, IEnumerable<Rule> rules, string? name = null)
    {
        Name = name;
        Match = match;
        Rules.AddRange(rules);
    }

    /// <summary>
    /// Creates an "Any" rule with a name
    /// </summary>
    public static BooleanRule Any(string name, params Rule[] rules)
    {
        return new BooleanRule(BooleanRuleKind.Any, rules, name);
    }

    /// <summary>
    /// Creates an "Any" rule without a name
    /// </summary>
    public static BooleanRule Any(params Rule[] rules)
    {
        return new BooleanRule(BooleanRuleKind.Any, rules);
    }

    /// <summary>
    /// Creates an "All" rule with a name
    /// </summary>
    public static BooleanRule All(string name, params Rule[] rules)
    {
        return new BooleanRule(BooleanRuleKind.All, rules, name);
    }

    /// <summary>
    /// Creates an "All" rule without a name
    /// </summary>
    public static BooleanRule All(params Rule[] rules)
    {
        return new BooleanRule(BooleanRuleKind.All, rules);
    }

    /// <summary>
    /// Creates a "Not" rule with a name
    /// </summary>
    public static BooleanRule Not(string name, params Rule[] rules)
    {
        return new BooleanRule(BooleanRuleKind.Not, rules, name);
    }

    /// <summary>
    /// Creates a "Not" rule without a name
    /// </summary>
    public static BooleanRule Not(params Rule[] rules)
    {
        return new BooleanRule(BooleanRuleKind.Not, rules);
    }
}