using System.Web;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Kast.Provider.Supports;
using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public class MetadataProvider : IMetadataProvider
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        protected readonly ILogger<MetadataProvider> Logger;
        protected readonly SettingsProvider SettingsProvider;
        protected readonly JsonSerializerOptions Options;

        public MetadataProvider(ILogger<MetadataProvider> logger, SettingsProvider settingsProvider, JsonSerializerOptions options)
        {
            Logger = logger;
            SettingsProvider = settingsProvider;
            Options = options;

            if (string.IsNullOrWhiteSpace(settingsProvider.Application.BaseUrl))
                throw new ArgumentException($"{nameof(settingsProvider)} must define a valid URL");

            _baseUrl = settingsProvider.Application.BaseUrl;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SettingsProvider.Application.ApiToken);
        }

        public virtual async Task<Metadata> GetAsync(IMediaInfo info, string lookup)
        {
            using (MassTimer.Measure("GetMetadata"))
            {
                Metadata? metadata = null;
                var cancellation = new CancellationTokenSource();
                try
                {
                    cancellation.CancelAfter(SettingsProvider.Application.MetadataTimeout ?? 1000);
                    var content = await _client.GetStringAsync($"{_baseUrl}?query={HttpUtility.UrlEncode(lookup)}", cancellation.Token);
                    var requests = JsonSerializer.Deserialize<MetadataResults>(content, Options);
                    metadata = requests?.Results.FirstOrDefault()!;
                }
                catch (OperationCanceledException ex)
                {
                    Logger.LogError(ex, "Failed to retrieve metadata for {lookup} within {timeout} ms", lookup, SettingsProvider.Application.MetadataTimeout);
                }
                catch (JsonException ex)
                {
                    Logger.LogError(ex, "Could not deserialize metadata for {lookup}", lookup);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unexpected error retrieving metadata for {lookup}", lookup);
                }
                finally
                {
                    metadata ??= metadata ?? new Metadata();
                    cancellation.Dispose();
                }

                // Adjust Inage
                if (!string.IsNullOrWhiteSpace(metadata.Poster) || !string.IsNullOrWhiteSpace(metadata.Backdrop))
                    metadata.Image = "https://image.tmdb.org/t/p/original" + (metadata.Poster ?? metadata.Backdrop);

                return metadata;
            }
        }
    }
}
