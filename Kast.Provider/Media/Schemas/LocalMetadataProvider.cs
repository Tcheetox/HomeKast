﻿using System.Text.Json;
using Microsoft.Extensions.Logging;
using Kast.Provider.Media.YouTube;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class LocalMetadataProvider : YoutubeMetadataProvider
    {
        public LocalMetadataProvider(ILogger<IMetadataProvider> logger, HttpClient httpClient, SettingsProvider settingsProvider, JsonSerializerOptions options)
            : base(logger, httpClient, settingsProvider, options)
        { }

        public override async Task<Metadata?> GetAsync(IMedia media)
        {
            var metadata = await base.GetAsync(media);
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
                Logger.LogError(ex, "Could not download picture from {image} or {backdrop}", metadata.ImageUrl, metadata.BackdropUrl);
            }

            return metadata;
        }

        private async Task DownloadBackdropAsync(string imagePath, Metadata metadata, CancellationTokenSource canceller)
        {
            var backdropPath = imagePath.Replace(".jpg", "_backdrop.jpg");

            using var response = await HttpClient.GetAsync(metadata.BackdropUrl, canceller.Token);
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
            using var response = await HttpClient.GetAsync(metadata.ImageUrl, canceller.Token);
            response.EnsureSuccessStatusCode();

            // Download image
            await using var ms = await response.Content.ReadAsStreamAsync(canceller.Token);
            using var fs = File.Create(imagePath);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(fs);
            metadata.ImagePath = imagePath;

            // Create thumbnail
            var thumbnailPath = imagePath.Replace(".jpg", "_thumbnail.jpg");
            ImageGenerator.TryCreateThumbnail(Logger, ms, thumbnailPath, 640, 480);
            metadata.ThumbnailPath = thumbnailPath;
        }
    }
}