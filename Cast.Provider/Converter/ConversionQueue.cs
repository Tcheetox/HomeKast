using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;
using Xabe.FFmpeg.Exceptions;

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

                            Current = state.Media;
                            // Extract and assign subtitles
                            await ExtractMediaSubtitlesAsync(state);
                            // Proceed to media conversion
                            await ConvertMediaAsync(conversion, state);
                            // Raise event
                            OnMediaConverted?.Invoke(this, new ConversionEventArgs(state));
                        }
                        catch (OperationCanceledException ex)
                        {
                            _logger.LogError(ex, "Conversion cancelled by user for {media.Name} ({media.Id})", state.Media.Name, state.Media.Id);
                        }
                        catch (ConversionException ex)
                        {
                            _logger.LogError(ex, "Conversion error for {media.Name} ({media.Id})", state.Media.Name, state.Media.Id);
                        }
                        finally
                        {
                            Current = null;
                            state.UpdateProgress();
                            state.Canceller.Dispose();
                            // It might already be removed from the queue but let's try anyway
                            TryRemove(state.Media);
                        }
                    }
                    Thread.Sleep(50);
                }
            }, _conversionCanceller.Token);
        }

        #region Private Members
        private async Task ExtractMediaSubtitlesAsync(ConversionState state)
        {
            if (state.Canceller.Token.IsCancellationRequested
                || !state.Media.Subtitles.Any())
                return;

            var clock = new Stopwatch();
            clock.Restart();

            StringBuilder command = new($"-i {state.Media.LocalPath}");
            foreach (var subtitle in state.Media.Subtitles)
            {
                if (File.Exists(subtitle.Path))
                    File.Delete(subtitle.Path);
                command.AppendFormat(" -c copy -map 0:s:{0} {1}", subtitle.Index, subtitle.Path);
            }

            await FFmpeg
                .Conversions
                .New()
                .AddParameter(command.ToString())
                .Start(state.Canceller.Token);

            _logger.LogInformation("Extracted {subtitles.count} subtitles stream(s) for {media.Name} ({media.Id}) in {time} ms",
                state.Media.Subtitles.Count,
                state.Media.Name,
                state.Media.Id,
                clock.Elapsed.Minutes);
        }

        private async Task ConvertMediaAsync(IConversion conversion, ConversionState state)
        {
            if (state.Canceller.Token.IsCancellationRequested)
                return;

            // Nuke existing file
            if (File.Exists(conversion.OutputFilePath))
                File.Delete(conversion.OutputFilePath);

            var clock = new Stopwatch();
            clock.Restart();
            // Convert file
            await conversion.Start(state.Canceller.Token);
            // Put converted file under user library directory
            ConversionHelper.MoveAndRename(state);

            clock.Stop();
            _logger.LogInformation("Conversion successful for {media.Name} ({media.Id}) after {time} minutes",
                state.Media.Name,
                state.Media.Id,
                clock.Elapsed.Minutes);
        }
        #endregion

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
