using System.Collections.Concurrent;

namespace Cast.Provider
{
    public interface IMediaProvider
    {
        Task<ConcurrentDictionary<Guid, IMedia>> GetAllMedia();
        Task<IMedia> GetMedia(Guid guid);
        Task<bool> TryAddMediaFromPath(string filePath);
        Task<bool> TryRemoveMediaFromPath(string filePath);
        bool IsCached { get; }
    }
}