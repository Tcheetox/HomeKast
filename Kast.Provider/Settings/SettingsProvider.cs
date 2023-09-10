using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kast.Provider
{
    public class SettingsProvider
    {
        public Library Library => Settings.Library;
        public Application Application => Settings.Application;
        public List<Preferences> Preferences => Settings.Preferences;
        public Settings Settings { get; private set; }

        private readonly ILogger<SettingsProvider> _logger;
        private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usersettings.json");
        private readonly JsonSerializerOptions _options;

        public SettingsProvider(ILogger<SettingsProvider> logger, JsonSerializerOptions serializerOptions)
        {
            _logger = logger;
            _options = new JsonSerializerOptions(serializerOptions) { WriteIndented = true };
            if (!File.Exists(_path))
                throw new ArgumentException($"User settings could not be found {_path}");

            var content = File.ReadAllText(_path);
            Settings = JsonSerializer.Deserialize<Settings>(content, _options)!;
            Directory.CreateDirectory(Settings.Application.CacheDirectory);
        }

        public event EventHandler<Settings>? SettingsChanged;

        public bool TryUpdate(Settings settings)
        {
            if (settings.Equals(Settings)) 
                return false;

            try
            {
                string content = JsonSerializer.Serialize(settings, _options);
                File.WriteAllText(_path, content);
                Directory.CreateDirectory(settings.Application.CacheDirectory);
                var previousSettings = Settings;
                Settings = settings;
                SettingsChanged?.Invoke(this, previousSettings);
                return true;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Could not serialize updated {settings}", nameof(Settings));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Could not write updated {settings} to {path}", nameof(Settings), _path);
            }

            return false;
        }
    }
}
