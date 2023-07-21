using Xabe.FFmpeg;
using Kast.Provider.Media;
using Kast.Provider.Supports;
using static Kast.Provider.Media.Serie;

namespace Kast.Api.Models
{
    public class Media
    {
        public static Media From(IMedia media)
            => new()
            {
                Name = media.Name,
                Creation = media.Creation,
                Id = media.Id,
                Length = media.Length,
                Resolution = media.Resolution,
                Status = media.Status,
                Captions = new List<Caption>(media.Subtitles.Select(s => new Caption(s))),
                Description = media.Metadata?.Description,
                Popularity = media.Metadata?.Vote,
                Episode = media is Serie serie ? serie.Episode : null,
                Released = media.Metadata?.Released ?? Utilities.ToDateTime(media.Year),
                HasImage = media.Metadata?.HasImage,
                HasThumbnail = media.Metadata?.HasThumbnail
            };

        public string? Name { get; private init; }
        public DateTime Creation { get; private init; }
        public Guid Id { get; private init; }
        public TimeSpan Length { get; private init; }
        public VideoSize Resolution { get; private init; }
        public MediaStatus Status { get; private init; }
        public IReadOnlyList<Caption>? Captions { get; private init; }
        public string? Description { get; private init; }
        public double? Popularity { get; private init; }
        public DateTime? Released { get; private init; }
        public EpisodeInfo? Episode { get; private init; }
        public bool? HasImage { get; private init; }
        public bool? HasThumbnail { get; private init; }

        private Media()
        { }

        public class Caption
        {
            public string Label { get; private set; }
            public int Index { get; private set; }
            public bool Preferred { get; private set; }
            public Caption(Subtitles subtitles)
            {
                Label = subtitles.Language;
                Index = subtitles.Index;
                Preferred = subtitles.Preferred;
            }
        }
    }
}
