using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using Cast.Provider.Converter;
using Cast.Provider.Meta;
using Cast.SharedModels;
using Cast.SharedModels.User;
using Xabe.FFmpeg;

namespace Cast.Provider
{
    public enum MediaStatus
    {
        Hidden,
        Playable,
        Unplayable,
        Queued,
        Converting
    }

    [DebuggerDisplay("{Name} - {LocalPath}")]
    internal class Media : IMedia
    {
        private readonly FileInfo _fileInfo;
        public Media(IMediaInfo info)
        {
            Info = info;
            _fileInfo = new FileInfo(info.Path);

            Status = ConversionHelper.RequireConversion(info) 
                ? MediaStatus.Unplayable 
                : MediaStatus.Playable;
        }

        public string LocalPath => Info.Path;
        public long Size => _fileInfo.Length;
        public TimeSpan Length => Info.Duration;
        public DateTime Creation => _fileInfo.CreationTime;

        public IMediaInfo Info { get; }
        public MediaStatus Status { get; set; }
        public string Name { get; init; }
        public Metadata Metadata { get; init; }
        public VideoSize Resolution
        {
            get
            {
                var videoStream = Info.VideoStreams.FirstOrDefault();
                if (videoStream != null && Status == MediaStatus.Playable)
                    return (videoStream.Width >= 1920 || videoStream.Height >= 1080) 
                        ? VideoSize.Hd1080 
                        : VideoSize.Hd720;
                return default;
            }
        }

        #region Conversion members
        private Guid? _conversionId;
        public Guid Id
        {
            get
            {
                if (!_conversionId.HasValue)
                    _conversionId = Guid.NewGuid();
                return _conversionId.Value;
            }
        }

        private string? _conversionPath = null;
        public string ConversionPath 
            => _conversionPath ??= Path.Combine(Path.GetTempPath(), $"{Id}.mp4");

        public List<Subtitles> Subtitles => subtitles;

        private readonly List<Subtitles> subtitles = new();
        public Media WithSubtitles(UserProfile userProfile)
        {
            string mediaName = Path.GetFileNameWithoutExtension(LocalPath);
            string prefixedName = mediaName.StartsWith("_") ? mediaName : "_" + mediaName;

            // Build expected subtitles based on streams
            if (Info.SubtitleStreams?.Any() ?? false)
            {
                for (int i = 0; i < Info.SubtitleStreams.Count(); i++)
                {
                    var sub = Info.SubtitleStreams.ElementAt(i);
                    string file = $"{prefixedName}_{sub.Language}_{i}.srt";
                    subtitles.Add(new Subtitles()
                    {
                        Index = i,
                        Source = $"{Helper.CACHE_FOLDER}/{file}",
                        Label = sub.Forced.HasValue && sub.Forced.Value == 1 ? $"{sub.Language} (forced)" : sub.Language, // TODO: nicer name?!
                        Path = Path.Combine(userProfile.Application.CacheDirectory, file) // TODO: change to proper extension?
                    });
                }
                return this;
            }

            // Search available local files
            foreach (var file in Directory.GetFiles(userProfile.Application.CacheDirectory, "*.srt"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!fileName.StartsWith(prefixedName))
                    continue;

                var composedName = fileName.Replace(prefixedName, string.Empty).Split('_', StringSplitOptions.RemoveEmptyEntries);
                subtitles.Add(new Subtitles()
                {
                    Index = int.Parse(composedName[1]),
                    Source = $"{Helper.CACHE_FOLDER}/{Path.GetFileName(file)}",
                    Label = composedName[0], 
                    Path = file
                });
            }

            return this;
        }
        #endregion
    }
}
