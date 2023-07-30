using System.Text.Json.Serialization;
using System.Collections;
using Xabe.FFmpeg;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class SubtitlesList : IReadOnlyList<Subtitles>
    {
        private readonly List<Subtitles> _subtitles = new();

        [JsonConstructor]
        public SubtitlesList(IEnumerable<Subtitles> subtitles)
        {
            _subtitles.AddRange(subtitles);
        }

        public SubtitlesList(IMediaInfo info, SettingsProvider settingsProvider)
        {
            var mediaName = Path.GetFileNameWithoutExtension(info.Path);
            var targetDirectory = IOSupport.CreateTargetDirectory(info.Path);

            // Build expected subtitles based on streams
            for (int i = 0; i < info.SubtitleStreams.Count(); i++)
            {
                var stream = info.SubtitleStreams.ElementAt(i);
                var language = GetLanguage(stream);
                var name = stream.Forced == 1 ? $"{language} (forced)" : language;
                var filePath = Path.Combine(targetDirectory, $"{mediaName.TrimStart('_')}_{name.Replace(" ", "_")}_{i}.vtt");
                var preferred = settingsProvider.Preferences.Exists(e => Utilities.InsensitiveCompare(stream.Language, e.Subtitles));
                _subtitles.Add(new Subtitles(i, name, language, filePath, preferred));
            }
        }

        public bool IsExtractionRequired() => Count > 0 && !this.All(s => !string.IsNullOrWhiteSpace(s.FilePath) && File.Exists(s.FilePath));

        private static string GetLanguage(ISubtitleStream subtitle)
            => subtitle.Language?.ToLower() switch
            {
                "fre" => "French",
                "eng" => "English",
                "ger" => "German",
                "ita" => "Italian",
                "swe" => "Swedish",
                "jpn" => "Japanese",
                "kor" => "Korean",
                "chi" => "Chinese",
                "por" => "Portuguese",
                _ => subtitle.Language?.ToLower() ?? "Unknown",
            };

        #region IReadOnlyList<Subtitles>
        public int Count => ((IReadOnlyCollection<Subtitles>)_subtitles).Count;
        public Subtitles this[int index] => _subtitles[index];
        public IEnumerator<Subtitles> GetEnumerator() => _subtitles.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}
