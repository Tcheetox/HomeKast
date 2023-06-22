using Kast.Provider.Media;

namespace Kast.Provider.Cast
{
    public interface ICastProvider : IRefreshable
    {
        Task<IEnumerable<ReceiverContext<IMedia>>> GetAllAsync();
        Task<bool> TryPause(Guid receiverId);
        Task<bool> TryPlay(Guid receiverId);
        Task<bool> TryStop(Guid receiverId);
        Task<bool> TrySeek(Guid receiverId, double seconds);
        Task<bool> TryChangeSubtitles(Guid receiverId, int? subtitleIndex = null);
        Task<bool> TryToggleMute(Guid receiverId, bool mute);
        Task<bool> TryStart(Guid receiverId, IMedia media, int? subtitleIndex = null);
        Task<bool> TrySetVolume(Guid receiverId, float volume);
    }
}