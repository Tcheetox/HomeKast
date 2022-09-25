using Xabe.FFmpeg;
using Cast.Provider.Meta;
using Cast.Provider.Conversions;
using System;

namespace Cast.Provider
{
    public interface IMedia
    {
        List<Subtitles> Subtitles { get; set; }

        IMediaInfo Info { get; init; }
        Metadata Metadata { get; init; }
        string Name { get; init; }

        MediaStatus Status { get; }
        Guid Id { get; }
        string LocalPath { get; }
        long Size { get; }
        bool IsMissingSubtitles { get; }
        TimeSpan Length { get; }
        DateTime Creation { get; }
        VideoSize Resolution { get; }

        MediaStatus UpdateStatus(MediaStatus status);
        MediaStatus UpdateStatus(ConversionState? state = null);
    }
}