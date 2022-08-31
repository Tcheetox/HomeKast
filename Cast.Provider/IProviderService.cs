using System.Collections.Concurrent;

namespace Cast.Provider
{
    public interface IProviderService
    {
        Task<ConcurrentDictionary<Guid, IMedia>> GetMedia();
        Task<IMedia?> GetMedia(Guid guid);
        bool IsCached { get; }
    }
}