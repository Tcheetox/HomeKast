using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace Cast.SharedModels.User
{
    public class Settings
    {
        public LibrarySettings Library { get; set; }
        public class LibrarySettings
        {
            public List<string> Extensions { get; set; }
            public List<string> Directories { get; set; }
        }

        public ConnectionSettings ConnectionStrings { get; set; }
        public class ConnectionSettings
        {
            public string DefaultConnection { get; set; }
        }

        public ApplicationSettings Application { get; set; }
        public class ApplicationSettings
        {
            public string ApiKey { get; set; }
            public int Port { get; set; }

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
            public IPAddress IP
            {
                get
                {
                    if (_ip == null)
                        _ip = Helper.GetIPAddress();
                    return _ip;
                }
            }
        }
    }
}
