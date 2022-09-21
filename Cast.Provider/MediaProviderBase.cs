using Xabe.FFmpeg;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Cast.Provider.Converter;
using Cast.Provider.Meta;
using Cast.SharedModels.User;

namespace Cast.Provider
{
    public abstract class MediaProviderBase : IMediaProvider
    {
        private readonly IMetadataProvider _metadataProvider;
        private readonly ILogger<MediaProviderBase> _logger;

        protected readonly UserProfile _userProfile;
        protected readonly IMediaConverter _mediaConverter;

        static MediaProviderBase()
        {
            _specific.AddRange(Enum.GetNames(typeof(VideoSize)));
            _specific.AddRange(Enum.GetNames(typeof(VideoCodec)));
            _specific.AddRange(Enum.GetNames(typeof(AudioCodec)));
        }

        protected MediaProviderBase(
            ILogger<MediaProviderBase> logger,
            IMetadataProvider metadataProvider,
            IMediaConverter mediaConverter,
            UserProfile profile)
        {
            _logger = logger;
            _mediaConverter = mediaConverter;
            _mediaConverter.OnMediaConverted += async (sender, e) => await TryAddMediaFromPath(e.State.TargetPath);
            _metadataProvider = metadataProvider;
            _userProfile = profile;
        }

        #region Public Members
        public abstract bool IsCached { get; }

        public virtual async Task<IMedia> GetMedia(Guid guid) => (await GetAllMedia())[guid];

        public virtual async Task<ConcurrentDictionary<Guid, IMedia>> GetAllMedia()
        {
            ConcurrentDictionary<Guid, IMedia> library = new();

            foreach (var file in _userProfile
                .Library
                .Directories
                .SelectMany(directory => Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                .Where(f => _userProfile.Library.IsMonitoredExtensions(Path.GetExtension(f))))
            {
                var media = await CreateMedia(file);
                if (media != null)
                    AddMediaAndUpdateCompanions(library, media);
            }

            _logger.LogInformation("MediaProvider retrieved {media} media from {directories} directories", library.Count, _userProfile.Library.Directories.Count);

            return library;
        }

        public async Task<bool> TryAddMediaFromPath(string filePath)
        {
            var media = await CreateMedia(filePath);
            if (media == null)
                return false;

            var library = await GetAllMedia();
            if (library.Any(m => m.Value.LocalPath == media.LocalPath))
                return false;

            var state = AddMediaAndUpdateCompanions(library, media);
            var info = state ? "added" : "could not add";
            _logger.LogInformation("MediaProvider {state} {name} ({guid}) to library from {path}", info, media.Name, media.Id, filePath);

            return state;
        }

        public async Task<bool> TryRemoveMediaFromPath(string filePath)
        {
            var allMedia = await GetAllMedia();
            var media = allMedia.FirstOrDefault(m => m.Value.LocalPath == filePath).Value;
            if (media == null)
                return false;

            var state = RemoveMediaAndUpdateCompanions(allMedia, media);
            var info = state ? "removed" : "could not remove";
            _logger.LogInformation("MediaProvider {state} {name} ({guid}) from library", info, media.Name, media.Id);

            return state;
        }
        #endregion

        #region Private Members
        private static bool AddMediaAndUpdateCompanions(ConcurrentDictionary<Guid, IMedia> library, IMedia media)
        {
            foreach (var companion in from companionEntry in library.Where(m => m.Value.Name == media.Name)
                                      let companion = companionEntry.Value
                                      select companion)
            {
                if (companion.Status != MediaStatus.Playable && media.Status == MediaStatus.Playable)
                    companion.Status = MediaStatus.Hidden;
                else if (companion.Status == MediaStatus.Playable && media.Status != MediaStatus.Playable)
                    media.Status = MediaStatus.Hidden;
            }

            return library.TryAdd(media.Id, media);
        }

        private static bool RemoveMediaAndUpdateCompanions(ConcurrentDictionary<Guid, IMedia> library, IMedia media)
        {
            foreach (var companion in from companionEntry in library.Where(m => m.Value.Name == media.Name && m.Value.Id != media.Id)
                                      let companion = companionEntry.Value
                                      select companion)
            {
                companion.Status = ConversionHelper.RequireConversion(companion.Info)
                    ? MediaStatus.Unplayable
                    : MediaStatus.Playable;

                if (companion.Status == MediaStatus.Playable)
                    break;
            }

            return library.Remove(media.Id, out _);
        }

        private async Task<IMedia?> CreateMedia(string file)
        {
            var info = await _mediaConverter.GetMediaInfo(file);
            if (info == null) return null;

            var (normalized, displayed) = CreateNames(file);
            return new Media(info)
            {
                Name = displayed,
                Metadata = await _metadataProvider.GetMetadataAsync(normalized)
            }.WithSubtitles(_userProfile);
        }

        #region Naming
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
        #endregion
        #endregion
    }
}