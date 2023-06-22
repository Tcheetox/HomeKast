using System.Diagnostics;

namespace Kast.Provider.Media
{
    [DebuggerDisplay("{Name}")]
    public class Subtitles
    {
        public int Index { get; private set; }
        public string Name { get; private set; }
        public string Language { get; private set; }
        public bool Preferred { get; private set; }
        public string FilePath { get; private set; }

        public Subtitles(int index, string name, string language, string filePath, bool preferred = false)
        {
            Index = index;
            Name = name;
            Language = language;
            FilePath = filePath;
            Preferred = preferred;
        }

        public bool Exists() => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
    }
}
