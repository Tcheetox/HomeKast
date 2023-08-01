using System.Diagnostics;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;
using Kast.Provider.Supports;
using Kast.Provider.Conversions.Factories;

namespace Kast.Provider.Media
{
    public enum MediaStatus
    {
        Hidden,
        Unplayable,    
        MissingSubtitles,
        Queued,
        Converting,
        Streamable,
        Playable
    }

    [DebuggerDisplay("{Name} ({Id})")]
    public abstract class MediaBase : IMedia
    {
        [JsonConstructor]
        protected MediaBase(
            Guid id,
            string name, 
            Metadata? metadata,
            SubtitlesList subtitles,
            string filePath,
            TimeSpan length,
            string videoCodec,
            double frameRate,
            string audioCodec,
            VideoSize resolution,
            int? year = null
            )
        {
            Id = id;
            Name = name;
            Metadata = metadata;
            Subtitles = subtitles;
            FilePath = filePath;
            Length = length;
            VideoCodec = videoCodec;
            VideoFrameRate = frameRate;
            AudioCodec = audioCodec;
            Resolution = resolution;
            Year = year;

            UpdateStatus();
        }

        protected MediaBase(IMediaInfo info, SubtitlesList subtitles)
        {
            Id = Guid.NewGuid();
            Subtitles = subtitles;
            FilePath = info.Path;
            Name = FileName;
            Length = info.Duration;
            var videoStream = info.VideoStreams.First();
            Resolution = ConversionSupport.GetResolution(videoStream.Width, videoStream.Height);
            VideoCodec = videoStream.Codec;
            VideoFrameRate = videoStream.Framerate;
            var audioStream = info.AudioStreams.First();
            AudioCodec = audioStream.Codec;
            Info = info;

            UpdateStatus();
        }
        public string Name { get; protected set; }
        [JsonIgnore]
        public string FileName => FileInfo.Name.Capitalize();
        public abstract string Type { get; }
        public string FilePath { get; }
        private FileInfo? _fileInfo;
        [JsonIgnore]
        public FileInfo FileInfo => _fileInfo ??= new(FilePath);
        public TimeSpan Length { get; private set; }
        [JsonIgnore]
        public MediaStatus Status { get; private set; }
        public SubtitlesList Subtitles { get; private set; }     
        public Metadata? Metadata { get; private set; }
        public VideoSize Resolution { get; private set; }
        [JsonIgnore]
        public IMedia? Companion { get; private set; }
        [JsonIgnore]
        public IMediaInfo? Info { get; private set; }
        public Guid Id { get; private set; }
        public double VideoFrameRate { get; private set; }
        public string VideoCodec { get; private set; }
        public string AudioCodec { get; private set; }
        public int? Year { get; protected set; }

        public void UpdateStatus(int? progress = null, FactoryTarget? target = null)
        {
            if (progress.HasValue)
            {
                if (progress < 0)
                {
                    Status = MediaStatus.Queued;
                    return;
                }

                Status = target == FactoryTarget.Stream ? MediaStatus.Streamable : MediaStatus.Converting;
                return;
            }

            MediaStatus newStatus;
            if (ConversionSupport.IsConversionRequired(this))
                newStatus = MediaStatus.Unplayable;
            else if (Subtitles.IsExtractionRequired())
                newStatus = MediaStatus.MissingSubtitles;
            else
                newStatus = MediaStatus.Playable;

            switch (newStatus)
            {
                case MediaStatus.Playable when Companion?.Status == MediaStatus.Unplayable:
                    Companion?.UpdateStatus();
                    break;
                case MediaStatus.Unplayable when Companion?.Status == MediaStatus.Playable:
                case MediaStatus.Unplayable when Companion?.Status == MediaStatus.Unplayable:
                    newStatus = MediaStatus.Hidden;
                    break;
            }

            Status = newStatus;
        }

        public void UpdateCompanion(IMedia? companion = null)
        { 
            Companion = companion;
            UpdateStatus();
        }

        public void UpdateInfo(IMediaInfo? info = null)
        { 
            Info = info;
        }
        public void UpdateMetadata(Metadata? metadata = null)
        {
            Metadata = metadata;
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
