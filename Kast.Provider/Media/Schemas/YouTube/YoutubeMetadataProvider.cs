using System.Web;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Kast.Provider.Media.IMDb;

namespace Kast.Provider.Media.YouTube
{
    public class YoutubeMetadataProvider : IMDbMetadataProvider
    {
        public YoutubeMetadataProvider(ILogger<IMetadataProvider> logger, HttpClient httpClient, SettingsProvider settingsProvider, JsonSerializerOptions options)
            : base(logger, httpClient, settingsProvider, options)
        { }

        private readonly ConcurrentDictionary<string, string?> _store = new(StringComparer.OrdinalIgnoreCase);
        public override async Task<Metadata?> GetAsync(IMedia media)
        {
            var metadata = await base.GetAsync(media);

            if (string.IsNullOrWhiteSpace(SettingsProvider.Application.YoutubeApiToken)
                || string.IsNullOrWhiteSpace(SettingsProvider.Application.YoutubeEndpoint)
                || string.IsNullOrWhiteSpace(SettingsProvider.Application.YoutubeEmbedBaseUrl))
            {
                Logger.LogDebug("Missing youtube settings to retrieve trailers... (skipping {me})", nameof(YoutubeMetadataProvider));
                return metadata;
            }

            if (metadata == null || !string.IsNullOrWhiteSpace(metadata.YoutubeEmbedUrl))
                return metadata;

            if (_store.TryGetValue(media.Name, out var embedUrl))
            {
                metadata.YoutubeEmbedUrl = embedUrl;
                return metadata;
            }

            var id = await GetYoutubeEmbedIdAsync(media);
            if (!string.IsNullOrWhiteSpace(id))
                metadata.YoutubeEmbedUrl = SettingsProvider.Application.YoutubeEmbedBaseUrl + id;
            _store.TryAdd(media.Name, metadata.YoutubeEmbedUrl);

            return metadata;
        }

        private async Task<string?> GetYoutubeEmbedIdAsync(IMedia media)
        {
            try
            {
                using var canceller = new CancellationTokenSource(MetadataTimeout);
                var requestUri = $"{SettingsProvider.Application.YoutubeEndpoint}/search?q={HttpUtility.UrlEncode(media.Name + " trailer")}&type=video&maxResults=1&key={SettingsProvider.Application.YoutubeApiToken}";
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

                var response = await HttpClient.SendAsync(request, canceller.Token);
                response.EnsureSuccessStatusCode();

                var results = await response.Content.ReadFromJsonAsync<YoutubeCollectionDTO>(Options, canceller.Token);
                return results?.Items.SingleOrDefault()?.VideoId;

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to retrieve trailer info for {media}", media);
            }

            return null;
        }
    }
}
