using System.Xml.Linq;

namespace VSTestPlaylistTools.TrxToPlaylist;

internal static class TrxFileParser
{
    private static readonly XNamespace TrxNs = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    /// <summary>
    /// Parses a TRX file and returns a list of test results.
    /// Each result contains the fully-qualified test name (ClassName.MethodName) and its outcome.
    /// </summary>
    internal static IReadOnlyList<TrxTestResult> Parse(string filePath)
    {
        XDocument doc = XDocument.Load(filePath);
        XElement? root = doc.Root;
        if (root is null)
            return [];

        // Build testId -> FullyQualifiedTestName lookup from TestDefinitions
        Dictionary<string, string> testNames = new(StringComparer.OrdinalIgnoreCase);
        XElement? definitions = root.Element(TrxNs + "TestDefinitions");
        if (definitions is not null)
        {
            foreach (XElement unitTest in definitions.Elements(TrxNs + "UnitTest"))
            {
                string? id = unitTest.Attribute("id")?.Value;
                XElement? method = unitTest.Element(TrxNs + "TestMethod");
                string? className = method?.Attribute("className")?.Value;
                string? name = method?.Attribute("name")?.Value;
                if (id is not null && className is not null && name is not null)
                    testNames[id] = $"{className}.{name}";
            }
        }

        // Parse UnitTestResult elements, joining with test name lookup
        List<TrxTestResult> results = [];
        XElement? resultsElement = root.Element(TrxNs + "Results");
        if (resultsElement is not null)
        {
            foreach (XElement unitTestResult in resultsElement.Elements(TrxNs + "UnitTestResult"))
            {
                string? testId = unitTestResult.Attribute("testId")?.Value;
                if (testId is null || !testNames.TryGetValue(testId, out string? fqn))
                    continue;

                string? outcomeStr = unitTestResult.Attribute("outcome")?.Value;
                if (!Enum.TryParse(outcomeStr, ignoreCase: true, out TestOutcome outcome) || !Enum.IsDefined(typeof(TestOutcome), outcome))
                    continue;

                results.Add(new TrxTestResult(fqn, outcome));
            }
        }

        return results;
    }
}

internal sealed class TrxTestResult
{
    internal TrxTestResult(string fullyQualifiedTestName, TestOutcome outcome)
    {
        FullyQualifiedTestName = fullyQualifiedTestName;
        Outcome = outcome;
    }

    internal string FullyQualifiedTestName { get; }
    internal TestOutcome Outcome { get; }
}
