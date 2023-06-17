using Xabe.FFmpeg.Events;
using Kast.Provider.Conversions.Factories;
using Kast.Provider.Media;
using Kast.Provider.Supports;

namespace Kast.Provider.Conversions
{
    public class ConversionState
    {
        public enum ConversionType
        {
            FullConversion,
            SubtitlesOnly
        }

        public readonly IMedia Media;
        public readonly ConversionType Type;
        public readonly string MediaTargetPath;
        public readonly int? SubtitlesStreamIndex;
        public readonly int AudioStreamIndex;
        public readonly string TargetDirectory;

        public ConversionState(IMedia media, SettingsProvider settingsProvider) 
        { 
            Media = media;
            TargetDirectory = IOSupport.CreateTargetDirectory(media.FilePath);
            Type = media.Status != MediaStatus.MissingSubtitles ? ConversionType.FullConversion : ConversionType.SubtitlesOnly;
            MediaTargetPath = Type != ConversionType.SubtitlesOnly
                ? Path.Combine(TargetDirectory, $"_{Media.FileName.Replace(Media.Extension, Extension)}")
                : Media.FilePath;

            // Define streams preferred index
            var audioStreams = Media.Info!.AudioStreams.ToList();
            var subtitlesStreams = Media.Info!.SubtitleStreams.ToList();
            foreach (var setting in settingsProvider.Preferences)
            {
                var audioStream = audioStreams.FirstOrDefault(s => s.Language.ToLower() == setting.Language?.ToLower());
                var subtitlesStream = subtitlesStreams.FirstOrDefault(s => s.Language.ToLower() == setting.Subtitles?.ToLower());
                if (audioStream != null && (subtitlesStream != null || string.IsNullOrWhiteSpace(setting.Subtitles)))
                {
                    AudioStreamIndex = audioStreams.IndexOf(audioStream);
                    SubtitlesStreamIndex = subtitlesStreams?.IndexOf(subtitlesStream);
                    return;
                }
            }
        }

        public string Name => Media.Name;
        public Guid Id => Media.Id;
        public MediaStatus Status => Media.Status;
        public FactoryTarget? Target { get; private set; }
        public ConversionProgressEventArgs? Progress { get; private set; }
        public int QueueCount { get; set; }
        public bool BurnSubtitles { get; set; }
        public string Extension => BurnSubtitles ? ".mp4" : ".mkv";

        private string? _mediaTemporaryPath;
        public string MediaTemporaryPath => _mediaTemporaryPath ??= IOSupport.GetTempPath(Extension);

        internal void Update(ConversionProgressEventArgs? args, FactoryTarget? target)
        {
            Progress = args;
            Target = target;
            Media.UpdateStatus(args?.Percent);
        }
    }
}