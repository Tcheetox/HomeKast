using Cast.SharedModels.User;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cast.Provider.Meta
{
    public class CachedMetadataProvider : IMetadataProvider
    {
        public const string VIRTUAL_DIRECTORY = "/Metadata";

        private readonly ILogger<CachedMetadataProvider> _logger;
        private readonly MetadataProvider _metadataProvider;
        private readonly UserProfile _userProfile;

        public CachedMetadataProvider(ILogger<CachedMetadataProvider> logger, MetadataProvider metadataProvider, UserProfile userProfile)
        {
            _logger = logger;
            _metadataProvider = metadataProvider;
            _userProfile = userProfile;
        }

        public async Task<Metadata> GetMetadataAsync(string lookup)
        {
            var metadata = await _metadataProvider.GetMetadataAsync(lookup);
            if (!metadata.HasImage)
                return metadata;

            string path = Path.Combine(_userProfile.Library.Metadata, metadata.Image!.Trim('/'));
            if (File.Exists(path))
            {
                metadata.ImageUrl = VIRTUAL_DIRECTORY + metadata.Image!;
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
                metadata.ImageUrl = VIRTUAL_DIRECTORY + metadata.Image!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not download image from {url}", metadata.ImageUrl);
            }

            return metadata;
        }
    }
}
