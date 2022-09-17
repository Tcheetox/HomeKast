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

        // TODO: logging
        // TODO: try-catching around IO
        public UserProfile(ILogger<UserProfile> logger)
        {
            _logger = logger;

            if (File.Exists(_path))
            {
                var content = File.ReadAllText(_path);
                _settings = JsonConvert.DeserializeObject<Settings>(content)!;
            }
        }

        public event EventHandler ProfileChanged;

        public void Update(List<string>? newExtensions = null, List<string>? newDirectories = null)
        {
            _settings.Library = new LibrarySettings
            {
                Directories = newDirectories ?? Library.Directories,
                Extensions = newExtensions ?? Library.Extensions
            };

            string content = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(_path, content);

            ProfileChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
