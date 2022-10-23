using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Cast.SharedModels.User.Settings;

namespace Cast.SharedModels.User
{
    public class UserProfile
    {
        public ConnectionSettings ConnectionStrings => _settings.ConnectionStrings;
        public LibrarySettings Library => _settings.Library;
        public ApplicationSettings Application => _settings.Application;
        public List<PreferencesSettings> Preferences => _settings.Preferences;

        private readonly Settings _settings;
        private readonly ILogger<UserProfile> _logger;
        private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usersettings.json");

        public UserProfile(ILogger<UserProfile> logger)
        {
            _logger = logger;

            if (!File.Exists(_path))
                throw new ArgumentException($"User settings could not be found {_path}");
            else
            {
                var content = File.ReadAllText(_path);
                _settings = JsonConvert.DeserializeObject<Settings>(content)!;
            }
        }

        public event EventHandler ProfileChanged;

        public bool TryUpdate(string? staticFileDirectory,
            string? subtitles,
            string? languages,
            List<string> directories)
        {
            if (staticFileDirectory?.ToLower() == Application.StaticFilesDirectory.ToLower()
                && Library.Directories.SequenceEqual(directories)
                && string.Join(';', Preferences.Select(p => p.Language)).ToLower() == languages?.ToLower()
                && string.Join(';', Preferences.Select(p => p.Subtitles)).ToLower() == subtitles?.ToLower())
                return false;

            var preferences = new List<PreferencesSettings>();
            var splittedLanguages = languages?.Split(';') ?? Array.Empty<string>();
            var splittedSubtitles = subtitles?.Split(';') ?? Array.Empty<string>();
            for (int i = 0; i < Math.Max(splittedLanguages.Length, splittedSubtitles.Length); i++)
                preferences.Add(new PreferencesSettings()
                {
                    Language = splittedLanguages.Length > i ? splittedLanguages[i] : string.Empty,
                    Subtitles = splittedSubtitles.Length > i ? splittedSubtitles[i] : string.Empty
                });

            Application.StaticFilesDirectory = staticFileDirectory!;
            Library.Directories = directories;
            _settings.Preferences = preferences;

            try
            {
                string content = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_path, content);
                ProfileChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogError(ex, "Could not serialize updated {settings} of {profile}", nameof(Settings), nameof(UserProfile));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Could not write updated {settings} of {profile} to {path}", nameof(Settings), nameof(UserProfile), _path);
            }

            return false;
        }
    }
}
