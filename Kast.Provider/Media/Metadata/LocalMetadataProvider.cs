using System.Text.Json;
using Kast.Provider.Supports;
using Microsoft.Extensions.Logging;

namespace Kast.Provider.Media
{
    public class LocalMetadataProvider : MetadataProvider
    {
        public LocalMetadataProvider(ILogger<MetadataProvider> logger, HttpClient httpClient, SettingsProvider settingsProvider, JsonSerializerOptions options)
            : base(logger, httpClient, settingsProvider, options)
        { }
        
        public override async Task<Metadata?> GetAsync(IMedia media)
        {
            var metadata = await base.GetAsync(media);
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.ImageUrl) || (metadata.HasThumbnail && metadata.HasImage)) 
                return metadata;

            try
            {
                using HttpClient client = new();
                var response = await client.GetAsync(metadata.ImageUrl);
                response.EnsureSuccessStatusCode();

                // Download image
                await using var ms = await response.Content.ReadAsStreamAsync();
                var directory = IOSupport.CreateTargetDirectory(media.FilePath);
                var imagePath = Path.Combine(directory, media.Name + ".jpg");
                using var fs = File.Create(imagePath);
                ms.Seek(0, SeekOrigin.Begin);
                ms.CopyTo(fs);
                metadata.ImagePath = imagePath;

                // Create thumbnail
                var thumbnailPath = Path.Combine(directory, media.Name + "_thumbnail.jpg");
                ImageGenerator.TryCreateThumbnail(Logger, ms, thumbnailPath, 640, 480);
                metadata.ThumbnailPath = thumbnailPath;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not download image from {url}", metadata.ImageUrl);
            }

            return metadata;
        }
    }
}
