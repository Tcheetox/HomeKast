using Kast.Provider.Media.YouTube;
using Kast.Provider.Supports;
using Microsoft.Extensions.Logging;

namespace Kast.Provider.Media
{
    public class LocalMetadataProvider : IMetadataProvider
    {
        private readonly IMetadataProvider _metadataProvider;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly SettingsProvider _settingsProvider;

        private int MetadataTimeout => _settingsProvider.Application.MetadataTimeout ?? Constants.MetadataFetchTimeout;

        public LocalMetadataProvider(
            ILogger<IMetadataProvider> logger,
            YoutubeMetadataProvider metadataProvider,
            HttpClient httpClient,
            SettingsProvider settingsProvider)
        {
            _logger = logger;
            _metadataProvider = metadataProvider;
            _httpClient = httpClient;
            _settingsProvider = settingsProvider;
        }

        public async Task<Metadata?> GetAsync(IMedia media)
        {
            var metadata = await _metadataProvider.GetAsync(media);
            if (metadata == null || !metadata.HasMissingInfo)
                return metadata;

            try
            {
                using var canceller = new CancellationTokenSource(MetadataTimeout);
                var directory = IOSupport.CreateTargetDirectory(media.FilePath);
                var imagePath = Path.Combine(directory, media.Name + ".jpg");

                if (!string.IsNullOrWhiteSpace(metadata.ImageUrl) && (!metadata.HasThumbnail || !metadata.HasImage))
                    await DownloadImageAsync(imagePath, metadata, canceller);
                if (!string.IsNullOrWhiteSpace(metadata.BackdropUrl) && !metadata.HasBackdrop)
                    await DownloadBackdropAsync(imagePath, metadata, canceller);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not download picture from {image} or {backdrop}", metadata.ImageUrl, metadata.BackdropUrl);
            }

            return metadata;
        }

        private async Task DownloadBackdropAsync(string imagePath, Metadata metadata, CancellationTokenSource canceller)
        {
            var backdropPath = imagePath.Replace(".jpg", "_backdrop.jpg");

            using var response = await _httpClient.GetAsync(metadata.BackdropUrl, canceller.Token);
            response.EnsureSuccessStatusCode();

            // Download backdrop
            await using var ms = await response.Content.ReadAsStreamAsync(canceller.Token);
            using var fs = File.Create(backdropPath);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(fs);
            metadata.BackdropPath = backdropPath;
        }

        private async Task DownloadImageAsync(string imagePath, Metadata metadata, CancellationTokenSource canceller)
        {
            using var response = await _httpClient.GetAsync(metadata.ImageUrl, canceller.Token);
            response.EnsureSuccessStatusCode();

            // Download image
            await using var ms = await response.Content.ReadAsStreamAsync(canceller.Token);
            using var fs = File.Create(imagePath);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(fs);
            metadata.ImagePath = imagePath;

            // Create thumbnail
            var thumbnailPath = imagePath.Replace(".jpg", "_thumbnail.jpg");
            ImageGenerator.TryCreateThumbnail(_logger, ms, thumbnailPath, 640, 480);
            metadata.ThumbnailPath = thumbnailPath;
        }
    }
}
