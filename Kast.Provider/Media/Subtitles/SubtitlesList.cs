using System.Text.Json.Serialization;
using System.Collections;
using Xabe.FFmpeg;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class SubtitlesList : IList<Subtitles>
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
                var preferred = settingsProvider.Preferences.Any(e => stream.Language?.Equals(e.Subtitles, StringComparison.OrdinalIgnoreCase) ?? false);
                _subtitles.Add(new Subtitles(i, name, language, filePath, preferred));
            }
        }

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

        #region OnChange
        public delegate void SubtitlesChangeEventHandler(object sender, Subtitles? e);
        public event SubtitlesChangeEventHandler? OnSubtitlesChange;
        #endregion

        #region IList<Subtitles>
        public Subtitles this[int index]
        {
            get => _subtitles[index];
            set
            {
                _subtitles[index] = value;
                OnSubtitlesChange?.Invoke(this, value);
            }
        }
        public int Count => _subtitles.Count;
        public bool IsReadOnly => ((ICollection<Subtitles>)_subtitles).IsReadOnly;

        public void Add(Subtitles item)
        {
            _subtitles.Add(item);
            OnSubtitlesChange?.Invoke(this, item);
        }

        public void Clear()
        {
            _subtitles.Clear();
            OnSubtitlesChange?.Invoke(this, null);
        }

        public bool Contains(Subtitles item) => _subtitles.Contains(item);
        public void CopyTo(Subtitles[] array, int arrayIndex) => _subtitles.CopyTo(array, arrayIndex);
        public IEnumerator<Subtitles> GetEnumerator() => _subtitles.GetEnumerator();
        public int IndexOf(Subtitles item) => _subtitles.IndexOf(item);

        public void Insert(int index, Subtitles item)
        {
            _subtitles.Insert(index, item);
            OnSubtitlesChange?.Invoke(this, item);
        }

        public bool Remove(Subtitles item)
        {
            if (_subtitles.Remove(item))
            {
                OnSubtitlesChange?.Invoke(this, item);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            var item = _subtitles[index];
            _subtitles.RemoveAt(index);
            OnSubtitlesChange?.Invoke(this, item);
        }

        IEnumerator IEnumerable.GetEnumerator() => _subtitles.GetEnumerator();
        #endregion
    }
}
