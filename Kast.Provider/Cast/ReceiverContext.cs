using System.Diagnostics;
using Microsoft.Extensions.Logging;
using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using GoogleCast.Models.Receiver;

namespace Kast.Provider.Cast
{
    [DebuggerDisplay("{Name}")]
    public class ReceiverContext<T> : IRefreshable, IDisposable
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
        public TimeSpan? Current
        {
            get
            {
                var time = _mediaStatus?.CurrentTime;
                if (!time.HasValue) return null;
                return TimeSpan.FromSeconds(time.Value);
            }
        }

        private readonly IReceiver _receiver;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Sender _sender;
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
                if (item != null)
                    _mediaStatus = item;
            }    
        }

        public ReceiverContext(ILogger logger, IReceiver receiver)
        {
            _logger = logger;
            _receiver = receiver;

            Id = Guid.Parse(receiver.Id);
            Name = receiver.FriendlyName;
            
            _sender = new Sender();
            _sender.Disconnected += OnDisconnected;
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            IsConnected = false;
            IsLaunched = false;
        }

        private async Task UnlockedRefreshAsync()
        {
            if (!IsConnected || MediaChannel == null)
            {
                MediaChannel = _sender.GetChannel<IMediaChannel>();
                await _sender.ConnectAsync(_receiver);
                if (MediaChannel != null)
                {
                    IsConnected = true;
                    await GetMediaStatusAsync();
                }
            }
            
            // Update status
            var senderStatus = _sender.GetStatuses();
            if (senderStatus?.FirstOrDefault(e => e.Key.EndsWith("receiver")).Value is ReceiverStatus receiverStatus)
                _receiverStatus = receiverStatus;
            if (senderStatus?.FirstOrDefault(e => e.Key.EndsWith("media")).Value is MediaStatus[] mediaStatus && mediaStatus.Any())
                _mediaStatus = mediaStatus.First();
        }

        private async Task GetMediaStatusAsync()
        {
            try
            {
                _mediaStatus = await MediaChannel!
                    .GetStatusAsync()
                    .WaitAsync(new CancellationTokenSource(2000).Token);
                IsOwner = true;
            }
            catch (Exception)
            {
                IsOwner = false;
            }
        }

        public async Task RefreshAsync()
        {
            try
            {
                await _lock.WaitAsync();

                await UnlockedRefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {me} of {name} ({id})", nameof(RefreshAsync), Name, Id);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<bool> Do(Func<IMediaChannel, Task<bool>> perform)
        {
            try
            {
                await _lock.WaitAsync();

                await UnlockedRefreshAsync();

                if (MediaChannel != null && IsConnected && !IsLaunched)
                {
                    _receiverStatus = await _sender.LaunchAsync(MediaChannel);
                    IsLaunched = true;
                }

                if (MediaChannel == null || !IsConnected || !IsLaunched)
                    return false;

                return await perform(MediaChannel);
            }
            catch(Exception ex)
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
                if (Media == null)
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
