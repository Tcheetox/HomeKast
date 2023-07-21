using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class Serie : MediaBase
    {
        private readonly static Regex _infoRegex = new(@"\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Serie(IMediaInfo info, SubtitlesList subtitles) 
            : base(info, subtitles)
        {
            var (name, episode, episodeName, year) = Normalization.NameFromPath(info.Path);
            Name = name;
            Year = year;

            if (!string.IsNullOrWhiteSpace(episode))
            {
                var indexes = _infoRegex.Matches(episode);
                Episode = new EpisodeInfo()
                {
                    Indicator = episode,
                    Name = episodeName,
                    Season = indexes.Count > 1 && int.TryParse(indexes[0].Value, out int season) ? season : null,
                    Episode = indexes.Count > 1 && int.TryParse(indexes[1].Value, out int _episode) 
                        || indexes.Count == 1 && int.TryParse(indexes[0].Value, out _episode) ? _episode : null,
                };
            }
        }

        [JsonConstructor]
        public Serie(
            Guid id,
            string name,
            Metadata? metadata,
            SubtitlesList subtitles,
            string filePath,
            TimeSpan length,
            string videoCodec,
            double videoFrameRate,
            string audioCodec,
            VideoSize resolution,
            EpisodeInfo? episode = null,
            int? year = null)
            : base(id, name, metadata, subtitles, filePath, length, videoCodec, videoFrameRate, audioCodec, resolution, year)
        {
            Episode = episode;
        }

        public override string Type => "Serie";
        public EpisodeInfo? Episode { get; }

        public class EpisodeInfo
        {
            public string? Indicator { get; init; }
            public string? Name { get; init; }
            public int? Episode { get; init; }
            public int? Season { get; init; }
        }
    }
}
