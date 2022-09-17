using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;
using Cast.SharedModels.User;

namespace Cast.Provider.Converter
{
    // TODO: logging + IDisposable
    internal class ConversionQueue
    {
        private readonly ConcurrentQueue<IConversion> _queue;
        private readonly ConcurrentDictionary<string, ConversionState> _conversions;
        private readonly CancellationTokenSource _conversionCanceller;
        private readonly ILogger _logger;

        public ConversionQueue(ILogger logger)
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
                            Current = state.SourceMedia;
                            if (File.Exists(conversion.OutputFilePath))
                                File.Delete(conversion.OutputFilePath);
                            // Convert file
                            await conversion.Start(state.Canceller.Token);
                            // Put covnerted file under user library directory
                            ConversionHelper.MoveAndRename(state);
                            // Raise event
                            OnMediaConverted?.Invoke(this, new ConversionEventArgs(state)); 
                        }
                        catch (OperationCanceledException ex)
                        {
                            _logger.LogError("!! Conversion cancelled by user", ex);
                        }
                        catch (Exception ex)
                        {               
                            _logger.LogError("!! Conversion error", ex);
                        }
                        finally
                        {
                            Current = null;
                            state.UpdateProgress();
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
            => _conversions.TryGetValue(media.ConversionPath, out state);

        public bool TryRemove(IMedia media)
            => _conversions.TryRemove(media.ConversionPath, out _);
        #endregion
    }
}
