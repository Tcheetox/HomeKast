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
        public PreferencesSettings Preferences => _settings.Preferences;

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

        public bool TryUpdate(string staticFileDirectory,
            List<string> subtitles, 
            List<string> languages, 
            List<string> directories)
        {
            if (staticFileDirectory == Application.StaticFilesDirectory
                && Library.Directories.SequenceEqual(directories)
                && Preferences.Subtitles.SequenceEqual(subtitles)
                && Preferences.Language.SequenceEqual(languages))
                return false;

            Application.StaticFilesDirectory = staticFileDirectory;
            Library.Directories = directories;
            Preferences.Subtitles = subtitles;
            Preferences.Language = languages;

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
