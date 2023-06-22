using System.Diagnostics;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public enum MediaStatus
    {
        Hidden,
        Playable,
        MissingSubtitles,
        Unplayable,
        Queued,
        Converting
    }

    [DebuggerDisplay("{Name} ({Id})")]
    public abstract class MediaBase : IMedia
    {
        [JsonConstructor]
        protected MediaBase(
            Guid id,
            string name, 
            Metadata metadata,
            SubtitlesList subtitles,
            string filePath,
            TimeSpan length,
            string videoCodec,
            double frameRate,
            string audioCodec,
            VideoSize resolution
            )
        {
            Id = id;
            Name = name;
            Metadata = metadata;
            Subtitles = subtitles;
            Subtitles.OnSubtitlesChange += (_, _) => UpdateStatus();
            FilePath = filePath;
            Length = length;
            VideoCodec = videoCodec;
            VideoFrameRate = frameRate;
            AudioCodec = audioCodec;
            Resolution = resolution;

            UpdateStatus();
        }

        protected MediaBase(string name, IMediaInfo info, Metadata metadata, SubtitlesList subtitles)
        {
            Id = Guid.NewGuid();
            Name = name;
            Metadata = metadata;
            Subtitles = subtitles;
            Subtitles.OnSubtitlesChange += (_, _) => UpdateStatus();
            FilePath = info.Path;
            Length = info.Duration;
            var videoStream = info.VideoStreams.First();
            Resolution = ConversionSupport.GetResolution(videoStream.Width, videoStream.Height);
            VideoCodec = videoStream.Codec;
            VideoFrameRate = videoStream.Framerate;
            var audioStream = info.AudioStreams.First();
            AudioCodec = audioStream.Codec;
            Info = info; // Will trigger a status update
        }

        public abstract string Type { get; }

        private FileInfo? _fileInfo;
        private FileInfo FileInfo => _fileInfo ??= new(FilePath);
        public string FilePath {get; private set;}
        [JsonIgnore]
        public string Directory => FileInfo.Directory!.FullName;
        [JsonIgnore]
        public string FileName => FileInfo.Name.Capitalize();
        [JsonIgnore]
        public string Extension => FileInfo.Extension;
        [JsonIgnore]
        public string ContentType => Extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase) ? "video/x-matroska" : "video/mp4";
        [JsonIgnore]
        public DateTime Creation => FileInfo.CreationTime;
        public TimeSpan Length { get; private set; }
        [JsonIgnore]
        public MediaStatus Status { get; private set; }
        public SubtitlesList Subtitles { get; private set; }
        public string Name { get; protected set; }
        public Metadata Metadata { get; private set; }
        public VideoSize Resolution { get; private set; }

        private IMedia? _companion;
        [JsonIgnore]
        public IMedia? Companion 
        { 
            get => _companion; 
            set
            {
                _companion = value;
                UpdateStatus();
            }
        }
        [JsonIgnore]
        public bool HasCompanion => Companion != null;

        private IMediaInfo? _info;
        [JsonIgnore]
        public IMediaInfo? Info 
        { 
            get => _info;
            set
            {
                _info = value;
                UpdateStatus();
            }
        }

        [JsonIgnore]
        public bool HasInfo => Info != null;

        public Guid Id { get; private set; }
        public double VideoFrameRate { get; private set; }
        public string VideoCodec { get; private set; }
        public string AudioCodec { get; private set; }

        public void UpdateStatus(int? progress = null)
        {
            if (progress.HasValue)
            {
                Status = progress.Value > 0 ? MediaStatus.Converting : MediaStatus.Queued;
                return;
            }

            MediaStatus newStatus;
            if (ConversionSupport.IsConversionRequired(this))
                newStatus = MediaStatus.Unplayable;
            else if (Subtitles.Count > 0 && !Subtitles.Any(s => s.Exists()))
                newStatus = MediaStatus.MissingSubtitles;
            else
                newStatus = MediaStatus.Playable;

            if (HasCompanion)
                switch (newStatus)
                {
                    case MediaStatus.Playable when Companion!.Status == MediaStatus.Unplayable:
                        (Companion as MediaBase)?.UpdateStatus();
                        break;
                    case MediaStatus.Unplayable when Companion!.Status == MediaStatus.Playable:
                    case MediaStatus.Unplayable when Companion!.Status == MediaStatus.Unplayable:
                        newStatus = MediaStatus.Hidden;
                        break;
                }

            Status = newStatus;
        }

        public override string ToString() => $"{Name} ({Id})";

        #region IEquatable<IMedia>
        public override bool Equals(object? obj)
            => Equals(obj as MediaBase);
        public virtual bool Equals(IMedia? other)
        {
            if (other == null) 
                return false;
            if (string.IsNullOrWhiteSpace(other.FilePath) && string.IsNullOrWhiteSpace(FilePath))
                return other.Id == Id;
            return other.FilePath == FilePath;
        }

        public override int GetHashCode()
            => FilePath.GetHashCode();
        #endregion
    }
}
