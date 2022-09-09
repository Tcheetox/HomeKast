using System;
using System.Collections.Concurrent;
using Cast.SharedModels.User;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Cast.Provider
{
    // TODO: logging
    public class CachedMediaProvider : IProviderService
    {
        private readonly IAppCache _lazyCache;
        private readonly IProviderService _providerService;
        private readonly ILogger<CachedMediaProvider> _logger;
        private readonly UserProfile _userProfile;

        public CachedMediaProvider(ILogger<CachedMediaProvider> logger, IProviderService providerService, IAppCache lazyCache, UserProfile userProfile)
        {
            _logger = logger;
            _providerService = providerService;
            _lazyCache = lazyCache;
            _userProfile = userProfile;

            _userProfile.ProfileChanged += UserProfileChanged;
        }

        private string CacheKey => CreateCacheKey(_userProfile.Library.Directories, _userProfile.Library.Extensions);

        private void UserProfileChanged(object? sender, EventArgs e)
        {
            // TODO: gently refresh cache
            _ = GetMedia();
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
                return $"{nameof(CachedMediaProvider)}|{hash}";
            }
        }

        public bool IsCached => _lazyCache.TryGetValue(CacheKey, out object _);

        public async Task<ConcurrentDictionary<Guid, IMedia>> GetMedia()
            => await _lazyCache.GetOrAddAsync(CacheKey,
                async (cacheEntry) =>
                {
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                    return await _providerService.GetMedia();
                });

        public async Task<IMedia?> GetMedia(Guid guid) => (await GetMedia())[guid];
    }
}
