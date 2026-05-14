namespace VS.TestPlaylistTools.PlaylistV2;

/// <summary>
/// Defines the types of properties that can be used in property rules
/// </summary>
public enum TestPropertyType
{
    /// <summary>
    /// Represents the solution containing the tests.
    /// </summary>
    Solution,
    /// <summary>
    /// Represents the name of the project containing the tests.
    /// </summary>
    ProjectName,
    /// <summary>
    /// Represents the namespace of the test class.
    /// </summary>
    NamespaceName,
    /// <summary>
    /// Represents the name of the test class.
    /// </summary>
    ClassName,
    /// <summary>
    /// Represents the target framework of the test project.
    /// </summary>
    TargetFramework,
    /// <summary>
    /// Represents the outcome of the test (e.g., Passed, Failed).
    /// </summary>
    Outcome,
    /// <summary>
    /// Represents a trait (category or property) assigned to the test.
    /// </summary>
    Trait,
    /// <summary>
    /// Represents the fully qualified name of the test method.
    /// </summary>
    FullyQualifiedName,
    /// <summary>
    /// Represents the normalized fully qualified name of the test method.
    /// </summary>
    NormalizedFullyQualifiedName,
    /// <summary>
    /// Represents the display name of the test.
    /// </summary>
    DisplayName,
    /// <summary>
    /// Represents the managed type (CLR type) of the test class.
    /// </summary>
    ManagedType,
    /// <summary>
    /// Represents the managed method (CLR method) of the test.
    /// </summary>
    ManagedMethod
}