using System.Web;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Kast.Provider.Media.IMDb;

namespace Kast.Provider.Media.YouTube
{
    public class YoutubeMetadataProvider : IMetadataProvider
    {
        private readonly bool _enabled;

        private readonly ILogger _logger;
        private readonly IMetadataProvider _metadataProvider;
        private readonly HttpClient _httpClient;
        private readonly SettingsProvider _settingsProvider;
        private readonly JsonSerializerOptions _options;

        private int MetadataTimeout => _settingsProvider.Application.MetadataTimeout ?? Constants.MetadataFetchTimeout;
        private string? YoutubeApiToken => _settingsProvider.Application.YoutubeApiToken;
        private string? YoutubeEndPoint => _settingsProvider.Application.YoutubeEndPoint;
        private string? YoutubeBaseUrl => _settingsProvider.Application.YoutubeEmbedBaseUrl;

        public YoutubeMetadataProvider(
            ILogger<IMetadataProvider> logger, 
            IMDbMetadataProvider metadataProvider,
            HttpClient httpClient, 
            SettingsProvider settingsProvider, 
            JsonSerializerOptions options)
        {
            _logger = logger;
            _metadataProvider = metadataProvider;
            _httpClient = httpClient;
            _settingsProvider = settingsProvider;
            _options = options;

            _enabled = !string.IsNullOrWhiteSpace(YoutubeApiToken)
                && !string.IsNullOrWhiteSpace(YoutubeEndPoint)
                && !string.IsNullOrWhiteSpace(YoutubeBaseUrl);
            
            if (!_enabled)
                _logger.LogDebug("Missing youtube settings to retrieve trailers... (skipping {me})", nameof(YoutubeMetadataProvider));
        }

        private readonly ConcurrentDictionary<string, string?> _store = new(StringComparer.OrdinalIgnoreCase);
        public async Task<Metadata?> GetAsync(IMedia media)
        {
            var metadata = await _metadataProvider.GetAsync(media);
            if (!_enabled || !string.IsNullOrWhiteSpace(metadata?.YoutubeEmbedUrl))
                return metadata;

            metadata ??= new Metadata();
            if (_store.TryGetValue(media.Name, out var embedUrl))
            {
                metadata.YoutubeEmbedUrl = embedUrl;
                return metadata;
            }

            var id = await GetYoutubeEmbedIdAsync(media);
            if (!string.IsNullOrWhiteSpace(id))
                metadata.YoutubeEmbedUrl = YoutubeBaseUrl + id;
            _store.TryAdd(media.Name, metadata.YoutubeEmbedUrl);

            return metadata;
        }

        private async Task<string?> GetYoutubeEmbedIdAsync(IMedia media)
        {
            try
            {
                using var canceller = new CancellationTokenSource(MetadataTimeout);
                var requestUri = $"{YoutubeEndPoint}/search?q={HttpUtility.UrlEncode(media.Name + " trailer")}&type=video&maxResults=1&key={YoutubeApiToken}";
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

                var response = await _httpClient.SendAsync(request, canceller.Token);
                response.EnsureSuccessStatusCode();

                var results = await response.Content.ReadFromJsonAsync<YoutubeCollectionDTO>(_options, canceller.Token);
                return results?.Items.SingleOrDefault()?.VideoId;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve trailer info for {media}", media);
            }

            return null;
        }
    }
}
