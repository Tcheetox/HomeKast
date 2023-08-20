using Kast.Provider;
using Kast.Provider.Media;

namespace Kast.Api.Models
{
    public class MediaGroup : Group<string, Media>
    {
        public static IGrouping<string, Media> Filtered(IGrouping<string, IMedia> group)
        {
            var item = group.First();
            if (item is Movie)
                return new MediaGroup(item.Name, Media.From(group.MaxBy(x => x.Status)!));

            if (item is not Serie)
                throw new NotImplementedException($"Missing type implementation {item.GetType}");

            return new MediaGroup(item.Name, group
                .OfType<Serie>()
                .GroupBy(s => s.Episode?.Indicator)
                .Select(entries => Media.From(entries.MaxBy(x => x.Status)!))
                .OrderBy(m => m.Episode?.Indicator)
                );
        }

        public static IGrouping<string, Media> Unfiltered(IGrouping<string, IMedia> group)
            => new MediaGroup(group.First().Name, group.Select(Media.From));

        private MediaGroup(string key, Media media) : this(key, new List<Media>() { media })
        { }

        private MediaGroup(string key, IEnumerable<Media> items) : base(key, items)
        { }
    }
}
