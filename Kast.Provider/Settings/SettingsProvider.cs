using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kast.Provider
{
    public class SettingsProvider
    {
        // TODO: port for both HTTP and HTTPS please...
        public Library Library => Settings.Library;
        public Application Application => Settings.Application;
        public List<Preferences> Preferences => Settings.Preferences;
        public Settings Settings { get; private set; }

        private readonly ILogger<SettingsProvider> _logger;
        private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usersettings.json");
        private readonly JsonSerializerOptions _serializerOptions;

        public SettingsProvider(ILogger<SettingsProvider> logger, JsonSerializerOptions serializerOptions)
        {
            _logger = logger;
            _serializerOptions = new JsonSerializerOptions(serializerOptions) { WriteIndented = true };

            if (!File.Exists(_path))
                throw new ArgumentException($"User settings could not be found {_path}");

            var content = File.ReadAllText(_path);
            Settings = JsonSerializer.Deserialize<Settings>(content)!;
            Directory.CreateDirectory(Settings.Application.CacheDirectory);
        }

        public event EventHandler<Settings>? SettingsChanged;

        public bool TryUpdate(Settings settings)
        {
            if (settings.Equals(Settings)) 
                return false;

            try
            {
                string content = JsonSerializer.Serialize(Settings, _serializerOptions);
                File.WriteAllText(_path, content);
                Directory.CreateDirectory(settings.Application.CacheDirectory);
                SettingsChanged?.Invoke(this, settings);
                Settings = settings;
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
