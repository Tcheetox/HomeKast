using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace Cast.Provider.Converter
{
    // TODO: logging + IDisposable
    // TODO: try catching around conversion
    internal class ConversionQueue
    {
        private readonly ConcurrentQueue<IConversion> _queue;
        private readonly ConcurrentDictionary<string, ConversionState> _conversions;
        private readonly CancellationTokenSource _conversionCanceller;
        private readonly ILogger _logger;

        public bool IsEmpty => _queue.IsEmpty;

        public ConversionQueue(ILogger logger)
        {
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
                            await conversion.Start(state.Canceller.Token);
                        }
                        catch (Exception ex)
                        {
                            _logger!.LogError("!! Conversion error", ex);
                        }
                    }
                    Thread.Sleep(100);
                }
            }, _conversionCanceller.Token);
        }

        private static void OnConversionProgress(IMedia media, IConversion conversion, ConversionProgressEventArgs args)
        {
            // TODO: media update + conversions rem
            //if (args.Percent >= 100)
            //    _conversions.Remove(conversion.OutputFilePath, out _);
            //conversionItem.UpdateProgress(args);
        }

        public bool TryAdd(IMedia media, IConversion conversion)
        {
            if (!_conversions.TryAdd(media.ConversionPath, new ConversionState(media)))
                return false;

            conversion.OnProgress += (object sender, ConversionProgressEventArgs args)
                => OnConversionProgress(media, conversion, args);
            _queue.Enqueue(conversion);

            return true;
        }

        public bool TryGet(IMedia media, out ConversionState? state)
            => _conversions.TryGetValue(media.ConversionPath, out state);

        public QueueState GetState()
        {
            if (_conversions.IsEmpty)
                return new QueueState() { IsConverting = false };

            // Double-check needed to avoid minor chance of race condition
            var item = _conversions.FirstOrDefault(c => c.Value.Converting).Value;
            if (item == null)
                return new QueueState() { IsConverting = false };

            return new QueueState()
            {
                IsConverting = true,
                Media = item.SourceMedia,
                MediaProgress = item.Progress?.Percent ?? 0,
                QueueLength = _conversions.Count
            };
        }

        public void Abort(IMedia media)
        {
            if (_conversions.TryGetValue(media.ConversionPath, out ConversionState? state))
                state.Canceller.Cancel();
        }
    }
}
