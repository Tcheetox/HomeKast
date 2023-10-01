using Kast.Provider.Conversions.Factories;
using Kast.Provider.Media;
using Kast.Provider.Supports;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Xabe.FFmpeg;

namespace Kast.Provider.Conversions
{
    public class MediaConverter : IMediaConverter, IDisposable
    {
        private sealed class ConversionInfo : Tuple<ConversionToken, ConversionContext>
        {
            public readonly ConversionContext Context;
            public readonly ConversionToken Token;
            public ConversionInfo(ConversionToken token, ConversionContext container)
                : base(token, container)
            {
                Token = token;
                Context = container;
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

        public bool Start(IMedia media)
        {
            if (media.Status != MediaStatus.MissingSubtitles && media.Status != MediaStatus.Unplayable)
                return false;

            var context = new ConversionContext(media, _settingsProvider);
            var token = new ConversionToken(
                media.ToString(),
                new List<Func<CancellationToken, Task>>()
                {
                    _subtitlesFactory.ConvertAsync(context),
                    _streamFactory.ConvertAsync(context)
                },
                onAdd: (_o, _e) => media.UpdateStatus(-1),
                onStart: async (_o, _e) =>
                {
                    if (media.Info != null)
                        return;

                    var info = await _mediaProvider.GetInfoAsync(media) ?? throw new InvalidOperationException($"Conversion of {media} cannot be started because of missing information");
                    media.UpdateInfo(info);
                    _logger.LogInformation("{info} added to {media} to prepare conversion", nameof(IMediaInfo), media);
                },
                onSuccess: async (_o, _e) => await _mediaProvider.AddOrUpdateAsync(context.TargetPath),
                onFinally: (_o, _e) =>
                {
                    _conversionTracking.TryRemove(media, out _);
                    context.Update();
                });

            return _conversionTracking.TryAdd(media, new ConversionInfo(token, context))
                && _conversionQueue.TryAdd(token);
        }

        public bool Stop(IMedia media)
        {
            if (!_conversionTracking.TryRemove(media, out ConversionInfo? info))
                return false;

            _logger.LogInformation("Stopping conversion for {media}", media);

            info.Token.Dispose();
            info.Context.Update();
            return true;
        }

        public bool TryGetValue(IMedia media, out ConversionContext? state)
        {
            state = null;
            if (!_conversionTracking.TryGetValue(media, out ConversionInfo? info))
                return false;

            state = info!.Context;
            return true;
        }

        public IEnumerable<ConversionContext> GetAll()
            => _conversionTracking.Values.Select(e => e.Item2);

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
