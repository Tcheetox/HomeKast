using Kast.Provider.Supports;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public class Movie : MediaBase
    {
        [JsonConstructor]
        public Movie(
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
            int? year = null)
            : base(id, name, metadata, subtitles, filePath, length, videoCodec, videoFrameRate, audioCodec, resolution, year)
        { }

        public Movie(IMediaInfo info, SubtitlesList subtitles)
            : base(info, subtitles)
        {
            var (name, _, _, year) = Normalization.NameFromPath(info.Path);
            Name = name;
            Year = year;
        }

        public override string Type => "Movie";
    }
}
