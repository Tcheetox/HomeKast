using Xabe.FFmpeg;
using Cast.Provider.Meta;

namespace Cast.Provider
{
    public interface IMedia
    {
        string ConversionPath { get; }
        Guid Id { get; }

        MediaStatus Status { get; set; }

        string Name { get; init; }
        string LocalPath { get; init; }
        DateTime Created { get; init; }
        long Size { get; init; }
        TimeSpan Length { get; init; }
        IMediaInfo Info { get; init; }
        DateTime Creation { get; init; }
        Metadata Metadata { get; init; }
        VideoSize Resolution { get; init; }
    }
}