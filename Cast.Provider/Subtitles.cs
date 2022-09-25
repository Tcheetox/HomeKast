using Cast.SharedModels.User;
using Cast.SharedModels;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xabe.FFmpeg;

namespace Cast.Provider
{
    [DebuggerDisplay("{Label}")]
    public class Subtitles
    {
        private Subtitles()
        { }

        public int Index { get; init; }
        public string Label { get; init; }
        public string DisplayLabel { get; init; }
        public bool Active { get; set; }

        [JsonIgnore]
        public string LocalPath { get; init; }

        public bool Exists() => !string.IsNullOrEmpty(LocalPath) && File.Exists(LocalPath);

        public static List<Subtitles> Create(IMediaInfo info, UserProfile userProfile)
        {
            List<Subtitles> subtitles = new();
            string mediaName = Path.GetFileNameWithoutExtension(info.Path);

            // Build expected subtitles based on streams
            for (int i = 0; i < info.SubtitleStreams.Count(); i++)
            {
                var sub = info.SubtitleStreams.ElementAt(i);
                var displayLabel = PrettyName(sub);
                subtitles.Add(new Subtitles()
                {
                    Index = i,
                    Label = sub.Language,
                    DisplayLabel = displayLabel,
                    LocalPath = Path.Combine(userProfile.Application.StaticFilesDirectory, $"{mediaName}_{sub.Language}_{i}_{displayLabel}.vtt")
                });
            }

            // Set preferred subtitles based on user preferences
            if (!subtitles.Any())
                return subtitles;

            foreach (var pref in userProfile.Preferences.Subtitles)
                if (subtitles.FirstOrDefault(s => s.Label.ToLower() == pref.ToLower()) is Subtitles sub)
                {
                    sub.Active = true;
                    break;
                }

            return subtitles;
        }

        private static string PrettyName(ISubtitleStream subtitle)
        {
            string suffix = subtitle.Forced.HasValue && subtitle.Forced == 1 ? " (forced)" : string.Empty;
            string label = subtitle.Language.ToLower() switch
            {
                "fre" => "French",
                "eng" => "English",
                "ger" => "German",
                _ => subtitle.Language.Capitalize(),
            };
            return label + suffix;
        }
    }
}
