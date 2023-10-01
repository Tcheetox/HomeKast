using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using GoogleCast.Models.Receiver;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Kast.Provider.Cast
{
    [DebuggerDisplay("{Name}")]
    public class ReceiverContext<T> : IDisposable
        where T : class, IEquatable<T>
    {
        public readonly Guid Id;
        public readonly string Name;

        public bool IsConnected { get; private set; }
        public bool IsOwner { get; private set; }
        public bool IsLaunched { get; private set; }
        public T? Media { get; private set; }
        public bool? IsMuted => _receiverStatus?.Volume?.IsMuted;
        public bool? IsIdle => _receiverStatus?.Applications?.FirstOrDefault()?.IsIdleScreen;
        public float? Volume => _receiverStatus?.Volume?.Level;
        public string? Owner
        {
            get
            {
                var application = _receiverStatus?.Applications?.FirstOrDefault()?.DisplayName;
                if (application == MediaChannel?.ApplicationId)
                    return "HomeKast";
                return application;
            }
        }

        public string? Title => (Media as Media.IMedia)?.Name ?? _mediaStatus?.Media?.Metadata?.Title;

        public TimeSpan? Current
        {
            get
            {
                var time = _mediaStatus?.CurrentTime;
                if (!time.HasValue) return null;
                return TimeSpan.FromSeconds(time.Value);
            }
        }

        public TimeSpan? Duration
        {
            get
            {
                var length = (Media as Media.IMedia)?.Length;
                if (length is not null)
                    return length;
                var duration = _mediaStatus?.Media?.Duration;
                if (duration.HasValue)
                    return TimeSpan.FromSeconds(duration.Value);
                return null;
            }
        }

        private readonly IReceiver _receiver;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Sender _sender = new();
        private readonly ILogger _logger;

        private ReceiverStatus? _receiverStatus;
        private MediaStatus? _mediaStatus;
        private IMediaChannel? _channel;

        private IMediaChannel? MediaChannel
        {
            get => _channel;
            set
            {
                if (_channel != null)
                    _channel.StatusChanged -= OnStatusChanged;
                if (value != null)
                    value.StatusChanged += OnStatusChanged;
                _channel = value;
            }
        }

        private void OnStatusChanged(object? sender, EventArgs e)
        {
            if (sender is IMediaChannel channel)
            {
                var item = channel.Status?.FirstOrDefault();
                if (item is not null)
                    _mediaStatus = item;
            }
        }

        private TimeSpan RefreshInterval 
            => IsOwner
            ? TimeSpan.FromMilliseconds(300) 
            : TimeSpan.FromMilliseconds(_applicationSettings.ReceiverRefreshInterval);

        private readonly Application _applicationSettings;
        private readonly CancellationTokenSource _refreshCanceller = new();
        public ReceiverContext(ILogger logger, SettingsProvider settings, IReceiver receiver)
        {
            Id = Guid.Parse(receiver.Id);
            Name = receiver.FriendlyName;

            _logger = logger;
            _applicationSettings = settings.Application;
            _receiver = receiver;
            _sender.Disconnected += OnDisconnected;

            Task.Run(async () =>
            {
                while (!_refreshCanceller.IsCancellationRequested)
                {
                    await RefreshAsync();
                    await Task.Delay(RefreshInterval, _refreshCanceller.Token);
                }
            },
            _refreshCanceller.Token);
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            IsConnected = false;
            IsLaunched = false;
        }

        private async Task RefreshAsync()
        {
            if (MediaChannel == null || !IsConnected)
            {
                MediaChannel = _sender.GetChannel<IMediaChannel>();
                await _sender.ConnectAsync(_receiver);
                IsConnected = true;
            }

            // Update status
            if (MediaChannel is not null)
                _mediaStatus = await GetMediaStatusAsync();
            if (_sender
                .GetStatuses()
                .FirstOrDefault(e => e.Key.EndsWith("receiver")).Value is ReceiverStatus receiverStatus)
                _receiverStatus = receiverStatus;
        }

        private async Task<MediaStatus?> GetMediaStatusAsync()
        {
            try
            {
                var status = await MediaChannel!
                    .GetStatusAsync()
                    .WaitAsync(_refreshCanceller.Token);
                if (status is not null)
                    IsOwner = true;
                return status;
            }
            catch (Exception)
            {
                IsOwner = false;
            }

            return null;
        }

        private async Task<bool> Do(Func<IMediaChannel, Task<bool>> perform)
        {
            try
            {
                await _lock.WaitAsync();

                await RefreshAsync();

                if (MediaChannel is not null && IsConnected && !IsLaunched)
                {
                    _receiverStatus = await _sender.LaunchAsync(MediaChannel);
                    IsLaunched = true;
                }

                if (MediaChannel is null || !IsConnected || !IsLaunched)
                    return false;

                return await perform(MediaChannel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing action on {name} ({id}) (Owner: {ownership})", Name, Id, IsOwner);
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> Do(Func<IMediaChannel, Task> perform)
            => await Do(async m =>
            {
                await perform(m);
                return true;
            });

        public async Task<bool> Do(Func<IMediaChannel, T, Task> perform)
            => await Do(async m =>
            {
                if (Media is null)
                    return false;
                await perform(m, Media);
                return true;
            });

        public async Task<bool> Do(T target, Func<IMediaChannel, T, Task> perform)
            => await Do(async m =>
            {
                Media = target;
                await perform(m, Media);
                IsOwner = true;
                return true;
            });

        public override string ToString() => $"{Name} ({Id})";

        #region IDisposable
        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _refreshCanceller.Cancel();
                    _refreshCanceller.Dispose();

                    if (IsConnected)
                        _sender.Disconnect();

                    MediaChannel = null;
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
