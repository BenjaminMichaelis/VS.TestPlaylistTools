using System;
using System.Xml.Serialization;

namespace PlaylistV1
{
    /// <summary>
    /// Represents a test element in a V1 playlist.
    /// </summary>
    public class AddElement
    {
        /// <summary>
        /// The fully qualified name of the test.
        /// </summary>
        [XmlAttribute("Test")]
        public string? Test { get; set; }

        /// <summary>
        /// Initializes a new instance of the AddElement class.
        /// </summary>
        public AddElement() { }

        /// <summary>
        /// Initializes a new instance of the AddElement class with the specified test name.
        /// </summary>
        /// <param name="test">The fully qualified name of the test.</param>
        public AddElement(string test)
        {
            Test = test;
        }
    }
}