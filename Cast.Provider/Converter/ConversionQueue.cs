using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace Cast.Provider.Converter
{
    internal class ConversionQueue : IDisposable
    {
        private readonly ConcurrentQueue<IConversion> _queue;
        private readonly ConcurrentDictionary<string, ConversionState> _conversions;
        private readonly CancellationTokenSource _conversionCanceller;
        private readonly ILogger<ConversionQueue> _logger;

        public ConversionQueue(ILogger<ConversionQueue> logger)
        {
            _logger = logger;
            _queue = new ConcurrentQueue<IConversion>();
            _conversions = new ConcurrentDictionary<string, ConversionState>();
            _conversionCanceller = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_conversionCanceller.IsCancellationRequested)
                {
                    if (!_queue.IsEmpty
                    && _queue.TryDequeue(out IConversion? conversion)
                    && _conversions.TryGetValue(conversion.OutputFilePath, out ConversionState? state))
                    {
                        try
                        {
                            var clock = new Stopwatch();
                            Current = state.SourceMedia;
                            if (File.Exists(conversion.OutputFilePath))
                                File.Delete(conversion.OutputFilePath);
                            // Convert file
                            clock.Start();
                            await conversion.Start(state.Canceller.Token);
                            // Put converted file under user library directory
                            ConversionHelper.MoveAndRename(state);
                            // Raise event
                            OnMediaConverted?.Invoke(this, new ConversionEventArgs(state));
                            clock.Stop();
                            _logger.LogInformation("Conversion successful for {media.Name} ({media.Id}) after {time} minutes",
                                state.SourceMedia.Name,
                                state.SourceMedia.Id,
                                clock.Elapsed.Minutes);
                        }
                        catch (OperationCanceledException ex)
                        {
                            _logger.LogError(ex, "Conversion cancelled by user for {media.Name} ({media.Id})", state.SourceMedia.Name, state.SourceMedia.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Conversion error for {media.Name} ({media.Id})", state.SourceMedia.Name, state.SourceMedia.Id);
                        }
                        finally
                        {
                            Current = null;
                            state.UpdateProgress();
                            state.Canceller.Dispose();
                            // It might already be removed from the queue but let's try anyway
                            TryRemove(state.SourceMedia);
                        }
                    }
                    Thread.Sleep(50);
                }
            }, _conversionCanceller.Token);
        }

        #region Public Members
        public IMedia? Current { get; private set; }
        public bool IsQueueEmpty => _conversions.IsEmpty;
        public event EventHandler<ConversionEventArgs> OnMediaConverted;

        public bool TryAdd(IMedia media, IConversion conversion)
        {
            var state = new ConversionState(_conversions, media);
            if (!_conversions.TryAdd(media.ConversionPath, state))
                return false;

            media.Status = MediaStatus.Queued;
            _queue.Enqueue(conversion);
            conversion.OnProgress += (object sender, ConversionProgressEventArgs args)
                => state.UpdateProgress(args);

            return true;
        }

        public bool TryGet(IMedia media, out ConversionState state)
            => _conversions.TryGetValue(media.ConversionPath, out state!);

        public bool TryRemove(IMedia media)
            => _conversions.TryRemove(media.ConversionPath, out _);
        #endregion

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _conversionCanceller.Dispose();
                    foreach (var canceller in _conversions.Select(conversion => conversion.Value.Canceller)
                                                          .Where(canceller => !canceller.IsCancellationRequested))
                        canceller.Cancel();
                    _conversions.Clear();
                }
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
