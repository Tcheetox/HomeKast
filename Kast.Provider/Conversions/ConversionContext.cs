using Xabe.FFmpeg.Events;
using Kast.Provider.Conversions.Factories;
using Kast.Provider.Media;
using Kast.Provider.Supports;

namespace Kast.Provider.Conversions
{
    public class ConversionContext
    {
        public enum ConversionType
        {
            FullConversion,
            SubtitlesOnly
        }

        public string Name => Media.Name;
        public Guid Id => Media.Id;
        public MediaStatus Status => Media.Status;
        public FactoryTarget? Target { get; private set; }
        public ConversionProgressEventArgs? Progress { get; private set; }
        
        public readonly ConversionType Type;
        public readonly StreamHandle? StreamHandle;
        
        internal bool BurnSubtitles { get; set; }
        internal readonly IMedia Media;
        internal readonly string TargetPath;
        internal readonly string? TemporaryTargetPath;
        internal readonly int? SubtitlesStreamIndex;
        internal readonly int AudioStreamIndex;

        public ConversionContext(IMedia media, SettingsProvider settingsProvider) 
        {
            if (media.Info == null)
                throw new ArgumentNullException(nameof(media), $"Media info must be defined");

            Media = media;
            
            Type = media.Status != MediaStatus.MissingSubtitles ? ConversionType.FullConversion : ConversionType.SubtitlesOnly;
            TargetPath = Path.Combine(IOSupport.CreateTargetDirectory(media.FilePath), $"_{Path.ChangeExtension(Media.FileName, ".mkv")}");

            if (Type == ConversionType.FullConversion)
            {
                TemporaryTargetPath = Path.ChangeExtension(TargetPath, ".tmp");
                StreamHandle = new StreamHandle(TemporaryTargetPath, TargetPath);
            }

            // Define streams preferred index
            var audioStreams = Media.Info.AudioStreams.ToList();
            var subtitlesStreams = Media.Info.SubtitleStreams.ToList();
            foreach (var setting in settingsProvider.Preferences)
            {
                var audioStream = audioStreams.Find(s => Utilities.InsensitiveCompare(s.Language, setting.Language));
                var subtitlesStream = subtitlesStreams.Find(s => Utilities.InsensitiveCompare(s.Language, setting.Subtitles));
                if (audioStream != null && (subtitlesStream != null || string.IsNullOrWhiteSpace(setting.Subtitles)))
                {
                    AudioStreamIndex = audioStreams.IndexOf(audioStream);
                    SubtitlesStreamIndex = subtitlesStreams?.IndexOf(subtitlesStream);
                    return;
                }
            }
        }

        internal void Update(ConversionProgressEventArgs? args = null, FactoryTarget? target = null)
        {
            Progress = args;
            Target = target;
            Media.UpdateStatus(args?.Percent);
        }
    }
}