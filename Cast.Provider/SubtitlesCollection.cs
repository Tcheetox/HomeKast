using System;
using System.Collections;
using Xabe.FFmpeg;
using Cast.SharedModels.User;

namespace Cast.Provider
{
    public class SubtitlesCollection : IReadOnlyList<Subtitles>
    {
        private readonly IMediaInfo _info;
        private readonly UserProfile _userProfile;

        private List<Subtitles> _subtitles;

        public SubtitlesCollection(IMediaInfo info, UserProfile userProfile)
        {
            _info = info;
            _userProfile = userProfile;
            Refresh();
        }

        public void Refresh()
        {
            _subtitles = new();
            string mediaName = Path.GetFileNameWithoutExtension(_info.Path);

            // Build expected subtitles based on streams
            for (int i = 0; i < _info.SubtitleStreams.Count(); i++)
            {
                var sub = _info.SubtitleStreams.ElementAt(i);
                var displayLabel = PrettyName(sub);
                var prefixedName = mediaName.StartsWith('_') ? mediaName : '_' + mediaName;
                _subtitles.Add(new Subtitles()
                {
                    Index = i,
                    Label = sub.Language,
                    DisplayLabel = displayLabel,
                    LocalPath = Path.Combine(_userProfile.Application.StaticFilesDirectory, $"{prefixedName}_{sub.Language}_{i}_{displayLabel}.vtt")
                });
            }

            // Set preferred subtitles based on user preferences
            if (!_subtitles.Any())
                return;

            foreach (var pref in _userProfile.Preferences.Subtitles)
                if (_subtitles.FirstOrDefault(s => s.Label.ToLower() == pref.ToLower()) is Subtitles sub)
                {
                    sub.Active = true;
                    break;
                }
        }

        private static string PrettyName(ISubtitleStream subtitle)
        {
            string suffix = subtitle.Forced.HasValue && subtitle.Forced == 1 ? " (forced)" : string.Empty;
            string label = subtitle.Language?.ToLower() switch
            {
                "fre" => "French",
                "eng" => "English",
                "ger" => "German",
                _ => subtitle.Language?.ToLower() ?? "Unknown",
            };
            return label + suffix;
        }

        #region IReadOnlyList
        public int Count => ((IReadOnlyCollection<Subtitles>)_subtitles).Count;
        public Subtitles this[int index] => ((IReadOnlyList<Subtitles>)_subtitles)[index];
        public IEnumerator<Subtitles> GetEnumerator()
            => ((IEnumerable<Subtitles>)_subtitles).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)_subtitles).GetEnumerator();
        #endregion
    }
}
