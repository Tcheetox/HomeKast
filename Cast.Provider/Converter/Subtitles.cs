using Newtonsoft.Json;

namespace Cast.Provider.Converter
{
    public class Subtitles
    {
        public bool Active { get; init; }
        public string Label { get; init; }
        public string Source { get; init; }

        [JsonIgnore]
        public string Path { get; init; }
        [JsonIgnore]
        public int Index { get; init; }

        public bool IsValid
            => !string.IsNullOrEmpty(Path)
            && !string.IsNullOrEmpty(Source)
            && File.Exists(Path);
    }
}
