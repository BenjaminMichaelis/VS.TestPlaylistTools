namespace VSTestPlaylistTools.TrxToPlaylist;

/// <summary>
/// Represents the outcome of a test result from a TRX file.
/// </summary>
public enum TestOutcome
{
    Passed,
    Failed,
    NotExecuted,
    Inconclusive,
    Timeout,
    Pending
}
