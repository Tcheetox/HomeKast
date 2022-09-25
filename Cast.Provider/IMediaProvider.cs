using System.Collections.Concurrent;

namespace Cast.Provider
{
    public interface IMediaProvider
    {
        Task<ConcurrentDictionary<Guid, IMedia>> GetAllMedia();
        Task<IMedia> GetMedia(Guid guid);
        Task<bool> TryAddOrUpdateMedia(string path);
        Task<bool> TryAddMedia(string path);
        Task<bool> TryRemoveMedia(string path);
        void UpdateMediaSubtitles(string path);

        bool IsCached { get; }
    }
}