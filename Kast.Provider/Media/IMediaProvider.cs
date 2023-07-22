using Xabe.FFmpeg;

namespace Kast.Provider.Media
{
    public interface IProvider<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetAsync(Guid guid);
        Task<T?> GetAsync(string path);
        Task<bool> AddOrUpdateAsync(string path);
        Task<bool> TryRemoveAsync(string path);
    }

    public interface IMediaProvider : IProvider<IMedia>, IRefreshable
    {
        Task<IMediaInfo?> GetInfoAsync(IMedia media);
        Task<IEnumerable<IGrouping<string, IMedia>>> GetGroupAsync();
    }
}
