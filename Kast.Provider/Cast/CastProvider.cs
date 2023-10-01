using GoogleCast;
using GoogleCast.Models.Media;
using Kast.Provider.Cast.Messages;
using Kast.Provider.Media;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

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

        private TimeSpan RefreshInterval => TimeSpan.FromMilliseconds(_settingsProvider.Application.ReceiverRefreshInterval);

        public CastProvider(ILogger<CastProvider> logger, SettingsProvider settingsProvider)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
            _refreshTask = new Lazy<Task>(() => Task.Run(async () =>
            {
                _logger.LogDebug("Refreshing {target}'s context...", nameof(IReceiver));
                while (!_refreshCanceller.IsCancellationRequested)
                {
                    await RefreshStoreAsync();
                    await Task.Delay(RefreshInterval, _refreshCanceller.Token);
                }
            },
            _refreshCanceller.Token),
            LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private async Task<IDictionary<IReceiver, ReceiverContext<IMedia>>> RefreshStoreAsync()
        {
            try
            {
                var devices = await _deviceLocator.FindReceiversAsync();
                foreach (var receiver in devices.Where(d => !_deviceStore.ContainsKey(d)))
                    _deviceStore.TryAdd(receiver, new ReceiverContext<IMedia>(_logger, _settingsProvider, receiver));
                foreach (var receiver in _deviceStore.Where(d => !devices.Contains(d.Key)).ToList())
                    _deviceStore.TryRemove(receiver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding receivers");
            }

            return _deviceStore;
        }

        private async Task<IDictionary<IReceiver, ReceiverContext<IMedia>>> GetStoreAsync()
        {
            if (_deviceStore.Any() && _refreshTask.IsValueCreated)
                return _deviceStore;
            return await RefreshStoreAsync();
        }

        private async Task<ReceiverContext<IMedia>?> GetContext(Guid id)
        {
            var context = (await GetAllAsync()).FirstOrDefault(c => c.Id == id);
            if (context is null)
                _logger.LogDebug("{context} not found for Id: {id}", nameof(ReceiverContext<IMedia>), id);
            return context;
        }

        #region ICastProvider
        public async Task<IEnumerable<ReceiverContext<IMedia>>> GetAllAsync() => (await GetStoreAsync()).Values;

        public async Task<bool> TrySeek(Guid receiverId, double seconds)
        {
            var context = await GetContext(receiverId);
            if (context is null)
                return false;

            return await context.Do(m => m.SeekAsync(seconds));
        }

        public async Task<bool> TryStop(Guid receiverId)
        {
            var context = await GetContext(receiverId);
            if (context is null)
                return false;

            return await context.Do(m => m.StopAsync());
        }

        public async Task<bool> TryPause(Guid receiverId)
        {
            var context = await GetContext(receiverId);
            if (context is null)
                return false;

            return await context.Do(m => m.PauseAsync());
        }

        public async Task<bool> TryPlay(Guid receiverId)
        {
            var context = await GetContext(receiverId);
            if (context is null)
                return false;

            return await context.Do(m => m.PlayAsync());
        }

        public async Task<bool> TryStart(Guid receiverId, IMedia media, int? subtitleIndex = null)
        {
            var context = await GetContext(receiverId);
            if (context is null)
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
            if (context is null)
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
            if (context is null)
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
            if (context is null)
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
            var metadata = new GenericMediaMetadata() { Title = media.Name };
            if (media.Metadata is not null && (media.Metadata.HasImage || !string.IsNullOrWhiteSpace(media.Metadata.ImageUrl)))
            {
                string url = !media.Metadata.HasImage
                    ? media.Metadata.ImageUrl!
                    : $"{_settingsProvider.Application.Uri}media/{media.Id}/image";
                metadata.Images = new GoogleCast.Models.Image[] { new GoogleCast.Models.Image() { Url = url } };
            }

            var subtitles = new List<Track>(media.Subtitles.Select(s => new Track()
            {
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
                StreamType = StreamType.Buffered,
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
                    _deviceStore.Clear();
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
