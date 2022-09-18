using System.Net;
using System.Text.Json.Serialization;

namespace Cast.SharedModels.User
{
    public class Settings
    {
        public MediaSettings Media { get; set; }
        public class MediaSettings
        {
            public List<PreferencesSettings> Preferences { get; set; }
            public class PreferencesSettings
            {
                public string Language { get; set; }
                public string Subtitles { get; set; }
            }
        }

        public LibrarySettings Library { get; set; }
        public class LibrarySettings
        {
            public List<string> Extensions { get; set; }
            public List<string> Directories { get; set; }
            public bool IsMonitoredExtensions(string extension)
                => Extensions?.Any(e => e.ToLower() == extension.ToLower()) ?? false;
        }

        public ConnectionSettings ConnectionStrings { get; set; }
        public class ConnectionSettings
        {
            public string DefaultConnection { get; set; }
        }

        public ApplicationSettings Application { get; set; }
        public class ApplicationSettings
        {
            public string ApiToken { get; set; }
            public int Port { get; set; }

            public string BaseUrl { get; set; }

            private Uri _uri;
            [JsonIgnore]
            public Uri Uri
            {
                get
                {
                    if (_uri == null)
                        _uri = new Uri($"http://{IP}:{Port}");
                    return _uri;
                }
            }

            private IPAddress _ip;
            [JsonIgnore]
            public IPAddress IP => _ip ??= Helper.GetLocalIPAddress();
        }
    }
}
