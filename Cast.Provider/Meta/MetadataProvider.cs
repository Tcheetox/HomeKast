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
            var timeout = 1000;
            try
            {
                cancellation.CancelAfter(timeout);
                var content = await _client.GetStringAsync($"{_baseUrl}?query={HttpUtility.UrlEncode(lookup)}", cancellation.Token);
                var requests = JsonConvert.DeserializeObject<MediaMetadataResults>(content);
                metadata = requests?.Results.FirstOrDefault() ?? new Metadata();
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Failed to retrieve metadata for {lookup} within {timeout} ms", lookup, timeout);
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogError(ex, "Could not deserialize metadata for {lookup}", lookup);
            }
            finally
            {
                cancellation.Dispose();
            }

            // Adjusting image
            metadata.ImageUrl 
                = metadata.HasImage
                ? "https://image.tmdb.org/t/p/original" + metadata.Image
                : "/media/notfound.png";

            return metadata;
        }
    }
}
