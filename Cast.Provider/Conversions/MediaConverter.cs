using System;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Cast.SharedModels.User;
using System.Diagnostics;

namespace Cast.Provider.Conversions
{
    public class MediaConverter : IMediaConverter, IDisposable
    {
        private readonly ILogger<MediaConverter> _logger;
        private readonly ConversionQueue _conversionQueue;

        public IMedia? Current => _conversionQueue.Current;
        public bool HasPendingConversions => _conversionQueue.IsQueueEmpty;

        // Pass-through
        public event EventHandler<ConversionEventArgs> OnMediaConverted
        {
            add => _conversionQueue.OnMediaConverted += value;
            remove => _conversionQueue.OnMediaConverted -= value;
        }

        public MediaConverter(ILoggerFactory loggerFactory, UserProfile userProfile)
        {
            _logger = loggerFactory.CreateLogger<MediaConverter>();
            _conversionQueue = new ConversionQueue(userProfile, loggerFactory);

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
            catch (Exception ex)
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
            var state = _conversionQueue.TryAdd(new ConversionOptions()
            {
                Media = media,
                ConversionType = media.Status == MediaStatus.MissingSubtitles
                    ? ConversionType.SubtitlesOnly
                    : ConversionType.FullConversion
            });

            if (state)
                _logger.LogInformation("Conversion successfully enqueued for {media.Name} ({media.Id})", media.Name, media.Id);
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
