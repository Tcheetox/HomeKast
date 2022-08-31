using System;
using System.Text.Json;
using System.Web;
using Cast.Provider.Metadata;
using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cast.Provider.MediaInfoProvider
{
    public class MetadataProvider : IMetadataProvider
    {
        private const string BASE_URL = "https://api.themoviedb.org/3/search/multi";
        private static readonly HttpClient _client = new();

        private readonly ILogger _logger;
        private readonly UserProfile _userProfile;

        public MetadataProvider(ILogger<MetadataProvider> logger, UserProfile userProfile)
        {
            _logger = logger;
            _userProfile = userProfile;
        }
        // TODO: header api key du con!
        public async Task<Metadata.Metadata?> GetMetadataAsync(string lookup)
        {
            if (string.IsNullOrWhiteSpace(lookup))
                return null;

            var content = await _client.GetStringAsync($"{BASE_URL}?api_key={_userProfile.Application.ApiKey}&query={HttpUtility.UrlEncode(lookup)}");
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var requests = JsonConvert.DeserializeObject<MediaMetadataResults>(content);
            if (requests == null || requests.TotalResults == 0)
                return null;

            return requests.Results.FirstOrDefault();
        }
    }
}
