using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace VS.TestPlaylistTools.PlaylistV2;

/// <summary>
/// Represents a property-based rule that matches against test properties
/// </summary>
public class PropertyRule : Rule
{
    private static readonly Dictionary<TestPropertyType, string> PropertyTypeToXmlNameMap = new()
    {
        { TestPropertyType.Solution, "Solution" },
        { TestPropertyType.ProjectName, "Project" },
        { TestPropertyType.NamespaceName, "Namespace" },
        { TestPropertyType.ClassName, "Class" },
        { TestPropertyType.TargetFramework, "TargetFramework" },
        { TestPropertyType.Outcome, "Outcome" },
        { TestPropertyType.Trait, "Trait" },
        { TestPropertyType.FullyQualifiedName, "Test" },
        { TestPropertyType.NormalizedFullyQualifiedName, "TestWithNormalizedFullyQualifiedName" },
        { TestPropertyType.DisplayName, "DisplayName" },
        { TestPropertyType.ManagedType, "ManagedType" },
        { TestPropertyType.ManagedMethod, "ManagedMethod" }
    };

    private static readonly Dictionary<string, TestPropertyType> XmlNameToPropertyTypeMap =
        PropertyTypeToXmlNameMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    /// <summary>
    /// The property name for XML serialization
    /// </summary>
    [XmlAttribute(AttributeName = "Name")]
    public string Name
    {
        get => PropertyTypeToXmlNameMap.TryGetValue(Type, out var xmlName) ? xmlName : string.Empty;
        set => Type = XmlNameToPropertyTypeMap.TryGetValue(value, out var propertyType) 
            ? propertyType 
            : TestPropertyType.Solution;
    }

    /// <summary>
    /// The property type (not serialized to XML)
    /// </summary>
    [XmlIgnore]
    public TestPropertyType Type { get; private set; }

    /// <summary>
    /// The property value
    /// </summary>
    [XmlAttribute(AttributeName = "Value")]
    public string? Value { get; set; }

    /// <summary>
    /// Default constructor for XML serialization
    /// </summary>
    public PropertyRule()
    {
    }

    /// <summary>
    /// Creates a property rule with the specified type and value
    /// </summary>
    public PropertyRule(TestPropertyType type, string? value = null)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Creates a Solution property rule
    /// </summary>
    public static PropertyRule Solution()
    {
        return new PropertyRule(TestPropertyType.Solution);
    }

    /// <summary>
    /// Creates a Project property rule with the specified project name
    /// </summary>
    public static PropertyRule Project(string name)
    {
        return new PropertyRule(TestPropertyType.ProjectName, name);
    }

    /// <summary>
    /// Creates a Namespace property rule with the specified namespace name
    /// </summary>
    public static PropertyRule Namespace(string name)
    {
        return new PropertyRule(TestPropertyType.NamespaceName, name);
    }

    /// <summary>
    /// Creates a Class property rule with the specified class name
    /// </summary>
    public static PropertyRule Class(string name)
    {
        return new PropertyRule(TestPropertyType.ClassName, name);
    }

    /// <summary>
    /// Creates a Test property rule with the specified test name
    /// </summary>
    public static PropertyRule Test(string name)
    {
        return new PropertyRule(TestPropertyType.FullyQualifiedName, name);
    }

    /// <summary>
    /// Creates a TestWithNormalizedFullyQualifiedName property rule
    /// </summary>
    public static PropertyRule TestWithNormalizedFullyQualifiedName(string name)
    {
        return new PropertyRule(TestPropertyType.NormalizedFullyQualifiedName, name);
    }

    /// <summary>
    /// Creates a DisplayName property rule
    /// </summary>
    public static PropertyRule TestWithDisplayName(string displayName)
    {
        return new PropertyRule(TestPropertyType.DisplayName, displayName);
    }

    /// <summary>
    /// Creates a ManagedType property rule
    /// </summary>
    public static PropertyRule ManagedType(string name)
    {
        return new PropertyRule(TestPropertyType.ManagedType, name);
    }

    /// <summary>
    /// Creates a ManagedMethod property rule
    /// </summary>
    public static PropertyRule ManagedMethod(string name)
    {
        return new PropertyRule(TestPropertyType.ManagedMethod, name);
    }

    public override bool Equals(object? obj)
    {
        if (obj is PropertyRule other)
        {
            return Type == other.Type && Value == other.Value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Value);
    }
}