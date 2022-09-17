using System;
using System.Collections.Concurrent;
using Cast.Provider.Converter;
using Cast.Provider.Meta;
using Cast.SharedModels.User;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Cast.Provider
{
    // TODO: logging
    public class MediaProvider : MediaProviderBase
    {
        private readonly ILogger<MediaProvider> _logger;
        private readonly IAppCache _lazyCache;

        public MediaProvider(ILogger<MediaProvider> logger,
            IMetadataProvider metadataProvider,
            IMediaConverter mediaConverter,
            IAppCache lazyCache,
            UserProfile userProfile)
            : base(logger, metadataProvider, mediaConverter, userProfile)
        {
            _logger = logger;
            _lazyCache = lazyCache;
            _userProfile.ProfileChanged += UserProfileChanged;
        }

        private string CacheKey => CreateCacheKey(_userProfile.Library.Directories, _userProfile.Library.Extensions);

        private void UserProfileChanged(object? sender, EventArgs e)
        {
            // TODO: gently refresh cache
            _ = GetAllMedia();
        }

        private string CreateCacheKey(IEnumerable<string> directories, IEnumerable<string> extensions)
        {
            unchecked
            {
                int hash = 17;
                foreach (var directory in directories)
                    hash = hash * 31 + directory.GetHashCode();
                foreach (var extension in extensions)
                    hash = hash * 31 + extension.GetHashCode();
                return $"{nameof(MediaProvider)}|{hash}";
            }
        }

        public override async Task<ConcurrentDictionary<Guid, IMedia>> GetAllMedia()
            => await _lazyCache.GetOrAddAsync(CacheKey,
                async (cacheEntry) =>
                {
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                    return await base.GetAllMedia();
                });

        public override async Task<IMedia> GetMedia(Guid guid) => (await GetAllMedia())[guid];

        public override bool IsCached => _lazyCache.TryGetValue(CacheKey, out object _);
    }
}
