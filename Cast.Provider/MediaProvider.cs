using Cast.Provider.Converter;
using Cast.Provider.MediaInfoProvider;
using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace Cast.Provider
{
    // TODO: logging
    // TODO: layout shift on lib load because of scrollbar...
    public class MediaProvider : IProviderService
    {
        private readonly ILogger<MediaProvider> _logger;
        private readonly UserProfile _profile;
        private readonly IMediaConverter _mediaConverter;
        private readonly IMetadataProvider _metadataProvider;

        public bool IsCached => false;

        static MediaProvider()
        {
            _specific.AddRange(Enum.GetNames(typeof(VideoSize)));
            _specific.AddRange(Enum.GetNames(typeof(VideoCodec)));
            _specific.AddRange(Enum.GetNames(typeof(AudioCodec)));
        }

        public MediaProvider(ILogger<MediaProvider> logger, IMetadataProvider metadataProvider, IMediaConverter mediaConverter, UserProfile profile)
        {
            _logger = logger;
            _mediaConverter = mediaConverter;
            _metadataProvider = metadataProvider;
            _profile = profile;
        }

        public async Task<IMedia?> GetMedia(Guid guid) => (await GetMedia())[guid];

        // TODO: movies which have an equivalent playable state shouldn't be in the collection...
        public async Task<ConcurrentDictionary<Guid, IMedia>> GetMedia()
        {
            ConcurrentDictionary<Guid, IMedia> library = new();

            foreach (var file in _profile
                .Library
                .Directories
                .SelectMany(directory => Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                .Where(f => _profile.Library.Extensions.Any(e => e == Path.GetExtension(f))))
            {
                var info = await _mediaConverter.GetMediaInfo(file);
                if (info != null)
                {
                    var (Normalized, Displayed) = CreateNames(file);
                    var fileInfo = new FileInfo(file);
                    var media = new Media()
                    {
                        LocalPath = file,
                        Name = Displayed,
                        Created = File.GetCreationTime(file),
                        Size = fileInfo.Length,
                        Creation = fileInfo.CreationTime,
                        Length = info.Duration,
                        Info = info,
                        Metadata = await _metadataProvider.GetMetadataAsync(Normalized)
                    }.UpdateStatus(_mediaConverter);
                    library.TryAdd(media.Id, media);
                }
            }

            return library;
        }


        private static readonly List<string> _specific = new()
        {
            "webrip",
            "uncut",
            "1080p",
            "1080 p",
            "720p",
            "720 p",
            "10bit",
            "10 bit",
            "vff",
            "multi",
            "web",
            "french",
            "english",
            "bluray",
            "hdtv",
            "hevc",
            "6ch",
            "x265",
            "x265-chk",
            "-shc23",
            "shc23",
            "-dl",
            "-dnt",
            "bdrip",
            "x265",
            "-mgd",
            "mgd",
            "notag",
            "no tag",
            "custom",
            "vfi",
            "mhd",
            "x264",
            "uncensored",
            "vostfr",
            "-dl",
            "-fhd",
            "partie",
            "true",
            "_",
            "final"
        };
        private static (string Normalized, string Displayed) CreateNames(string path)
        {
            var original = Path.GetFileNameWithoutExtension(path);
            var cleaned = Regex.Replace(original, @"[\.]", " ");
            cleaned = Regex.Replace(cleaned, @"\[.*?\]", " ");
            cleaned = Regex.Replace(cleaned, @"\(.*?\)", " ");

            for (int i = 0; i < 2; i++)
            {
                var cleanedArray = cleaned
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    .Where(e => e.Length > 2);
                cleaned = string.Join(' ', cleanedArray);

                foreach (var word in _specific)
                    cleaned = cleaned.Replace(word, "", StringComparison.InvariantCultureIgnoreCase);
            }

            cleaned = cleaned.Trim();
            if (cleaned.Length > 0)
                cleaned = cleaned[0].ToString().ToUpper() + cleaned[1..];

            var splittedName = cleaned.Split(' ');
            int idx = -1;
            for (int i = 0; i < splittedName.Length; i++)
                if (splittedName[i].Any(char.IsDigit))
                {
                    idx = i;
                    break;
                }

            string displayName = string.Join(' ', splittedName.Where(e => !e.StartsWith('-')));
            string normalizedName = idx > 0 ? string.Join(' ', splittedName[0..idx]) : displayName;
            return (normalizedName, displayName);
        }
    }
}