using Cast.SharedModels.User;
using Cast.SharedModels;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

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
    }
}
