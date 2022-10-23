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
        private readonly IMemoryCache _cache;

        public CachedMediaProvider(ILogger<CachedMediaProvider> logger,
            IMetadataProvider metadataProvider,
            IMediaConverter mediaConverter,
            IMemoryCache cache,
            UserProfile userProfile)
            : base(logger, metadataProvider, mediaConverter, userProfile)
        {
            _logger = logger;
            _cache = cache;
            _userProfile.ProfileChanged += UserProfileChanged;
        }

        private string CacheKey => $"{nameof(CachedMediaProvider)}|Library";

        private void UserProfileChanged(object? sender, EventArgs e)
        {
            _cache.Remove(CacheKey);
            _ = GetAllMedia();
            _logger.LogInformation("The cached media library has been refreshed following change in {settings}", nameof(UserProfile));
        }

        public override async Task<ConcurrentDictionary<Guid, IMedia>> GetAllMedia()
            => await _cache.GetOrCreateAsync(CacheKey,
                async (cacheEntry) =>
                {
                    cacheEntry.SetSlidingExpiration(TimeSpan.FromMinutes(_userProfile.Library.SlidingExpirationInMinutes));
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(_userProfile.Library.AbsoluteExpirationInHours));
                    return await base.GetAllMedia();
                });

        public override async Task<IMedia> GetMedia(Guid guid) => (await GetAllMedia())[guid];

        public override bool IsCached => _cache.TryGetValue(CacheKey, out object _);
    }
}
