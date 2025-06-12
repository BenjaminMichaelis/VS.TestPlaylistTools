using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PlaylistV1.Models
{
    /// <summary>
    /// Represents a Visual Studio Test Playlist Version 1.0
    /// </summary>
    [XmlRoot("Playlist")]
    public class PlaylistV1
    {
        /// <summary>
        /// The version of the playlist format. Always "1.0" for V1 playlists.
        /// </summary>
        [XmlAttribute("Version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// The collection of tests to be included in this playlist.
        /// </summary>
        [XmlElement("Add")]
        public List<AddElement> Tests { get; set; } = new List<AddElement>();

        /// <summary>
        /// Initializes a new instance of the PlaylistV1 class.
        /// </summary>
        public PlaylistV1()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PlaylistV1 class with the specified tests.
        /// </summary>
        /// <param name="testNames">The test names to include in the playlist.</param>
        public PlaylistV1(IEnumerable<string> testNames)
        {
            if (testNames == null)
                throw new ArgumentNullException(nameof(testNames));

            foreach (var testName in testNames)
            {
                Tests.Add(new AddElement(testName ?? string.Empty));
            }
        }

        /// <summary>
        /// Adds a test to the playlist.
        /// </summary>
        /// <param name="testName">The fully qualified name of the test to add.</param>
        public void AddTest(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                throw new ArgumentException("Test name cannot be null or empty.", nameof(testName));

            Tests.Add(new AddElement(testName));
        }

        /// <summary>
        /// Removes a test from the playlist.
        /// </summary>
        /// <param name="testName">The fully qualified name of the test to remove.</param>
        /// <returns>True if the test was found and removed; otherwise, false.</returns>
        public bool RemoveTest(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                return false;

            return Tests.RemoveAll(t => string.Equals(t.Test, testName, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        /// <summary>
        /// Gets the count of tests in the playlist.
        /// </summary>
        public int TestCount => Tests.Count;
    }
}