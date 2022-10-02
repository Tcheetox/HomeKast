using System;
using System.Collections.Concurrent;
using Cast.Provider.Conversions;
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
            _ = Warmup();
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

        // Note: it's a little ugly but the below combination allows to achieve the display of spinning logo while the library is getting loaded in cache
        // It prevents the side effect of LazyCache lock when querying for cache entry while maintaing the advantage of not computing values twice
        public override bool IsCached => !_warmingUp && _lazyCache.TryGetValue(CacheKey, out object _);

        private bool _warmingUp;
        private readonly object _warmupLock = new object();
        public override async Task Warmup() 
        {
            lock (_warmupLock)
            {
                _warmingUp = true;
                _ = await GetAllMedia();
                _warmingUp = false;
            }
        }
    }
}
