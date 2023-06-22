using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using GoogleCast;
using GoogleCast.Models.Media;
using Kast.Provider.Media;
using Kast.Provider.Cast.Messages;

namespace Kast.Provider.Cast
{
    public class CastProvider : ICastProvider, IDisposable
    {
        private readonly ILogger<CastProvider> _logger;
        private readonly ConcurrentDictionary<IReceiver, ReceiverContext<IMedia>> _deviceStore = new(new ReceiverComparer());
        private readonly DeviceLocator _deviceLocator = new();
        private readonly SettingsProvider _settingsProvider;
        private readonly CancellationTokenSource _refreshCanceller = new();
        private readonly Lazy<Task> _refreshTask;

        public CastProvider(ILogger<CastProvider> logger, SettingsProvider settingsProvider)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _refreshTask = new Lazy<Task>(() => Task.Run(async () => 
            {
                while(!_refreshCanceller.IsCancellationRequested)
                {
                    _logger.LogInformation("Restoring {target}'s context...", nameof(IReceiver));
                    await RefreshStore();
                    await Task.Delay(TimeSpan.FromMilliseconds(settingsProvider.Application.ReceiverRefreshInterval));
                }
            }, 
            _refreshCanceller.Token), 
            LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private async Task RefreshStore()
        {
            try
            {
                foreach (var receiver in await _deviceLocator.FindReceiversAsync())
                    _deviceStore.TryAdd(receiver, new ReceiverContext<IMedia>(_logger, receiver));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding receivers");
            }

            foreach (var context in _deviceStore.Values)
                try
                {
                    await context.RefreshAsync();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "error refreshing {context} context", context);
                }
        }

        private async Task<IDictionary<IReceiver, ReceiverContext<IMedia>>> GetStoreAsync()
        {
            if (_deviceStore.Any() && !_refreshCanceller.IsCancellationRequested && _refreshTask.IsValueCreated)
                return _deviceStore;

            await RefreshStore();

            return _deviceStore;
        }

        private async Task<ReceiverContext<IMedia>?> GetContext(Guid id)
        {
            var context = (await GetAllAsync()).FirstOrDefault(c => c.Id == id);
            if (context == null)
                _logger.LogDebug("{context} not found for Id: {id}", nameof(ReceiverContext<IMedia>), id);
            return context;
        }

        #region ICastProvider
        public async Task<IEnumerable<ReceiverContext<IMedia>>> GetAllAsync() => (await GetStoreAsync()).Values;

        public async Task<bool> TrySeek(Guid receiverId, double seconds)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do(m => m.SeekAsync(seconds));
        }

        public async Task<bool> TryStop(Guid receiverId)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do(m => m.StopAsync());
        }

        public async Task<bool> TryPause(Guid receiverId)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do(m => m.PauseAsync());
        }

        public async Task<bool> TryPlay(Guid receiverId)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do(m => m.PlayAsync());
        }

        public async Task<bool> TryStart(Guid receiverId, IMedia media, int? subtitleIndex = null)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do(media, (c, m) =>
            {
                var mediaInfo = Convert(m);
                var trackIds = subtitleIndex.HasValue ? new int[] { subtitleIndex.Value } : Array.Empty<int>();
                return c.LoadAsync(mediaInfo, activeTrackIds: trackIds);
            });
        }

        public async Task<bool> TryChangeSubtitles(Guid receiverId, int? subtitleIndex = null)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do((c, m) =>
            {
                var subtitles = m.Subtitles.FirstOrDefault(s => s.Index == subtitleIndex);
                if (subtitles == null)
                    return c.EditTracksInfoAsync(enabledTextTracks: false);
                return c.EditTracksInfoAsync(subtitles.Language, activeTrackIds: subtitles.Index);
            });
        }

        private const string _receiverNamespace = "urn:x-cast:com.google.cast.receiver";
        private const string _destinationId = "receiver-0";
        public async Task<bool> TryToggleMute(Guid receiverId, bool mute)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do(m =>
            {
                var sender = m.Sender!;
                return sender.SendAsync(_receiverNamespace, new VolumeMessage(mute), _destinationId);
            });
        }

        public async Task<bool> TrySetVolume(Guid receiverId, float volume)
        {
            var context = await GetContext(receiverId);
            if (context == null)
                return false;

            return await context.Do(m =>
            {
                var sender = m.Sender!;
                return sender.SendAsync(_receiverNamespace, new VolumeMessage(volume), _destinationId);
            });
        }
        #endregion

        #region IRefreshable
        public Task RefreshAsync() => _refreshTask.Value;
        #endregion

        private MediaInformation Convert(IMedia media)
        {
            // TODO: implement image width and height
            var metadata = new GenericMediaMetadata()
            {
                Title = media.Name,
                //Images = string.IsNullOrWhiteSpace(media.Metadata.Image) 
                //? Array.Empty<Image>() 
                //: new Image[] { new Image() { Url = media.Metadata.Image } }
            };
            var subtitles = new List<Track>(media.Subtitles.Select(s => new Track() { 
                TrackId = s.Index, 
                Name = s.Language,
                Language = s.Name, 
                TrackContentId = $"{_settingsProvider.Application.Uri}media/{media.Id}/subtitles/{s.Index}" 
            }));
            return new MediaInformation()
            {
                ContentId = $"{_settingsProvider.Application.Uri}media/{media.Id}/stream",
                Tracks = subtitles,
                Metadata = metadata,
                Duration = media.Length.TotalSeconds,
                ContentType = media.ContentType,
                StreamType = GoogleCast.Models.Media.StreamType.Buffered,
                CustomData = new Dictionary<string, string> { { media.Id.ToString(), media.Name } }
            };
        }

        #region IDisposable
        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (!_refreshCanceller.IsCancellationRequested)
                        _refreshCanceller.Cancel();
                    _refreshCanceller.Dispose();
                    foreach (var state in _deviceStore.Values)
                        state.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
