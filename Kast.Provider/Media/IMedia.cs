using Kast.Provider.Conversions;
using Kast.Provider.Conversions.Factories;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public interface IMedia : IEquatable<IMedia>
    {
        string Name { get; }
        [JsonIgnore]
        IMedia? Companion { get; }
        string FileName { get; }
        string FilePath { get; }
        [JsonIgnore]
        FileInfo FileInfo { get; }
        Guid Id { get; }
        [JsonIgnore]
        IMediaInfo? Info { get; }
        string Type { get; }
        TimeSpan Length { get; }
        VideoSize Resolution { get; }
        MediaStatus Status { get; }
        Metadata? Metadata { get; }
        public int? Year { get; }
        SubtitlesList Subtitles { get; }

        void UpdateStatus(int? progress = null, FactoryTarget? target = null);
        void UpdateMetadata(Metadata? metadata = null);
        void UpdateCompanion(IMedia? companion = null);
        void UpdateInfo(IMediaInfo? info = null);

        string ToString();
    }
}