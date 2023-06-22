using System.Collections.Concurrent;
using System.Text.Json;
using System.Drawing;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Kast.Provider.Supports;

namespace Kast.Provider.Media
{
    public class CachedMetadataProvider : MetadataProvider
    {
        public CachedMetadataProvider(ILogger<MetadataProvider> logger, SettingsProvider settingsProvider, JsonSerializerOptions options)
            : base(logger, settingsProvider, options)
        { }

        private readonly ConcurrentDictionary<string, Metadata> _store = new(StringComparer.OrdinalIgnoreCase);
        public override async Task<Metadata> GetAsync(IMediaInfo info, string lookup)
        {
            var targetDirectory = IOSupport.CreateTargetDirectory(info.Path);
            if (_store.TryGetValue(lookup, out Metadata? metadata))
                return metadata;

            metadata = await base.GetAsync(info, lookup);
            if (string.IsNullOrWhiteSpace(metadata.Image))
            {
                _store.TryAdd(lookup, metadata);
                return metadata;
            }

            string path = Path.Combine(targetDirectory, lookup.Replace(" ", "_") + ".jpg");
            if (File.Exists(path))
            {
                metadata.Image = path;
                _store.TryAdd(lookup, metadata);
                return metadata;
            }

            try
            {
                using HttpClient client = new();
                var response = await client.GetAsync(metadata.Image);
                response.EnsureSuccessStatusCode();
                await using var ms = await response.Content.ReadAsStreamAsync();
                using var fs = File.Create(path);
                ms.Seek(0, SeekOrigin.Begin);
                ms.CopyTo(fs);
                metadata.Image = path;

                using var image = Image.FromStream(ms);
                metadata.ImageWidth = image.Width;
                metadata.ImageHeight = image.Height;
                if (ImageGenerator.TryCreateThumbnail(Logger, image, path, 640, 480, out string? thumbnailPath))
                    metadata.Thumbnail = thumbnailPath;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not download image from {url}", metadata.Image);
            }

            _store.TryAdd(lookup, metadata);
            return metadata;
        }
    }
}
