using System.Diagnostics;

namespace Kast.Provider.Media
{
    [DebuggerDisplay("{Label}")]
    public class Subtitles
    {
        public int Index { get; private set; }
        public string Name { get; private set; }
        public string Label { get; private set; }
        public bool Preferred { get; private set; }
        public string FilePath { get; private set; }

        public Subtitles(int index, string name, string label, string filePath, bool preferred = false)
        {
            Index = index;
            Name = name;
            Label = label;
            FilePath = filePath;
            Preferred = preferred;
        }

        public bool Exists() => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
    }
}
