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
        private readonly string _baseUrl;

        protected readonly ILogger<MetadataProvider> Logger;
        protected readonly UserProfile UserProfile;

        public MetadataProvider(ILogger<MetadataProvider> logger, UserProfile userProfile)
        {
            Logger = logger;
            UserProfile = userProfile;
            _baseUrl = userProfile.Application.BaseUrl;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserProfile.Application.ApiToken);
        }

        public virtual async Task<Metadata> GetMetadataAsync(string lookup)
        {
            Metadata metadata = null!;
            var cancellation = new CancellationTokenSource();
            try
            {
                cancellation.CancelAfter(UserProfile.Application.MetadataTimeout);
                var content = await _client.GetStringAsync($"{_baseUrl}?query={HttpUtility.UrlEncode(lookup)}", cancellation.Token);
                var requests = JsonConvert.DeserializeObject<MediaMetadataResults>(content);
                metadata = requests?.Results.FirstOrDefault()!;
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogError(ex, "Failed to retrieve metadata for {lookup} within {timeout} ms", lookup, UserProfile.Application.MetadataTimeout);
            }
            catch (JsonSerializationException ex)
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

            // Adjusting image
            metadata.ImageUrl
                = metadata.HasImage
                ? "https://image.tmdb.org/t/p/original" + metadata.Image
                : "/media/notfound.png";

            return metadata;
        }
    }
}
