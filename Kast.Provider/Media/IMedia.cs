using System.Text.Json.Serialization;
using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public interface IMedia : IEquatable<IMedia>
    {
        string Name { get; }
        bool HasCompanion { get; }
        [JsonIgnore]
        IMedia? Companion { get; set; }
        DateTime Creation { get; }
        string Directory { get; }
        string Extension { get; }
        string FileName { get; }
        bool HasInfo { get; }
        Guid Id { get; }
        [JsonIgnore]
        IMediaInfo? Info { get; set; }
        string Type { get; }
        TimeSpan Length { get; }
        string FilePath { get; }
        VideoSize Resolution { get; }
        MediaStatus Status { get; }
        Metadata Metadata { get; }
        SubtitlesList Subtitles { get; }

        void UpdateStatus(int? progress = null);
        string ToString();
    }
}