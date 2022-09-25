using Xabe.FFmpeg;
using Cast.Provider.Meta;
using Cast.Provider.Conversions;
using System;

namespace Cast.Provider
{
    public interface IMedia
    {
        MediaStatus Status { get; set; }
        List<Subtitles> Subtitles { get; set; }

        IMediaInfo Info { get; init; }
        Metadata Metadata { get; init; }
        string Name { get; init; }

        Guid Id { get; }
        string LocalPath { get; }
        long Size { get; }
        bool IsMissingSubtitles { get; }
        TimeSpan Length { get; }
        DateTime Creation { get; }
        VideoSize Resolution { get; }

        public MediaStatus SetBasicStatus()
        {
            if (ConversionHelper.IsConversionRequired(Info))
                Status = MediaStatus.Unplayable;
            else if (IsMissingSubtitles)
                Status = MediaStatus.MissingSubtitles;
            else
                Status = MediaStatus.Playable;

            return Status;
        }
    }
}