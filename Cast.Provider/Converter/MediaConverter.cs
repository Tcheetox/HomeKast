using System;
using Cast.SharedModels.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace Cast.Provider.Converter
{
    public class MediaConverter : IMediaConverter, IDisposable
    {
        private readonly ILogger<MediaConverter> _logger;
        private readonly ConversionQueue _conversionQueue;
        private readonly UserProfile _userProfile;

        public IMedia? Current => _conversionQueue.Current;
        public bool HasPendingConversions => _conversionQueue.IsQueueEmpty;

        // Pass-through
        public event EventHandler<ConversionEventArgs> OnMediaConverted
        {
            add => _conversionQueue.OnMediaConverted += value;
            remove => _conversionQueue.OnMediaConverted -= value;
        }

        public MediaConverter(ILogger<MediaConverter> logger, IServiceProvider serviceProvider, UserProfile userProfile)
        {
            _logger = logger;
            _userProfile = userProfile;
            _conversionQueue = new ConversionQueue(serviceProvider.GetRequiredService<ILogger<ConversionQueue>>());

            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg");
            if (!Directory.Exists(directory))
                throw new ArgumentException($"FFmpeg directory not found {directory}");

            FFmpeg.SetExecutablesPath(directory);
            _logger.LogDebug("FFmpeg directory {directory}", directory);
        }

        public async Task<IMediaInfo?> GetMediaInfo(string path, int timeout = 1000)
        {
            var canceller = new CancellationTokenSource();

            try
            {
                canceller.CancelAfter(timeout);
                return await FFmpeg.GetMediaInfo(path, canceller.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Extracting media info timed-out for {path}", path);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Extracting media info unexpectedly failed for {path}", path);
            }
            finally
            {
                canceller.Dispose();
            }

            return null;
        }

        public bool TryGetMediaState(IMedia media, out ConversionState state) => _conversionQueue.TryGet(media, out state);

        public bool StopConvertion(IMedia media)
        {
            if (TryGetMediaState(media, out ConversionState? conversionState)
                && _conversionQueue.TryRemove(media)
                && !conversionState!.Canceller.IsCancellationRequested)
            {
                conversionState.Canceller.Cancel();
                conversionState.UpdateProgress();
                return true;
            }
            return false;
        }

        public bool StartConversion(IMedia media)
        {
            IVideoStream videoStream = media.Info.VideoStreams
                .First()
                .SetCodec(VideoCodec.h264)
                .SetOptimalSize();

            IStream audioStream = media.Info.AudioStreams
                .SetPreferredStream(_userProfile.Preferences)
                .SetCodec(AudioCodec.mp3);

            IConversion conversion = FFmpeg.Conversions
                .New()
                .AddStream(audioStream, videoStream)
                .SetOutput(media.ConversionPath);

            var state = _conversionQueue.TryAdd(media, conversion);
            if (state)
                _logger.LogInformation("Conversion successfuly enqueued for {media.Name} ({media.Id})", media.Name, media.Id);
            else
                _logger.LogCritical("Failed to enqueue conversion for {media.Name} ({media.Id})", media.Name, media.Id);
            return state;
        }

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    _conversionQueue.Dispose();
                disposedValue = true;
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
