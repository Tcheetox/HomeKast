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
    public class CachedMediaProvider : MediaProviderBase
    {
        private readonly ILogger<CachedMediaProvider> _logger;
        private readonly IAppCache _lazyCache;

        public CachedMediaProvider(ILogger<CachedMediaProvider> logger,
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

        private string CacheKey => $"{nameof(CachedMediaProvider)}|Library";

        private void UserProfileChanged(object? sender, EventArgs e)
        {
            _lazyCache.Remove(CacheKey);
            _ = GetAllMedia();
            _logger.LogInformation("The cached media library has been refreshed following change in {settings}", nameof(UserProfile));
        }

        public override async Task<ConcurrentDictionary<Guid, IMedia>> GetAllMedia()
            => await _lazyCache.GetOrAddAsync(CacheKey,
                async (cacheEntry) =>
                {
                    cacheEntry.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                    return await base.GetAllMedia();
                });

        public override async Task<IMedia> GetMedia(Guid guid) => (await GetAllMedia())[guid];

        public override bool IsCached => _lazyCache.TryGetValue(CacheKey, out object _);
    }
}
