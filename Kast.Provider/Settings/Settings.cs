using System.Net;
using System.Text.Json.Serialization;
using Kast.Provider.Supports;

namespace Kast.Provider
{
    public sealed class Settings : IEquatable<Settings>
    {
        private List<Preferences>? _preferences;
        public List<Preferences> Preferences
        {
            get => _preferences ??= new List<Preferences>();
            set => _preferences = value;
        }

        private Library? _library;
        public Library Library
        {
            get => _library ??= new Library();
            set => _library = value;
        }

        private Application? _application;
        public Application Application
        {
            get => _application ??= new Application();
            set => _application = value;
        }

        public bool Equals(Settings? other)
        {
            if (other == null) 
                return false;

            return Application.Equals(other.Application) 
                && Library.Equals(other.Library) 
                && Preferences.SequenceEqual(other.Preferences);
        }

        public override bool Equals(object? obj)
            => Equals(obj as Settings);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash *= 13 + Application.GetHashCode();
                hash *= 13 + Library.GetHashCode();
                hash *= 13 + Preferences.GetHashCode();
                return hash;
            }
        }
        #region IEquatable<Settings>

        #endregion
    }

    public sealed class Library : IEquatable<Library>
    {
        private HashSet<string>? _extensions;
        public HashSet<string> Extensions
        {
            get => _extensions ??= new HashSet<string>();
            set => _extensions = new HashSet<string>(value ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        private HashSet<string>? _directories;
        public HashSet<string> Directories
        {
            get => _directories ??= new HashSet<string>();
            set => _directories = new HashSet<string>(value ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        #region IEquatable<Preferences>
        public bool Equals(Library? other)
        {
            if (other == null)
                return false;

           if ((Extensions.Count != other.Extensions.Count) || (Directories.Count != other.Directories.Count))
                return false;
            foreach (var entry in Extensions)
                if (!other.Extensions.Contains(entry))
                    return false;
            foreach (var entry in Directories)
                if (!other.Directories.Contains(entry))
                    return false;

            return true;
        }

        public override bool Equals(object? obj)
            => Equals(obj as Library);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var ext in Extensions)
                    hash *= 13 + ext.GetHashCode();
                foreach (var dir in Directories)
                    hash *= 13 + dir.GetHashCode();
                return hash;
            }
        }
        #endregion
    }

    public sealed class Preferences : IEquatable<Preferences>
    {
        public string? Language { get; set; }
        public string? Subtitles { get; set; }

        #region IEquatable<Preferences>
        public bool Equals(Preferences? other)
        {
            if (other == null)
                return false;

            if (Utilities.InsensitiveCompare(Language, other.Language)
                && Utilities.InsensitiveCompare(Subtitles, other.Subtitles))
                return true;

            return false;
        }

        public override bool Equals(object? obj)
            => Equals(obj as Preferences);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash *= 13 + Language?.GetHashCode() ?? 0;
                hash *= 13 + Subtitles?.GetHashCode() ?? 0;
                return hash;
            }
        }
        #endregion
    }

    public sealed class Application : IEquatable<Application>
    {
        public string? YoutubeApiToken { get; set; }
        public string? YoutubeEndPoint { get; set; }
        public string? YoutubeEmbedBaseUrl { get; set; }

        private int _httpPort = 7279;
        public int HttpPort 
        { 
            get => _httpPort;
            set
            {
                if (value > 0)
                    _httpPort = value;
            }
        }

        private int _receiverRefreshInterval = 20000;
        public int ReceiverRefreshInterval
        {
            get => _receiverRefreshInterval;
            set
            {
                if (value > 0)
                    _receiverRefreshInterval = value;
            }
        }

        private string _cacheDirectory = Path.Combine(Path.GetTempPath(), "HomeKast");
        public string CacheDirectory 
        {
            get => _cacheDirectory;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _cacheDirectory = value;
            }
        }

        public string? MetadataApiToken { get; set; }
        public string? MetadataEndpoint { get; set; }

        public string? ImageBaseUrl { get; set; }

        private int _maxDegreeOfParallelism = 1;
        public int MaxDegreeOfParallelism 
        {
            get => _maxDegreeOfParallelism;
            set
            {
                if (value > 0)
                    _maxDegreeOfParallelism = value;
            }
        }

        public int? MediaInfoTimeout { get; set; }
        public int? MetadataTimeout { get; set; }
        public int? FileAccessTimeout { get; set; }

        private Uri? _uri;
        [JsonIgnore]
        public Uri Uri
        {
            get
            {
                if (_uri == null)
                    _uri = new Uri($"http://{Ip}:{HttpPort}");
                return _uri;
            }
        }

        private IPAddress? _ip;
        [JsonIgnore]
        public IPAddress Ip => _ip ??= Utilities.GetLocalIPAddress();

        #region IEquatable
        public bool Equals(Application? other)
        {
            if (other == null) return false;

            if (!Utilities.InsensitiveCompare(MetadataApiToken, other.MetadataApiToken)
                || !Utilities.InsensitiveCompare(CacheDirectory, other.CacheDirectory)
                || !Utilities.InsensitiveCompare(MetadataEndpoint, other.MetadataEndpoint)
                || !Utilities.InsensitiveCompare(Ip.ToString(), other.Ip.ToString())
                || !Utilities.InsensitiveCompare(Uri.ToString(), other.Uri.ToString())
                || HttpPort != other.HttpPort
                || MaxDegreeOfParallelism != other.MaxDegreeOfParallelism
                || ReceiverRefreshInterval != other.ReceiverRefreshInterval
                || MediaInfoTimeout != other.MediaInfoTimeout
                || MetadataTimeout != other.MetadataTimeout
                || FileAccessTimeout != other.FileAccessTimeout)
                return false;

            return true;
        }

        public override bool Equals(object? obj)
            => Equals(obj as Application);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash *= 13 + MetadataApiToken?.GetHashCode() ?? 0;
                hash *= 13 + CacheDirectory?.GetHashCode() ?? 0;
                hash *= 13 + MetadataEndpoint?.GetHashCode() ?? 0;
                hash *= 13 + Ip?.ToString()?.GetHashCode() ?? 0;
                hash *= 13 + Uri?.ToString()?.GetHashCode() ?? 0;
                hash *= 13 + HttpPort;
                hash *= 13 + MaxDegreeOfParallelism.GetHashCode();
                hash *= 13 + MediaInfoTimeout?.GetHashCode() ?? 0;
                hash *= 13 + MetadataTimeout?.GetHashCode() ?? 0;
                hash *= 13 + FileAccessTimeout?.GetHashCode() ?? 0;
                return hash;
            }
        }
        #endregion
    }
}
