using System;
using System.Diagnostics;
using Xabe.FFmpeg;
using Cast.Provider.Conversions;
using Cast.Provider.Meta;
using Cast.SharedModels;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace Cast.Provider
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

    [DebuggerDisplay("{Name} - {LocalPath}")]
    internal class Media : IMedia
    {
        private FileInfo? _fileInfo;
        private FileInfo FileInfo => _fileInfo ??= new(Info.Path);

        public string LocalPath => Info.Path;
        public string FileName => _fileInfo!.Name.Capitalize();

        public long Size => FileInfo.Length;
        public TimeSpan Length => Info.Duration;
        public DateTime Creation => FileInfo.CreationTime;

        public MediaStatus Status { get; private set; }

        public SubtitlesCollection Subtitles { get; init; }
        public IMediaInfo Info { get; init; }
        public string Name { get; init; }
        public Metadata Metadata { get; init; }
        public VideoSize Resolution
        {
            get
            {
                var videoStream = Info.VideoStreams.FirstOrDefault();
                if (videoStream != null && (Status == MediaStatus.Playable || Status == MediaStatus.MissingSubtitles))
                    return (videoStream.Width >= 1920 || videoStream.Height >= 1080)
                        ? VideoSize.Hd1080
                        : VideoSize.Hd720;
                return default;
            }
        }

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

        public MediaStatus UpdateStatus(MediaStatus status)
        {
            Status = status;
            return Status;
        }

        public MediaStatus UpdateStatus(ConversionState? state = null)
        {
            if (state?.Progress != null)
            {
                Status = state.Progress.Percent >= 0
                    ? MediaStatus.Converting
                    : MediaStatus.Queued;
            }
            else
            {
                bool isMissingSubtitles = Info.SubtitleStreams.Any() && !Subtitles.Any(s => s.Exists());
                if (ConversionHelper.IsConversionRequired(Info))
                    Status = MediaStatus.Unplayable;
                else if (isMissingSubtitles)
                    Status = MediaStatus.MissingSubtitles;
                else
                    Status = MediaStatus.Playable;
            }

            return Status;
        }

        public void UpdateSubtitles(ConversionState? state = null)
        {
            Subtitles.Refresh();
            UpdateStatus(state);
        }

        public override string ToString()
            => JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true, });
    }
}
