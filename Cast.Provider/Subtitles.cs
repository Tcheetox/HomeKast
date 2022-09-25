using System.Diagnostics;
using Newtonsoft.Json;

namespace Cast.Provider
{
    [DebuggerDisplay("{DisplayLabel}")]
    public class Subtitles
    {
        public int Index { get; init; }
        public string Label { get; init; }
        public string DisplayLabel { get; init; }
        public bool Active { get; set; }

        [JsonIgnore]
        public string LocalPath { get; init; }

        public bool Exists() => !string.IsNullOrEmpty(LocalPath) && File.Exists(LocalPath);

        private Guid? _conversionId;
        public Guid Id
        {
            get
            {
                if (!_conversionId.HasValue)
                    _conversionId = Guid.NewGuid();
                return _conversionId.Value;
            }
        }

        private string? _temporaryPath;
        public string TemporaryPath
            => _temporaryPath ??= Path.Combine(Path.GetTempPath(), $"{Id}.vtt");
    }
}
