using Newtonsoft.Json;
using System.Net;

namespace Cast.SharedModels.User
{
    public class Settings
    {
        public List<PreferencesSettings> Preferences { get; set; }
        public class PreferencesSettings
        {
            public string Language { get; set; }
            public string Subtitles { get; set; }
        }

        public LibrarySettings Library { get; set; }
        public class LibrarySettings
        {
            public List<string> Extensions { get; set; }
            public List<string> Directories { get; set; }
            public bool IsMonitoredExtensions(string extension)
                => Extensions?.Any(e => e.ToLower() == extension.ToLower()) ?? false;
            public int SlidingExpirationInMinutes { get; set; }
            public int AbsoluteExpirationInHours { get; set; }
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
            public string StaticFilesDirectory { get; set; }
            public string BaseUrl { get; set; }
            public int MaxDegreeOfParallelism { get; set; }
            public int MediaInfoTimeout { get; set; }
            public int MetadataTimeout { get; set; }
            public int FileAccessTimeout { get; set; }

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
