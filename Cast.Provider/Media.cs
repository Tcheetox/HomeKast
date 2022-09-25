using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Runtime.CompilerServices;
using Cast.Provider.Conversions;
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
        MissingSubtitles,
        Unplayable,
        Queued,
        Converting
    }

    [DebuggerDisplay("{Name} - {LocalPath}")]
    internal class Media : IMedia
    {
        private FileInfo? m_FileInfo;
        private FileInfo FileInfo => m_FileInfo ??= new(Info.Path);
        
        public string LocalPath => Info.Path;
        public long Size => FileInfo.Length;
        public TimeSpan Length => Info.Duration;
        public DateTime Creation => FileInfo.CreationTime;

        public MediaStatus Status { get; private set; }

        public List<Subtitles> Subtitles { get; set; }

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

        public bool IsMissingSubtitles => Info.SubtitleStreams.Any() && !Subtitles.Any(s => s.Exists());

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
                if (ConversionHelper.IsConversionRequired(Info))
                    Status = MediaStatus.Unplayable;
                else if (IsMissingSubtitles)
                    Status = MediaStatus.MissingSubtitles;
                else
                    Status = MediaStatus.Playable;
            }

            return Status;
        }
    }
}
