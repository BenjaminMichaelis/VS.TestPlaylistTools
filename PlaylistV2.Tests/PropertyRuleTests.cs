using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PlaylistV2.Tests;

/// <summary>
/// Tests for PropertyRule class
/// </summary>
public class PropertyRuleTests
{
    [Fact]
    public void PropertyRule_CreatesCorrectXmlNames()
    {
        // Arrange & Act & Assert
        Assert.Equal("Solution", PropertyRule.Solution().Name);
        Assert.Equal("Project", PropertyRule.Project("TestProject").Name);
        Assert.Equal("TestProject", PropertyRule.Project("TestProject").Value);
        Assert.Equal("Namespace", PropertyRule.Namespace("TestNamespace").Name);
        Assert.Equal("TestNamespace", PropertyRule.Namespace("TestNamespace").Value);
        Assert.Equal("Class", PropertyRule.Class("TestClass").Name);
        Assert.Equal("TestClass", PropertyRule.Class("TestClass").Value);
        Assert.Equal("TestWithNormalizedFullyQualifiedName", PropertyRule.TestWithNormalizedFullyQualifiedName("TestMethod").Name);
        Assert.Equal("TestMethod", PropertyRule.TestWithNormalizedFullyQualifiedName("TestMethod").Value);
        Assert.Equal("DisplayName", PropertyRule.TestWithDisplayName("Test Display Name").Name);
        Assert.Equal("Test Display Name", PropertyRule.TestWithDisplayName("Test Display Name").Value);
    }
}