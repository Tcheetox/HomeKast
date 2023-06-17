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
            Metadata metadata,
            SubtitlesList subtitles,
            string filePath,
            TimeSpan length,
            string videoCodec,
            double videoFrameRate,
            string audioCodec,
            VideoSize resolution)
            : base(id, name, metadata, subtitles, filePath, TimeSpan.MaxValue, videoCodec, videoFrameRate, audioCodec, resolution)
        { }

        public Movie(string name, IMediaInfo info, Metadata metadata, SubtitlesList subtitles) 
            : base(name, info, metadata, subtitles)
        { }

        public override string Type => typeof(Movie).Name;
    }
}
