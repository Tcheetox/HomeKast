using Cast.Provider.Converter;
using Cast.Provider.Metadata;
using Xabe.FFmpeg;

namespace Cast.Provider
{
    public interface IMedia
    {
        string Name { get; init; }
        string LocalPath { get; init; }
        DateTime Created { get; init; }
        long Size { get; init; }
        TimeSpan Length { get; init; }
        IMediaInfo Info { get; init; }
        Guid Id { get; }
        string ConversionPath { get; }
        MediaStatus Status { get; set; }
        DateTime Creation { get; init; }
        Metadata.Metadata Metadata { get; init; }
    }
}