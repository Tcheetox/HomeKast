using System.Web;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class MetadataProvider : IMetadataProvider
    {
        protected readonly HttpClient HttpClient;
        protected readonly ILogger<MetadataProvider> Logger;
        protected readonly SettingsProvider SettingsProvider;
        protected readonly JsonSerializerOptions Options;

        protected int MetadataTimeout => SettingsProvider.Application.MetadataTimeout ?? Constants.MetadataFetchTimeout;
        private string MetadataEndpoint => SettingsProvider.Application.MetadataEndpoint!;
        private string ImageBaseUrl => SettingsProvider.Application.ImageBaseUrl!;

        public MetadataProvider(ILogger<MetadataProvider> logger, HttpClient httpClient, SettingsProvider settingsProvider, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(settingsProvider.Application.MetadataEndpoint))
                throw new ArgumentException($"{nameof(settingsProvider)} must define a valid endpoint to retrieve {nameof(Metadata)}");

            Logger = logger;
            HttpClient = httpClient;
            SettingsProvider = settingsProvider;
            Options = options;
        }

        public async Task<Metadata?> GetAsync(IMedia media)
        {
            if (_store.TryGetValue(media.Name, out var metadata) || (metadata = media.Metadata) != null)
                return metadata;

            metadata = await GetInternalAsync(media);
            _store.TryAdd(media.Name, metadata);

            return metadata;
        }

        private readonly ConcurrentDictionary<string, Metadata?> _store = new(StringComparer.OrdinalIgnoreCase);
        protected virtual async Task<Metadata?> GetInternalAsync(IMedia media)
        {
            try
            {
                using var cancellation = new CancellationTokenSource(MetadataTimeout);
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{MetadataEndpoint}?query={HttpUtility.UrlEncode(media.Name)}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SettingsProvider.Application.MetadataApiToken);

                using var response = await HttpClient.SendAsync(request, cancellation.Token);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellation.Token);
                var requests = JsonSerializer.Deserialize<MetadataCollectionDTO>(content, Options);
                var result = requests?.Results?.FirstOrDefault();

                if (result != null)
                    return new()
                    {
                        BackdropUrl = ImageBaseUrl + result.Backdrop,
                        Description = result.Description,
                        MediaType = result.MediaType,
                        OriginalTitle = result.OriginalTitle,
                        Vote = result.Vote,
                        Released = DateTime.TryParse(result.Released, out DateTime released) ? released : null,
                        ImageUrl = ImageBaseUrl + result.Poster
                    };
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogError(ex, "Failed to retrieve metadata for {media} within {timeout} ms", media, MetadataTimeout);
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "Could not deserialize metadata for {media}", media);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error retrieving metadata for {media}", media);
            }

            return null;
        }
    }
}
