using System.Web;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public abstract class MetadataProvider : IMetadataProvider
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        protected readonly ILogger<MetadataProvider> Logger;
        protected readonly SettingsProvider SettingsProvider;
        protected readonly JsonSerializerOptions Options;

        protected MetadataProvider(ILogger<MetadataProvider> logger, HttpClient httpClient, SettingsProvider settingsProvider, JsonSerializerOptions options)
        {
            Logger = logger;
            SettingsProvider = settingsProvider;
            Options = options;

            if (string.IsNullOrWhiteSpace(settingsProvider.Application.BaseUrl))
                throw new ArgumentException($"{nameof(settingsProvider)} must define a valid URL");

            _baseUrl = settingsProvider.Application.BaseUrl;
            _client = httpClient; 
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SettingsProvider.Application.ApiToken);
        }

        private readonly ConcurrentDictionary<string, Metadata?> _store = new(StringComparer.OrdinalIgnoreCase);
        public virtual async Task<Metadata?> GetAsync(IMedia media)
        {
            if (_store.TryGetValue(media.Name, out var metadata) || (metadata = media.Metadata) != null)
                return metadata;

            var cancellation = new CancellationTokenSource();
            try
            {
                using (MassTimer.Measure("GetMetadata"))
                {
                    cancellation.CancelAfter(SettingsProvider.Application.MetadataTimeout ?? Constants.MetadataFetchTimeout);
                    var content = await _client.GetStringAsync($"{_baseUrl}?query={HttpUtility.UrlEncode(media.Name)}", cancellation.Token);
                    var requests = JsonSerializer.Deserialize<MetadataResultsDTO>(content, Options);
                    var result = requests?.Results?.FirstOrDefault();
                    if (result != null)
                    {
                        metadata = new()
                        {
                            Backdrop = result.Backdrop,
                            Description = result.Description,
                            MediaType = result.MediaType,
                            OriginalTitle = result.OriginalTitle,
                            Vote = result.Vote,
                            Released = DateTime.TryParse(result.Released, out DateTime released) ? released : null,
                            ImageUrl = result.Poster
                        };
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogError(ex, "Failed to retrieve metadata for {media} within {timeout} ms", media, SettingsProvider.Application.MetadataTimeout);
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex, "Could not deserialize metadata for {media}", media);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error retrieving metadata for {media}", media);
            }
            finally
            {
                cancellation.Dispose();
            }

            return metadata;
        }
    }
}
