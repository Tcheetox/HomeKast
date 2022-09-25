using System;
using Cast.SharedModels;
using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;

namespace Cast.Provider.Meta
{
    public class CachedMetadataProvider : MetadataProvider
    {
        public CachedMetadataProvider(ILogger<MetadataProvider> logger, UserProfile userProfile) : base(logger, userProfile)
        { }

        public override async Task<Metadata> GetMetadataAsync(string lookup)
        {
            var metadata = await base.GetMetadataAsync(lookup);
            if (!metadata.HasImage)
                return metadata;

            string path = Path.Combine(UserProfile.Application.StaticFilesDirectory, metadata.Image!.Trim('/'));
            if (File.Exists(path))
            {
                metadata.ImageUrl = Helper.STATIC_FILES_DIRECTORY + metadata.Image!;
                return metadata;
            }

            try
            {
                using HttpClient client = new();
                var response = await client.GetAsync(metadata.ImageUrl);
                response.EnsureSuccessStatusCode();
                await using var ms = await response.Content.ReadAsStreamAsync();
                await using var fs = File.Create(path);
                ms.Seek(0, SeekOrigin.Begin);
                ms.CopyTo(fs);
                metadata.ImageUrl = Helper.STATIC_FILES_DIRECTORY + metadata.Image!;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not download image from {url}", metadata.ImageUrl);
            }

            return metadata;
        }
    }
}
