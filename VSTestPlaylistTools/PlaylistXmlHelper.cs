using System.IO;

namespace VS.TestPlaylistTools
{
    internal static class PlaylistXmlHelper
    {
        internal static void EnsureDirectory(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }

        internal static TextReader StringToReader(string content)
        {
            return new StringReader(content);
        }
    }
}
