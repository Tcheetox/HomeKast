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
        public MediaSettings Media => _settings.Media;

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

        public void Update(List<string> newExtensions, List<string> newDirectories)
        {
            if (_settings.Library.Directories.SequenceEqual(newDirectories)
                && _settings.Library.Extensions.SequenceEqual(newExtensions))
                return;
            
            _settings.Library = new LibrarySettings
            {
                Directories = newDirectories ?? Library.Directories,
                Extensions = newExtensions ?? Library.Extensions
            };

            try
            {
                string content = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_path, content);
                ProfileChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogError(ex, "Could not serialize updated {settings} of {profile}", nameof(Settings), nameof(UserProfile));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Could not write updated {settings} of {profile} to {path}", nameof(Settings), nameof(UserProfile), _path);
            }
        }
    }
}
