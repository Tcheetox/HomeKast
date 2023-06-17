using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Kast.Provider.Conversions.Factories;
using Kast.Provider.Media;
using Kast.Provider.Supports;

namespace Kast.Provider.Conversions
{
    public class MediaConverter : IMediaConverter, IDisposable
    {
        private sealed class ConversionInfo : Tuple<ConversionToken, ConversionState>
        {
            public readonly ConversionState Container;
            public readonly ConversionToken Token;
            public ConversionInfo(ConversionToken token, ConversionState container) 
                : base (token, container)
            {
                Token = token;
                Container = container;
            }
        }

        private readonly ILogger<MediaConverter> _logger;
        private readonly IMediaProvider _mediaProvider;
        private readonly ConversionQueue<IMedia> _conversionQueue;
        private readonly StreamFactory _streamFactory;
        private readonly SubtitlesFactory _subtitlesFactory;
        private readonly SettingsProvider _settingsProvider;
        private readonly ConcurrentDictionary<IMedia, ConversionInfo> _conversionTracking = new();
        
        public MediaConverter(ILoggerFactory loggerFactory, IMediaProvider mediaProvider, SettingsProvider settingsProvider) 
        {
            FFmpegSupport.SetExecutable(out string directory);

            _logger = loggerFactory.CreateLogger<MediaConverter>();
            _mediaProvider = mediaProvider;
            _conversionQueue = new ConversionQueue<IMedia>(loggerFactory.CreateLogger<ConversionQueue<IMedia>>());
            _streamFactory = new StreamFactory(loggerFactory.CreateLogger<StreamFactory>(), settingsProvider);
            _subtitlesFactory = new SubtitlesFactory(loggerFactory.CreateLogger<SubtitlesFactory>(), settingsProvider);
            _settingsProvider = settingsProvider;

            _logger.LogDebug("FFmpeg directory {directory}", directory);
        }

        public async Task<bool> StartAsync(IMedia media)
        {
            if (media.Status != MediaStatus.MissingSubtitles && media.Status != MediaStatus.Unplayable)
                return false;

            if (!media.HasInfo)
            {
                var info = await _mediaProvider.GetInfoAsync(media);
                if (info == null)
                {
                    _logger.LogInformation("Conversion of {media} cannot be started because of missing information", media);
                    return false;
                }
                media.Info = info;
            }

            var state = new ConversionState(media, _settingsProvider);
            var token = new ConversionToken(
                media.ToString(),
                new List<Func<CancellationToken, Task>>() 
                { 
                    _subtitlesFactory.ConvertAsync(state),
                    _streamFactory.ConvertAsync(state)
                },
                onSuccess: (_o, _e) => _mediaProvider.AddOrRefreshAsync(state.MediaTargetPath),
                onFinally: (_o, _e) =>
                {
                    _conversionTracking.TryRemove(media, out _);
                    media.UpdateStatus();
                    _ = IOSupport.DeleteAsync(state.MediaTemporaryPath, _settingsProvider.Application.FileAccessTimeout);
                });

            return _conversionTracking.TryAdd(media, new ConversionInfo(token, state)) 
                && _conversionQueue.TryAdd(token);
        }

        public bool Stop(IMedia media)
        {
            if (!_conversionTracking.TryRemove(media, out ConversionInfo? info))
                return false;

            _logger.LogInformation("Stopping conversion for {media}", media);

            info.Token.Dispose();
            info.Container.Update(null, null);
            return true;
        }

        public bool TryGetValue(IMedia media, out ConversionState? state)
        {
            state = null;
            if (!_conversionTracking.TryGetValue(media, out ConversionInfo? info))
                return false;

            state = info!.Container;
            state.QueueCount = _conversionTracking.Count;
            return true;
        }

        public IEnumerable<ConversionState> GetAll()
        {
            var currentCount = _conversionTracking.Count;
            return _conversionTracking.Values.Select(e =>
            {
                var state = e.Item2;
                state.QueueCount = currentCount;
                return state;
            });
        }

        #region IDisposable
        private bool _disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _conversionTracking.Clear();
                    _conversionQueue.Dispose();
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
