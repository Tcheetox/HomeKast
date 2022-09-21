using Xabe.FFmpeg;
using Cast.Provider.Meta;
using Cast.Provider.Converter;

namespace Cast.Provider
{
    public interface IMedia
    {
        MediaStatus Status { get; set; }

        Metadata Metadata { get; init; }
        string Name { get; init; }

        string ConversionPath { get; }
        Guid Id { get; }

        string LocalPath { get; }
        long Size { get; }
        TimeSpan Length { get; }
        IMediaInfo Info { get; }
        DateTime Creation { get; }
        VideoSize Resolution { get; }
        List<Subtitles> Subtitles { get; }
    }
}