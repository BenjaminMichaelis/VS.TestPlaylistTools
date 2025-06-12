using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PlaylistV2.Tests;

/// <summary>
/// Tests for generating boolean rules in playlists
/// </summary>
public class BooleanRuleTests
{
    [Fact]
    public void BooleanRule_CreatesCorrectStructure()
    {
        // Arrange
        var rule = BooleanRule.Any("TestRule",
            PropertyRule.Solution(),
            BooleanRule.All(
                PropertyRule.Project("TestProject"),
                PropertyRule.Namespace("TestNamespace")
            )
        );

        // Act
        var playlist = new PlaylistRoot();
        playlist.Rules.Add(rule);
        var xml = playlist.ToString();

        // Assert
        Assert.Contains("Name=\"TestRule\"", xml);
        Assert.Contains("Match=\"Any\"", xml);
        Assert.Contains("Match=\"All\"", xml);
        Assert.Contains("Name=\"Solution\"", xml);
        Assert.Contains("Name=\"Project\" Value=\"TestProject\"", xml);
        Assert.Contains("Name=\"Namespace\" Value=\"TestNamespace\"", xml);
    }
}