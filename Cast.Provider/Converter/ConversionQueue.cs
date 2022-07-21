
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

        public ConversionQueue(ILogger logger)
        {
            _queue = new ConcurrentQueue<IConversion>();
            _conversions = new ConcurrentDictionary<string, ConversionState>();

            _conversionCanceller = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!_conversionCanceller.IsCancellationRequested)
                {
                    if (!_queue.IsEmpty
                    && _queue.TryDequeue(out IConversion? conversion)
                    && _conversions.TryGetValue(conversion.OutputFilePath, out ConversionState? state))
                        conversion.Start(state.Canceller.Token);
                    Thread.Sleep(100);
                }
            }, _conversionCanceller.Token);
        }

        public bool TryAdd(IMedia media, IConversion conversion)
        {
            if (!_conversions.TryAdd(media.ConversionPath, new ConversionState(media)))
                return false;

            conversion.OnProgress += OnConversionProgress;
            _queue.Enqueue(conversion);

            return true;
        }

        public bool TryGet(IMedia media, out ConversionState? state)
            => _conversions.TryGetValue(media.ConversionPath, out state);

        private void OnConversionProgress(object sender, ConversionProgressEventArgs args)
        {
            var conversion = (IConversion)sender;
            if (_conversions.TryGetValue(conversion.OutputFilePath, out ConversionState? conversionItem))
                conversionItem.UpdateProgress(args);
        }

        public ConversionState? GetStatus(IMedia media)
            => _conversions.TryGetValue(media.ConversionPath, out ConversionState? state) ? state : null;

        public void Abort(IMedia media)
        {
            if (_conversions.TryGetValue(media.ConversionPath, out ConversionState? state))
                state.Canceller.Cancel();
        }
    }
}
