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
        public readonly StreamHandle? Handle;
        
        internal bool BurnSubtitles { get; set; }
        internal readonly IMedia Media;
        internal readonly string TargetPath;
        internal readonly string? TemporaryTargetPath;

        private (int, int, int?)? _streamIndices;
        internal (int, int, int?) StreamIndices
        {
            get
            {
                if (_streamIndices.HasValue)
                    return _streamIndices.Value;

                int audioStreamIndex = 0; int? subtitlesStreamIndex = null;
                var audioStreams = Media.Info!.AudioStreams.ToList();
                var subtitlesStreams = Media.Info.SubtitleStreams.ToList();
                foreach (var setting in _settingsProvider.Preferences)
                {
                    var preferredAudioStream = audioStreams.Find(s => Utilities.InsensitiveCompare(s.Language, setting.Language));
                    var preferredSubtitlesStream = subtitlesStreams.Find(s => Utilities.InsensitiveCompare(s.Language, setting.Subtitles));

                    if (preferredAudioStream != null && (preferredSubtitlesStream != null || string.IsNullOrWhiteSpace(setting.Subtitles)))
                    {
                        audioStreamIndex = audioStreams.IndexOf(preferredAudioStream);
                        subtitlesStreamIndex = subtitlesStreams.IndexOf(preferredSubtitlesStream);
                        break;
                    }
                }

                _streamIndices = (0, audioStreamIndex, subtitlesStreamIndex);
                return _streamIndices.Value;
            }
        }

        private readonly SettingsProvider _settingsProvider;
        public ConversionContext(IMedia media, SettingsProvider settingsProvider) 
        {
            _settingsProvider = settingsProvider;
            Media = media;            
            Type = media.Status != MediaStatus.MissingSubtitles ? ConversionType.FullConversion : ConversionType.SubtitlesOnly;
            TargetPath = Path.Combine(IOSupport.CreateTargetDirectory(media.FilePath), $"_{Path.ChangeExtension(Media.FileName, ".mkv")}");

            if (Type == ConversionType.FullConversion)
            {
                TemporaryTargetPath = Path.ChangeExtension(TargetPath, ".tmp");
                Handle = new StreamHandle(TemporaryTargetPath, TargetPath);
            }
        }

        internal void Update(ConversionProgressEventArgs? args = null, FactoryTarget? target = null)
        {
            Progress = args;
            Target = target;
            Media.UpdateStatus(args?.Percent, target);
        }
    }
}