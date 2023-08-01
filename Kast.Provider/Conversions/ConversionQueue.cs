using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg.Exceptions;

namespace Kast.Provider.Conversions
{
    internal class ConversionQueue<T> : IDisposable where T : IEquatable<T>
    {
        private readonly ILogger<ConversionQueue<T>> _logger;
        private readonly CancellationTokenSource _queueCanceller = new();
        private readonly BlockingCollection<ConversionToken> _blockingConversions = new();
        
        public ConversionQueue(ILogger<ConversionQueue<T>> logger) 
        {
            _logger = logger;

            Task.Run(async () =>
            {
                while (!_queueCanceller.IsCancellationRequested)
                {
                    if (!_blockingConversions.TryTake(out var item, -1, _queueCanceller.Token))
                        continue;
                    
                    if (item.IsCancellationRequested)
                    {
                        _logger.LogInformation("Conversion skipped for {item}", item);
                        continue;
                    }

                    var clock = Stopwatch.StartNew();
                    try
                    {
                        _logger.LogWarning("Conversion starting for {item}", item);
                        item.OnStart?.Invoke(this, EventArgs.Empty);
                        foreach (var conversion in item.Conversions)
                        {
                            item.CancellationToken.ThrowIfCancellationRequested();
                            await conversion(item.CancellationToken);
                        }
                        clock.Stop();
                        _logger.LogInformation("Conversion successful for {item} after {time} seconds", item, clock.Elapsed.TotalSeconds);
                        item.OnSuccess?.Invoke(this, EventArgs.Empty);
                    }
                    catch (OperationCanceledException ex) 
                    {
                        _logger.LogError(ex, "Conversion cancelled by user for {item} after {time} seconds", item, clock.Elapsed.TotalSeconds);
                        item.OnError?.Invoke(this, EventArgs.Empty);
                        
                    }
                    catch (ConversionException ex)
                    {
                        _logger.LogError(ex, "Conversion error for {item} after {time} seconds", item, clock.Elapsed.TotalSeconds);
                        item.OnError?.Invoke(this, EventArgs.Empty);
                    }
                    finally
                    {
                        clock.Stop();
                        item.OnFinally?.Invoke(this, EventArgs.Empty);
                        item.Dispose();
                    }
                }
            }, _queueCanceller.Token);
        }

        public bool TryAdd(ConversionToken options){
            if (_blockingConversions.TryAdd(options))
            {
                options.OnAdd?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        #region IDisposable
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _queueCanceller.Cancel();
                    _queueCanceller.Dispose();
                    _blockingConversions.Dispose();
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
