using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public class Serie : MediaBase
    {
        private readonly static Regex _episodeRegex = new(@"\b(S\d{2}E\d{2})\s*(.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public Serie(string name, IMediaInfo info, Metadata metadata, SubtitlesList subtitles) 
            : base(name, info, metadata, subtitles)
        {
            var match = _episodeRegex.Match(name);
            if (match.Success)
            {
                Episode = match.Value.Trim();
                Name = name.Replace(Episode, string.Empty).Trim();
            }
        }

        [JsonConstructor]
        public Serie(
            Guid id,
            string name,
            Metadata metadata,
            SubtitlesList subtitles,
            string filePath,
            TimeSpan length,
            string videoCodec,
            double videoFrameRate,
            string audioCodec,
            VideoSize resolution,
            string? episode = null)
            : base(id, name, metadata, subtitles, filePath, length, videoCodec, videoFrameRate, audioCodec, resolution)
        {
            Episode = episode;
        }

        public override string Type => typeof(Serie).Name;
        public string? Episode { get; }
    }
}
