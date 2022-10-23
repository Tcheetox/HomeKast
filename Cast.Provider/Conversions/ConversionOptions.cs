
using Cast.SharedModels.User;
using System.IO;

namespace Cast.Provider.Conversions
{
    public enum ConversionType
    {
        FullConversion,
        SubtitlesOnly
    }

    public class ConversionOptions
    {
        public bool BurnSubtitles { get; set; }
        public ConversionType ConversionType { get; init; }
        public IMedia Media { get; init; }

        public string Extension => BurnSubtitles ? ".mp4" : ".mkv";
        public string TemporaryPath => Path.Combine(Path.GetTempPath(), Media.Id + Extension);

        public string TargetPath
        {
            get
            {
                if (ConversionType == ConversionType.SubtitlesOnly)
                    return Media.LocalPath;
                else
                {
                    string targetDirectory = Path.GetDirectoryName(Media.LocalPath)!;
                    string fileName = "_"
                        + Path.GetFileNameWithoutExtension(Media.LocalPath)
                        + Extension;
                    return Path.Combine(targetDirectory, fileName);
                }
            }
        }

        public void DeleteTemporaryFiles()
        {
            try
            {
                foreach (var path in Media.Subtitles.Select(s => s.TemporaryPath).Where(p => File.Exists(p)))
                    File.Delete(path);
                if (File.Exists(TemporaryPath))
                    File.Delete(TemporaryPath);
            }
            catch (Exception)
            {
                // No need to add trace
            }
        }

        public int AudioStreamIndex { get; private set; }
        public int? SubtitlesStreamIndex { get; private set; }
        public void SetPreferences(List<Settings.PreferencesSettings> preferences)
        {
            var audioStreams = Media.Info.AudioStreams.ToList();
            var subtitlesStreams = Media.Info.SubtitleStreams.ToList();
            foreach (var setting in preferences)
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
    }
}
