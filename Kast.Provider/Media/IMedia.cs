using Kast.Provider.Conversions;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public interface IMedia : IEquatable<IMedia>
    {
        string Name { get; }
        [JsonIgnore]
        IMedia? Companion { get; }
        DateTime Creation { get; }
        string Directory { get; }
        string Extension { get; }
        string FileName { get; }
        Guid Id { get; }
        [JsonIgnore]
        IMediaInfo? Info { get; }
        string Type { get; }
        TimeSpan Length { get; }
        string FilePath { get; }
        VideoSize Resolution { get; }
        MediaStatus Status { get; }
        Metadata? Metadata { get; }
        public int? Year { get; }
        SubtitlesList Subtitles { get; }

        void UpdateStatus(int? progress = null);
        void UpdateMetadata(Metadata? metadata = null);
        void UpdateCompanion(IMedia? companion = null);
        void UpdateInfo(IMediaInfo? info = null);

        string ToString();
    }
}