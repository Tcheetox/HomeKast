using System;
using System.Net.Http.Headers;
using System.Web;
using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cast.Provider.Meta
{
    public class MetadataProvider : IMetadataProvider
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly string _baseUrl;

        public MetadataProvider(ILogger<MetadataProvider> logger, UserProfile userProfile)
        {
            _logger = logger;
            _baseUrl = userProfile.Application.BaseUrl;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userProfile.Application.ApiToken);
        }

        public async Task<Metadata> GetMetadataAsync(string lookup)
        {
            Metadata metadata = null!;
            var cancellation = new CancellationTokenSource();

            try
            {
                cancellation.CancelAfter(1000);
                var content = await _client.GetStringAsync($"{_baseUrl}?query={HttpUtility.UrlEncode(lookup)}", cancellation.Token);
                if (string.IsNullOrWhiteSpace(content))
                    return Metadata.Default;

                var requests = JsonConvert.DeserializeObject<MediaMetadataResults>(content);
                metadata = requests?.Results.FirstOrDefault()!;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Failed to retrieve metadata for {lookup}", lookup);
            }
            finally
            {
                cancellation.Dispose();
            }

            if (metadata == null)
                return Metadata.Default;

            metadata.Backdrop
                = string.IsNullOrWhiteSpace(metadata.Backdrop)
                ? Metadata.Default.Backdrop
                : "https://image.tmdb.org/t/p/original" + metadata.Backdrop;

            return metadata;
        }
    }
}
