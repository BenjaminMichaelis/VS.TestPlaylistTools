namespace VS.TestPlaylistTools
{
    /// <summary>
    /// Common interface for both V1 and V2 playlist root types.
    /// </summary>
    public interface IPlaylistRoot
    {
        /// <summary>
        /// The playlist format version (e.g. "1.0" or "2.0").
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Serializes the playlist to a <see cref="TextWriter"/>.
        /// </summary>
        void Serialize(TextWriter writer);
    }
}
