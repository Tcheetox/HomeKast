using Xabe.FFmpeg;
using Kast.Provider.Media;

namespace Kast.Api.Models
{
    public abstract class Media
    {
        public static Media From(IMedia media)
        {
            if (media is Provider.Media.Movie movie)
                return new Movie(movie);
            if (media is Provider.Media.Serie serie)
                return new Serie(serie);
            throw new NotImplementedException($"{media.GetType()} not implemented");
        }

        public string Name { get; private set; }
        public DateTime Creation { get; private set; }
        public Guid Id { get; private set; }
        public TimeSpan Length { get; private set; }
        public VideoSize Resolution { get; private set; }
        public MediaStatus Status { get; private set; }
        public IReadOnlyList<Caption> Captions { get; private set; }
        public string? Description { get; private set; }
        public double? Popularity { get; private set; }
        public DateTime? Released { get; private set; }

        protected Media(IMedia media)
        {
            Name = media.Name;
            Creation = media.Creation;
            Id = media.Id;
            Length = media.Length;
            Resolution = media.Resolution;
            Status = media.Status;
            Captions = new List<Caption>(media.Subtitles.Select(s => new Caption(s)));
            Description = media.Metadata.Description;
            if (!string.IsNullOrWhiteSpace(media.Metadata.Released)
                && DateTime.TryParse(media.Metadata.Released, out DateTime released))
                Released = released;
        }

        public class Caption
        {
            public string Label { get; private set; }
            public int Index { get; private set; }
            public bool Preferred { get; private set; }

            public Caption(Subtitles subtitles)
            {
                Label = subtitles.Label;
                Index = subtitles.Index;
                Preferred = subtitles.Preferred;
            }
        }

        #region Movie
        public class Movie : Media
        {
            public Movie(Provider.Media.Movie media) : base(media)
            { }
        }
        #endregion

        #region Serie
        public class Serie : Media
        {
            public string? Episode { get; private set; }
            public Serie(Provider.Media.Serie media) : base(media)
            {
                Episode = media.Episode;

                #endregion
            }
        }
    }
}
