namespace PlaylistV2;

/// <summary>
/// Defines the matching behavior for boolean rules
/// </summary>
public enum BooleanRuleKind
{
    /// <summary>
    /// All child rules must match
    /// </summary>
    All,
    
    /// <summary>
    /// Any child rule must match
    /// </summary>
    Any,
    
    /// <summary>
    /// None of the child rules must match
    /// </summary>
    Not
}